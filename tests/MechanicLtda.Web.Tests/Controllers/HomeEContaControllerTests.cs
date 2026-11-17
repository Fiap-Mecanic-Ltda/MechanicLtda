using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Web.Controllers;
using MechanicLtda.Web.Integracoes;
using MechanicLtda.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using static MechanicLtda.Web.Tests.Controllers.Apoio;

namespace MechanicLtda.Web.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<IClienteAppService> _clientes = new();
        private readonly Mock<IVeiculoAppService> _veiculos = new();
        private readonly Mock<IOrdemServicoCliente> _ordens = new();
        private readonly Mock<IEstoqueCliente> _estoque = new();

        private HomeController Criar() => Preparar(new HomeController(_clientes.Object, _veiculos.Object, _ordens.Object, _estoque.Object));

        [Fact]
        public async Task Index_resume_os_numeros_da_oficina()
        {
            _clientes.Setup(c => c.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<ClienteDto>>([new ClienteDto(), new ClienteDto()]));
            _veiculos.Setup(v => v.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<VeiculoDto>>([new VeiculoDto()]));
            _ordens.Setup(o => o.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<OrdemServicoDto>>(
            [
                new OrdemServicoDto { Id = 1, Status = StatusOrdemServico.Recebida, DataCriacao = DateTime.UtcNow.AddDays(-1) },
                new OrdemServicoDto { Id = 2, Status = StatusOrdemServico.Entregue, DataCriacao = DateTime.UtcNow },
                new OrdemServicoDto { Id = 3, Status = StatusOrdemServico.Cancelada, DataCriacao = DateTime.UtcNow }
            ]));
            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<EstoqueDto>>(
            [
                new EstoqueDto { Id = 1, BaixoEstoque = true, QuantidadeAtual = 3 },
                new EstoqueDto { Id = 2, BaixoEstoque = true, QuantidadeAtual = 1 },
                new EstoqueDto { Id = 3, BaixoEstoque = false }
            ]));

            var model = Modelo<DashboardViewModel>(await Criar().Index());
            Assert.Equal(2, model.TotalClientes);
            Assert.Equal(1, model.TotalVeiculos);
            Assert.Equal(1, model.TotalOrdensAbertas);
            Assert.Equal(2, model.TotalItensBaixoEstoque);
            Assert.Equal([2, 1], model.EstoquesCriticos.Select(e => e.Id));
            Assert.Equal(3, model.OrdensRecentes.Count());
        }

        [Fact]
        public async Task Index_com_servicos_fora_zera_os_numeros()
        {
            _clientes.Setup(c => c.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<ClienteDto>>());
            _veiculos.Setup(v => v.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<VeiculoDto>>());
            _ordens.Setup(o => o.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<OrdemServicoDto>>());
            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<EstoqueDto>>());

            var controller = Criar();
            var model = Modelo<DashboardViewModel>(await controller.Index());
            Assert.Equal(0, model.TotalClientes + model.TotalVeiculos + model.TotalOrdensAbertas + model.TotalItensBaixoEstoque);
            Assert.Equal("falhou", controller.TempData["Error"]);
        }
    }

    public class ContaControllerTests
    {
        private readonly Mock<IAuthAppService> _auth = new();
        private readonly Mock<IAuthenticationService> _autenticacao = new();
        private readonly Mock<IUrlHelper> _url = new();

        private ContaController Criar(ClaimsPrincipal? usuario = null)
        {
            var controller = Preparar(new ContaController(_auth.Object), _autenticacao);
            if (usuario is not null)
                controller.HttpContext.User = usuario;
            _url.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns<string?>(u => u is not null && u.StartsWith('/'));
            controller.Url = _url.Object;
            return controller;
        }

        private static string Token() => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Email, "admin@x.com"),
                new Claim("userName", "admin"),
                new Claim("role", "Administrador"),
                new Claim(ClaimTypes.Role, "Mecanico"),
                new Claim("sub", "1")
            ]));

        [Fact]
        public void Login_get_mostra_formulario_ou_redireciona_quem_ja_entrou()
        {
            Assert.Equal("/x", Modelo<LoginRazorViewModel>(Criar().Login("/x")).ReturnUrl);

            var logado = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "a")], "Cookies"));
            Assert.Equal("/os", Assert.IsType<RedirectResult>(Criar(logado).Login("/os")).Url);
            Redireciona(Criar(logado).Login("https://fora.com"), "Index", "Home");
        }

        [Fact]
        public async Task Login_post_invalido_ou_recusado_reexibe()
        {
            var form = new LoginRazorViewModel { Email = "a@x.com", Senha = "errada" };
            Assert.Same(form, Modelo<LoginRazorViewModel>(await Criar().InvalidarModelo().Login(form)));

            _auth.Setup(a => a.LoginAsync("a@x.com", "errada")).ReturnsAsync(Erro<TokenDto>("Usuário ou senha inválidos"));
            var controller = Criar();
            Assert.Same(form, Modelo<LoginRazorViewModel>(await controller.Login(form)));
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Login_post_ok_cria_o_cookie_com_o_token_e_as_roles()
        {
            ClaimsPrincipal? principal = null;
            _autenticacao.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, p, _) => principal = p)
                .Returns(Task.CompletedTask);
            var token = Token();
            _auth.Setup(a => a.LoginAsync("admin@x.com", "123")).ReturnsAsync(Ok(new TokenDto { Token = token, Expiracao = DateTime.UtcNow.AddHours(1) }));

            var resultado = await Criar().Login(new LoginRazorViewModel { Email = "admin@x.com", Senha = "123", ReturnUrl = "/OrdensServico" });

            Assert.Equal("/OrdensServico", Assert.IsType<RedirectResult>(resultado).Url);
            Assert.Equal("admin", principal!.Identity!.Name);
            Assert.True(principal.IsInRole("Administrador"));
            Assert.True(principal.IsInRole("Mecanico"));
            Assert.Equal("admin@x.com", principal.FindFirstValue(ClaimTypes.Email));
            Assert.Equal(token, principal.FindFirstValue(TokenDoUsuarioHandler.ClaimToken));
        }

        [Fact]
        public async Task Logout_encerra_a_sessao()
        {
            Redireciona(await Criar().Logout(), "Login");
            _autenticacao.Verify(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Once);
        }
    }
}
