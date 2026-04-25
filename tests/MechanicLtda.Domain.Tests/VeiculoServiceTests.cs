using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Repositories;
using MechanicLtda.Domain.Interfaces.Services;
using MechanicLtda.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MechanicLtda.Domain.Tests;

public class VeiculoServiceTests
{
    private readonly Mock<IVeiculoRepository> _veiculoRepositoryMock;
    private readonly Mock<IClienteRepository> _clienteRepositoryMock;
    private readonly Mock<INotificadorService> _notificadorMock;
    private readonly Mock<ILogger<VeiculoService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly VeiculoService _sut;

    public VeiculoServiceTests()
    {
        _veiculoRepositoryMock = new Mock<IVeiculoRepository>();
        _clienteRepositoryMock = new Mock<IClienteRepository>();
        _notificadorMock = new Mock<INotificadorService>();
        _loggerMock = new Mock<ILogger<VeiculoService>>();
        _configurationMock = new Mock<IConfiguration>();

        _sut = new VeiculoService(
            _loggerMock.Object,
            _configurationMock.Object,
            _notificadorMock.Object,
            _veiculoRepositoryMock.Object,
            _clienteRepositoryMock.Object);
    }

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoClienteExisteEPlacaDisponivel_DeveRetornarVeiculoCriado()
    {
        // Arrange
        var placa = "ABC1234";
        var marca = "Toyota";
        var modelo = "Corolla";
        var ano = 2022;
        var clienteId = 1;

        var cliente = new Cliente { Id = clienteId, Nome = "João Silva", Email = "joao@email.com", Ativo = true, DataCriacao = DateTime.Now };
        var veiculoEsperado = new Veiculo
        {
            Placa = placa.ToUpper(),
            Marca = marca,
            Modelo = modelo,
            Ano = ano,
            ClienteId = clienteId,
            Ativo = true,
            DataCriacao = DateTime.Now
        };

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId.ToString()))
            .ReturnsAsync(cliente);

        _veiculoRepositoryMock
            .Setup(r => r.PlacaExisteAsync(placa))
            .ReturnsAsync(false);

        _veiculoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Veiculo>()))
            .ReturnsAsync(veiculoEsperado);

        // Act
        var resultado = await _sut.AdicionarAsync(placa, marca, modelo, ano, clienteId);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(placa.ToUpper(), resultado.Placa);
        Assert.Equal(clienteId, resultado.ClienteId);
        Assert.True(resultado.Ativo);
        _veiculoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Veiculo>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoClienteNaoExiste_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AdicionarAsync("XYZ9999", "Fiat", "Uno", 2020, 99));

        _veiculoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Veiculo>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoPlacaJaExiste_DeveLancarInvalidOperationException()
    {
        // Arrange
        var clienteId = 1;
        var placa = "DUP1234";

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId.ToString()))
            .ReturnsAsync(new Cliente { Id = clienteId, Nome = "Maria", Email = "maria@email.com", Ativo = true, DataCriacao = DateTime.Now });

        _veiculoRepositoryMock
            .Setup(r => r.PlacaExisteAsync(placa))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AdicionarAsync(placa, "Honda", "Civic", 2021, clienteId));

        _veiculoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Veiculo>()), Times.Never);
    }

    #endregion

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoVeiculoExisteEPlacaDisponivel_DeveRetornarVeiculoAtualizado()
    {
        // Arrange
        var id = 1;
        var clienteId = 2;
        var veiculo = new Veiculo { Id = id, Placa = "NEW1234", Marca = "Ford", Modelo = "Ka", Ano = 2023, ClienteId = clienteId, Ativo = true };
        var existente = new Veiculo { Id = id, Placa = "OLD1234", Marca = "Ford", Modelo = "Ka", Ano = 2022, ClienteId = clienteId, Ativo = true, DataCriacao = DateTime.Now };
        var cliente = new Cliente { Id = clienteId, Nome = "Ana", Email = "ana@email.com", Ativo = true, DataCriacao = DateTime.Now };

        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(id.ToString())).ReturnsAsync(existente);
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync(clienteId.ToString())).ReturnsAsync(cliente);
        _veiculoRepositoryMock.Setup(r => r.PlacaExisteAsync(veiculo.Placa)).ReturnsAsync(false);
        _veiculoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Veiculo>())).ReturnsAsync(veiculo);

        // Act
        var resultado = await _sut.AtualizarAsync(veiculo);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotNull(veiculo.DataModificacao);
        _veiculoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Veiculo>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoVeiculoNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var veiculo = new Veiculo { Id = 999, Placa = "XXX0000", ClienteId = 1 };

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(veiculo.Id.ToString()))
            .ReturnsAsync((Veiculo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AtualizarAsync(veiculo));

        _veiculoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Veiculo>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoClienteNaoEncontrado_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var veiculo = new Veiculo { Id = 1, Placa = "ABC1234", ClienteId = 99 };
        var existente = new Veiculo { Id = 1, Placa = "ABC1234", ClienteId = 1, DataCriacao = DateTime.Now };

        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync("99")).ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.AtualizarAsync(veiculo));

        _veiculoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Veiculo>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoPlacaEmUsoDeOutroVeiculo_DeveLancarInvalidOperationException()
    {
        // Arrange
        var veiculo = new Veiculo { Id = 1, Placa = "NOVA123", ClienteId = 1 };
        var existente = new Veiculo { Id = 1, Placa = "VELHA12", ClienteId = 1, DataCriacao = DateTime.Now };
        var cliente = new Cliente { Id = 1, Nome = "Carlos", Email = "carlos@email.com", Ativo = true, DataCriacao = DateTime.Now };

        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(existente);
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync("1")).ReturnsAsync(cliente);
        _veiculoRepositoryMock.Setup(r => r.PlacaExisteAsync(veiculo.Placa)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AtualizarAsync(veiculo));

        _veiculoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Veiculo>()), Times.Never);
    }

    #endregion

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_DeveRetornarListaDeVeiculos()
    {
        // Arrange
        var lista = new List<Veiculo>
        {
            new() { Id = 1, Placa = "AAA1111", Marca = "Fiat", Modelo = "Palio",  Ano = 2019, ClienteId = 1 },
            new() { Id = 2, Placa = "BBB2222", Marca = "VW",   Modelo = "Gol",    Ano = 2020, ClienteId = 2 }
        };

        _veiculoRepositoryMock.Setup(r => r.ObterTodosAsync()).ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterTodosAsync();

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count());
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoRepositorioLancaExcecao_DeveRePropagar()
    {
        // Arrange
        _veiculoRepositoryMock
            .Setup(r => r.ObterTodosAsync())
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.ObterTodosAsync());
    }

    #endregion

    #region ObterPorClienteIdAsync

    [Fact]
    public async Task ObterPorClienteIdAsync_DeveRetornarVeiculosDoCliente()
    {
        // Arrange
        var clienteId = 1;
        var lista = new List<Veiculo>
        {
            new() { Id = 1, Placa = "CCC3333", Marca = "Chevrolet", Modelo = "Onix", Ano = 2021, ClienteId = clienteId }
        };

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorClienteIdAsync(clienteId))
            .ReturnsAsync(lista);

        // Act
        var resultado = await _sut.ObterPorClienteIdAsync(clienteId.ToString());

        // Assert
        Assert.NotNull(resultado);
        Assert.Single(resultado);
        Assert.All(resultado, v => Assert.Equal(clienteId, v.ClienteId));
    }

    #endregion

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoVeiculoExiste_DeveChamarRepositorio()
    {
        // Arrange
        var id = "1";
        var veiculo = new Veiculo { Id = 1, Placa = "DDD4444" };

        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(veiculo);
        _veiculoRepositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _sut.RemoverAsync(id);

        // Assert
        _veiculoRepositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoVeiculoNaoExiste_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var id = "999";

        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Veiculo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RemoverAsync(id));

        _veiculoRepositoryMock.Verify(r => r.RemoverAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    // ─── Dados Sensíveis — Placa ─────────────────────────────────────────────────

    #region DadosSensiveis_Placa

    [Fact]
    public async Task AdicionarAsync_DeveConverterPlacaParaMaiusculas()
    {
        // Arrange
        var placaMinuscula = "abc1234";
        var clienteId = 1;
        Veiculo veiculoSalvo = null!;

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId.ToString()))
            .ReturnsAsync(new Cliente { Id = clienteId, Nome = "João", Email = "j@email.com", CpfCnpj = "52998224725", Ativo = true, DataCriacao = DateTime.Now });

        _veiculoRepositoryMock
            .Setup(r => r.PlacaExisteAsync(placaMinuscula))
            .ReturnsAsync(false);

        _veiculoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Veiculo>()))
            .Callback<Veiculo>(v => veiculoSalvo = v)
            .ReturnsAsync((Veiculo v) => v);

        // Act
        await _sut.AdicionarAsync(placaMinuscula, "Toyota", "Corolla", 2022, clienteId);

        // Assert
        Assert.NotNull(veiculoSalvo);
        Assert.Equal("ABC1234", veiculoSalvo.Placa);
    }

    [Theory]
    [InlineData("abc1234", "ABC1234")]      // formato antigo minúsculo
    [InlineData("ABC1234", "ABC1234")]      // formato antigo já maiúsculo
    [InlineData("abc1d23", "ABC1D23")]      // Mercosul minúsculo
    [InlineData("ABC1D23", "ABC1D23")]      // Mercosul já maiúsculo
    public async Task AdicionarAsync_SempreDeveArmazenarPlacaEmMaiusculas(string placaEntrada, string placaEsperada)
    {
        // Arrange
        var clienteId = 1;
        Veiculo veiculoSalvo = null!;

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId.ToString()))
            .ReturnsAsync(new Cliente { Id = clienteId, Nome = "Ana", Email = "ana@email.com", CpfCnpj = "52998224725", Ativo = true, DataCriacao = DateTime.Now });

        _veiculoRepositoryMock
            .Setup(r => r.PlacaExisteAsync(placaEntrada))
            .ReturnsAsync(false);

        _veiculoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Veiculo>()))
            .Callback<Veiculo>(v => veiculoSalvo = v)
            .ReturnsAsync((Veiculo v) => v);

        // Act
        await _sut.AdicionarAsync(placaEntrada, "Honda", "Civic", 2023, clienteId);

        // Assert
        Assert.Equal(placaEsperada, veiculoSalvo.Placa);
    }

    [Fact]
    public async Task AdicionarAsync_PlacaMercosulEmMaiusculas_DeveSerArmazenadaCorretamente()
    {
        // Arrange
        var placa = "BRA2E19";
        var clienteId = 1;
        Veiculo veiculoSalvo = null!;

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId.ToString()))
            .ReturnsAsync(new Cliente { Id = clienteId, Nome = "Carlos", Email = "c@email.com", CpfCnpj = "52998224725", Ativo = true, DataCriacao = DateTime.Now });

        _veiculoRepositoryMock
            .Setup(r => r.PlacaExisteAsync(placa))
            .ReturnsAsync(false);

        _veiculoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Veiculo>()))
            .Callback<Veiculo>(v => veiculoSalvo = v)
            .ReturnsAsync((Veiculo v) => v);

        // Act
        await _sut.AdicionarAsync(placa, "VW", "Polo", 2024, clienteId);

        // Assert
        Assert.Equal(placa.ToUpper(), veiculoSalvo.Placa);
    }

    #endregion
}