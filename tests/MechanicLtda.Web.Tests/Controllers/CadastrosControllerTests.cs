using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Web.Controllers;
using MechanicLtda.Web.Integracoes;
using MechanicLtda.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using static MechanicLtda.Web.Tests.Controllers.Apoio;

namespace MechanicLtda.Web.Tests.Controllers
{
    public class ClientesControllerTests
    {
        private readonly Mock<IClienteAppService> _clientes = new();
        private ClientesController Criar() => Preparar(new ClientesController(_clientes.Object));

        private static ClienteDto Cliente => new() { Id = 1, Nome = "Maria", Email = "m@x.com", CpfCnpj = "52998224725", Ativo = true };

        [Fact]
        public async Task Index_lista_os_clientes_ou_vazio_com_erro()
        {
            _clientes.Setup(c => c.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<ClienteDto>>([Cliente]));
            Assert.Single(Modelo<IEnumerable<ClienteDto>>(await Criar().Index()));

            _clientes.Setup(c => c.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<ClienteDto>>("fora"));
            var controller = Criar();
            Assert.Empty(Modelo<IEnumerable<ClienteDto>>(await controller.Index()));
            Assert.Equal("fora", controller.TempData["Error"]);
        }

        [Fact]
        public void Create_get_devolve_formulario_vazio()
        {
            Assert.IsType<ClienteFormViewModel>(Modelo<ClienteFormViewModel>(Criar().Create()));
        }

        [Fact]
        public async Task Create_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new ClienteFormViewModel { Nome = "Maria", Email = "m@x.com", CpfCnpj = "52998224725" };

            Assert.Same(form, Modelo<ClienteFormViewModel>(await Criar().InvalidarModelo().Create(form)));

            _clientes.Setup(c => c.AdicionarAsync(It.IsAny<ClienteCreateDto>())).ReturnsAsync(Erro<ClienteDto>("CPF duplicado"));
            var comErro = Criar();
            Assert.Same(form, Modelo<ClienteFormViewModel>(await comErro.Create(form)));
            Assert.False(comErro.ModelState.IsValid);

            _clientes.Setup(c => c.AdicionarAsync(It.Is<ClienteCreateDto>(d => d.Nome == "Maria"))).ReturnsAsync(Ok(Cliente));
            var ok = Criar();
            Redireciona(await ok.Create(form), "Index");
            Assert.NotNull(ok.TempData["Success"]);
        }

        [Fact]
        public async Task Edit_get_cobre_modelo_invalido_erro_e_sucesso()
        {
            var invalido = Criar().InvalidarModelo();
            Redireciona(await invalido.Edit(1), "Index");
            Assert.NotNull(invalido.TempData["Error"]);

            _clientes.Setup(c => c.ObterPorIdAsync("1")).ReturnsAsync(Erro<ClienteDto>());
            Redireciona(await Criar().Edit(1), "Index");

            _clientes.Setup(c => c.ObterPorIdAsync("1")).ReturnsAsync(Ok(Cliente));
            var model = Modelo<ClienteFormViewModel>(await Criar().Edit(1));
            Assert.Equal("Maria", model.Nome);
            Assert.True(model.Ativo);
        }

        [Fact]
        public async Task Edit_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new ClienteFormViewModel { Id = 1, Nome = "Maria", Email = "m@x.com", CpfCnpj = "52998224725" };
            Assert.Same(form, Modelo<ClienteFormViewModel>(await Criar().InvalidarModelo().Edit(1, form)));

            _clientes.Setup(c => c.AtualizarAsync("1", It.IsAny<ClienteUpdateDto>())).ReturnsAsync(Erro<ClienteDto>());
            Assert.Same(form, Modelo<ClienteFormViewModel>(await Criar().Edit(1, form)));

            _clientes.Setup(c => c.AtualizarAsync("1", It.IsAny<ClienteUpdateDto>())).ReturnsAsync(Ok(Cliente));
            Redireciona(await Criar().Edit(1, form), "Index");
        }

        [Fact]
        public async Task Delete_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Delete(1), "Index");

            _clientes.Setup(c => c.RemoverAsync("1")).ReturnsAsync(Erro<bool>("vinculado"));
            var erro = Criar();
            Redireciona(await erro.Delete(1), "Index");
            Assert.Equal("vinculado", erro.TempData["Error"]);

            _clientes.Setup(c => c.RemoverAsync("1")).ReturnsAsync(Ok(true));
            var ok = Criar();
            Redireciona(await ok.Delete(1), "Index");
            Assert.NotNull(ok.TempData["Success"]);
        }
    }

    public class VeiculosControllerTests
    {
        private readonly Mock<IVeiculoAppService> _veiculos = new();
        private readonly Mock<IClienteAppService> _clientes = new();

        public VeiculosControllerTests()
        {
            _clientes.Setup(c => c.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<ClienteDto>>(
            [
                new ClienteDto { Id = 2, Nome = "Zé", Ativo = true },
                new ClienteDto { Id = 1, Nome = "Ana", Ativo = true },
                new ClienteDto { Id = 3, Nome = "Inativo", Ativo = false }
            ]));
        }

        private VeiculosController Criar() => Preparar(new VeiculosController(_veiculos.Object, _clientes.Object));

        private static VeiculoDto Veiculo => new() { Id = 5, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Uno", Ano = 2020, Ativo = true, ClienteId = 1 };

        [Fact]
        public async Task Index_lista_ou_vazio_com_erro()
        {
            _veiculos.Setup(v => v.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<VeiculoDto>>([Veiculo]));
            Assert.Single(Modelo<IEnumerable<VeiculoDto>>(await Criar().Index()));

            _veiculos.Setup(v => v.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<VeiculoDto>>());
            Assert.Empty(Modelo<IEnumerable<VeiculoDto>>(await Criar().Index()));
        }

        [Fact]
        public async Task Create_get_lista_so_clientes_ativos_em_ordem()
        {
            var model = Modelo<VeiculoFormViewModel>(await Criar().Create());
            Assert.Equal(["Ana", "Zé"], model.Clientes.Select(c => c.Text));
        }

        [Fact]
        public async Task Create_get_com_erro_nos_clientes_deixa_lista_vazia()
        {
            _clientes.Setup(c => c.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<ClienteDto>>());
            Assert.Empty(Modelo<VeiculoFormViewModel>(await Criar().Create()).Clientes);
        }

        [Fact]
        public async Task Create_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new VeiculoFormViewModel { Placa = "ABC1D23", Marca = "Fiat", Modelo = "Uno", Ano = 2020, ClienteId = 1 };
            Assert.NotEmpty(Modelo<VeiculoFormViewModel>(await Criar().InvalidarModelo().Create(form)).Clientes);

            _veiculos.Setup(v => v.AdicionarAsync(It.IsAny<VeiculoCreateDto>())).ReturnsAsync(Erro<VeiculoDto>());
            Assert.Same(form, Modelo<VeiculoFormViewModel>(await Criar().Create(form)));

            _veiculos.Setup(v => v.AdicionarAsync(It.Is<VeiculoCreateDto>(d => d.Placa == "ABC1D23"))).ReturnsAsync(Ok(Veiculo));
            Redireciona(await Criar().Create(form), "Index");
        }

        [Fact]
        public async Task Edit_get_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Edit(5), "Index");

            _veiculos.Setup(v => v.ObterPorIdAsync("5")).ReturnsAsync(Erro<VeiculoDto>());
            Redireciona(await Criar().Edit(5), "Index");

            _veiculos.Setup(v => v.ObterPorIdAsync("5")).ReturnsAsync(Ok(Veiculo));
            var model = Modelo<VeiculoFormViewModel>(await Criar().Edit(5));
            Assert.Equal("ABC1D23", model.Placa);
            Assert.True(model.Clientes.Single(c => c.Value == "1").Selected);
        }

        [Fact]
        public async Task Edit_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new VeiculoFormViewModel { Id = 5, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Uno", Ano = 2020, ClienteId = 1 };
            Assert.Same(form, Modelo<VeiculoFormViewModel>(await Criar().InvalidarModelo().Edit(5, form)));

            _veiculos.Setup(v => v.AtualizarAsync("5", It.IsAny<VeiculoUpdateDto>())).ReturnsAsync(Erro<VeiculoDto>());
            Assert.Same(form, Modelo<VeiculoFormViewModel>(await Criar().Edit(5, form)));

            _veiculos.Setup(v => v.AtualizarAsync("5", It.IsAny<VeiculoUpdateDto>())).ReturnsAsync(Ok(Veiculo));
            Redireciona(await Criar().Edit(5, form), "Index");
        }

        [Fact]
        public async Task Delete_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Delete(5), "Index");

            _veiculos.Setup(v => v.RemoverAsync("5")).ReturnsAsync(Erro<bool>());
            Redireciona(await Criar().Delete(5), "Index");

            _veiculos.Setup(v => v.RemoverAsync("5")).ReturnsAsync(Ok(true));
            var ok = Criar();
            Redireciona(await ok.Delete(5), "Index");
            Assert.NotNull(ok.TempData["Success"]);
        }
    }

    public class EstoquesControllerTests
    {
        private readonly Mock<IEstoqueCliente> _estoque = new();
        private EstoquesController Criar() => Preparar(new EstoquesController(_estoque.Object));

        private static EstoqueDto Estoque => new() { Id = 3, Nome = "Óleo", Tipo = TipoEstoque.Insumo, QuantidadeAtual = 10, QuantidadeMinima = 2 };

        [Fact]
        public async Task Index_lista_ou_vazio_com_erro()
        {
            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<EstoqueDto>>([Estoque]));
            Assert.Single(Modelo<IEnumerable<EstoqueDto>>(await Criar().Index()));

            _estoque.Setup(e => e.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<EstoqueDto>>());
            Assert.Empty(Modelo<IEnumerable<EstoqueDto>>(await Criar().Index()));
        }

        [Fact]
        public void Create_get_oferece_os_tipos()
        {
            var model = Modelo<EstoqueFormViewModel>(Criar().Create());
            Assert.Equal(2, model.Tipos.Count());
        }

        [Fact]
        public async Task Create_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new EstoqueFormViewModel { Nome = "Óleo", Tipo = TipoEstoque.Insumo, QuantidadeAtual = 10, QuantidadeMinima = 2 };
            Assert.NotEmpty(Modelo<EstoqueFormViewModel>(await Criar().InvalidarModelo().Create(form)).Tipos);

            _estoque.Setup(e => e.AdicionarAsync(It.IsAny<EstoqueCreateDto>())).ReturnsAsync(Erro<EstoqueDto>());
            Assert.Same(form, Modelo<EstoqueFormViewModel>(await Criar().Create(form)));

            _estoque.Setup(e => e.AdicionarAsync(It.Is<EstoqueCreateDto>(d => d.QuantidadeAtual == 10))).ReturnsAsync(Ok(Estoque));
            Redireciona(await Criar().Create(form), "Index");
        }

        [Fact]
        public async Task Edit_get_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Edit(3), "Index");

            _estoque.Setup(e => e.ObterPorIdAsync("3")).ReturnsAsync(Erro<EstoqueDto>());
            Redireciona(await Criar().Edit(3), "Index");

            _estoque.Setup(e => e.ObterPorIdAsync("3")).ReturnsAsync(Ok(Estoque));
            var model = Modelo<EstoqueFormViewModel>(await Criar().Edit(3));
            Assert.Equal("Óleo", model.Nome);
            Assert.True(model.Tipos.Single(t => t.Value == ((int)TipoEstoque.Insumo).ToString()).Selected);
        }

        [Fact]
        public async Task Edit_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new EstoqueFormViewModel { Id = 3, Nome = "Óleo", Tipo = TipoEstoque.Insumo };
            Assert.Same(form, Modelo<EstoqueFormViewModel>(await Criar().InvalidarModelo().Edit(3, form)));

            _estoque.Setup(e => e.AtualizarAsync("3", It.IsAny<EstoqueUpdateDto>())).ReturnsAsync(Erro<EstoqueDto>());
            Assert.Same(form, Modelo<EstoqueFormViewModel>(await Criar().Edit(3, form)));

            _estoque.Setup(e => e.AtualizarAsync("3", It.IsAny<EstoqueUpdateDto>())).ReturnsAsync(Ok(Estoque));
            Redireciona(await Criar().Edit(3, form), "Index");
        }

        [Fact]
        public async Task Repor_get_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Repor(3), "Index");

            _estoque.Setup(e => e.ObterPorIdAsync("3")).ReturnsAsync(Erro<EstoqueDto>());
            Redireciona(await Criar().Repor(3), "Index");

            _estoque.Setup(e => e.ObterPorIdAsync("3")).ReturnsAsync(Ok(Estoque));
            Assert.Equal("Óleo", Modelo<EstoqueReposicaoRazorViewModel>(await Criar().Repor(3)).Nome);
        }

        [Fact]
        public async Task Repor_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new EstoqueReposicaoRazorViewModel { Id = 3, QuantidadeEntrada = 5 };
            Assert.Same(form, Modelo<EstoqueReposicaoRazorViewModel>(await Criar().InvalidarModelo().Repor(3, form)));

            _estoque.Setup(e => e.ReporQuantidadeAsync("3", It.IsAny<EstoqueReposicaoDto>())).ReturnsAsync(Erro<EstoqueDto>());
            Assert.Same(form, Modelo<EstoqueReposicaoRazorViewModel>(await Criar().Repor(3, form)));

            _estoque.Setup(e => e.ReporQuantidadeAsync("3", It.Is<EstoqueReposicaoDto>(d => d.QuantidadeEntrada == 5))).ReturnsAsync(Ok(Estoque));
            Redireciona(await Criar().Repor(3, form), "Index");
        }

        [Fact]
        public async Task Delete_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Delete(3), "Index");

            _estoque.Setup(e => e.RemoverAsync("3")).ReturnsAsync(Erro<bool>());
            Redireciona(await Criar().Delete(3), "Index");

            _estoque.Setup(e => e.RemoverAsync("3")).ReturnsAsync(Ok(true));
            Redireciona(await Criar().Delete(3), "Index");
        }
    }

    public class ServicosOficinaControllerTests
    {
        private readonly Mock<IServicoOficinaCliente> _servicos = new();
        private ServicosOficinaController Criar() => Preparar(new ServicosOficinaController(_servicos.Object));

        private static ServicoOficinaDto Servico => new() { Id = 4, Nome = "Alinhamento", Descricao = "x", ValorBase = 120, Ativo = true };

        [Fact]
        public async Task Index_lista_ou_vazio_com_erro()
        {
            _servicos.Setup(s => s.ObterTodosAsync()).ReturnsAsync(Ok<IEnumerable<ServicoOficinaDto>>([Servico]));
            Assert.Single(Modelo<IEnumerable<ServicoOficinaDto>>(await Criar().Index()));

            _servicos.Setup(s => s.ObterTodosAsync()).ReturnsAsync(Erro<IEnumerable<ServicoOficinaDto>>());
            Assert.Empty(Modelo<IEnumerable<ServicoOficinaDto>>(await Criar().Index()));
        }

        [Fact]
        public void Create_get_devolve_formulario()
        {
            Assert.True(Modelo<ServicoOficinaFormViewModel>(Criar().Create()).Ativo);
        }

        [Fact]
        public async Task Create_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new ServicoOficinaFormViewModel { Nome = "Alinhamento", ValorBase = 120 };
            Assert.Same(form, Modelo<ServicoOficinaFormViewModel>(await Criar().InvalidarModelo().Create(form)));

            _servicos.Setup(s => s.AdicionarAsync(It.IsAny<ServicoOficinaCreateDto>())).ReturnsAsync(Erro<ServicoOficinaDto>());
            Assert.Same(form, Modelo<ServicoOficinaFormViewModel>(await Criar().Create(form)));

            _servicos.Setup(s => s.AdicionarAsync(It.Is<ServicoOficinaCreateDto>(d => d.Descricao == string.Empty))).ReturnsAsync(Ok(Servico));
            Redireciona(await Criar().Create(form), "Index");
        }

        [Fact]
        public async Task Edit_get_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Edit(4), "Index");

            _servicos.Setup(s => s.ObterPorIdAsync("4")).ReturnsAsync(Erro<ServicoOficinaDto>());
            Redireciona(await Criar().Edit(4), "Index");

            _servicos.Setup(s => s.ObterPorIdAsync("4")).ReturnsAsync(Ok(Servico));
            Assert.Equal(120, Modelo<ServicoOficinaFormViewModel>(await Criar().Edit(4)).ValorBase);
        }

        [Fact]
        public async Task Edit_post_cobre_modelo_invalido_erro_e_sucesso()
        {
            var form = new ServicoOficinaFormViewModel { Id = 4, Nome = "Alinhamento", Descricao = "d", ValorBase = 120 };
            Assert.Same(form, Modelo<ServicoOficinaFormViewModel>(await Criar().InvalidarModelo().Edit(4, form)));

            _servicos.Setup(s => s.AtualizarAsync("4", It.IsAny<ServicoOficinaUpdateDto>())).ReturnsAsync(Erro<ServicoOficinaDto>());
            Assert.Same(form, Modelo<ServicoOficinaFormViewModel>(await Criar().Edit(4, form)));

            _servicos.Setup(s => s.AtualizarAsync("4", It.IsAny<ServicoOficinaUpdateDto>())).ReturnsAsync(Ok(Servico));
            Redireciona(await Criar().Edit(4, form), "Index");
        }

        [Fact]
        public async Task Delete_cobre_modelo_invalido_erro_e_sucesso()
        {
            Redireciona(await Criar().InvalidarModelo().Delete(4), "Index");

            _servicos.Setup(s => s.RemoverAsync("4")).ReturnsAsync(Erro<bool>());
            Redireciona(await Criar().Delete(4), "Index");

            _servicos.Setup(s => s.RemoverAsync("4")).ReturnsAsync(Ok(true));
            Redireciona(await Criar().Delete(4), "Index");
        }
    }
}
