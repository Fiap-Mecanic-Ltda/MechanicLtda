using AutoMapper;
using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class VeiculoAppServiceTests
{
    private readonly Mock<IVeiculoService> _serviceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly VeiculoAppService _sut;

    public VeiculoAppServiceTests()
    {
        _serviceMock = new Mock<IVeiculoService>();
        _mapperMock = new Mock<IMapper>();
        _sut = new VeiculoAppService(_serviceMock.Object, _mapperMock.Object);
    }

    #region AdicionarAsync

    [Fact]
    public async Task AdicionarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var dto = new VeiculoCreateDto { Placa = "ABC1234", Marca = "Toyota", Modelo = "Corolla", Ano = 2022, ClienteId = 1 };

        var veiculoCriado = new Veiculo { Id = 1, Placa = dto.Placa, Marca = dto.Marca, Modelo = dto.Modelo, Ano = dto.Ano, ClienteId = dto.ClienteId, Ativo = true };
        var veiculoDto = new VeiculoDto { Id = 1, Placa = dto.Placa, Marca = dto.Marca, Modelo = dto.Modelo, Ano = dto.Ano, ClienteId = dto.ClienteId, Ativo = true };

        _serviceMock
            .Setup(s => s.AdicionarAsync(dto.Placa, dto.Marca, dto.Modelo, dto.Ano, dto.ClienteId))
            .ReturnsAsync(veiculoCriado);

        _mapperMock
            .Setup(m => m.Map<VeiculoDto>(veiculoCriado))
            .Returns(veiculoDto);

        // Act
        var response = await _sut.AdicionarAsync(dto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(veiculoDto, response.getResponse);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoPlacaJaCadastrada_DeveRetornarResponseComErro()
    {
        // Arrange
        var dto = new VeiculoCreateDto { Placa = "DUP1234", Marca = "Honda", Modelo = "Civic", Ano = 2021, ClienteId = 1 };

        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Já existe um veículo com a placa informada."));

        // Act
        var response = await _sut.AdicionarAsync(dto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoClienteNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var dto = new VeiculoCreateDto { Placa = "XYZ9999", Marca = "Fiat", Modelo = "Uno", Ano = 2020, ClienteId = 99 };

        _serviceMock
            .Setup(s => s.AdicionarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Cliente não encontrado."));

        // Act
        var response = await _sut.AdicionarAsync(dto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    #region AtualizarAsync

    [Fact]
    public async Task AtualizarAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var id = "1";
        var dto = new VeiculoUpdateDto { Placa = "NEW1234", Marca = "Ford", Modelo = "Ka", Ano = 2023, Ativo = true, ClienteId = 1 };

        var entidade = new Veiculo { Id = int.Parse(id), Placa = dto.Placa, Marca = dto.Marca, Modelo = dto.Modelo, Ano = dto.Ano, ClienteId = dto.ClienteId };
        var veiculoDto = new VeiculoDto { Id = int.Parse(id), Placa = dto.Placa, Marca = dto.Marca, Modelo = dto.Modelo, Ano = dto.Ano, ClienteId = dto.ClienteId, Ativo = true };

        _mapperMock
            .Setup(m => m.Map<Veiculo>(dto))
            .Returns(entidade);

        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<Veiculo>()))
            .ReturnsAsync(entidade);

        _mapperMock
            .Setup(m => m.Map<VeiculoDto>(entidade))
            .Returns(veiculoDto);

        // Act
        var response = await _sut.AtualizarAsync(id, dto);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(veiculoDto, response.getResponse);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoVeiculoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";
        var dto = new VeiculoUpdateDto { Placa = "XXX0000", Marca = "VW", Modelo = "Gol", Ano = 2019, Ativo = true, ClienteId = 1 };

        _mapperMock
            .Setup(m => m.Map<Veiculo>(dto))
            .Returns(new Veiculo());

        _serviceMock
            .Setup(s => s.AtualizarAsync(It.IsAny<Veiculo>()))
            .ThrowsAsync(new KeyNotFoundException($"Veículo com Id '{id}' não encontrado."));

        // Act
        var response = await _sut.AtualizarAsync(id, dto);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    #region ObterTodosAsync

    [Fact]
    public async Task ObterTodosAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var veiculos = new List<Veiculo>
        {
            new() { Id = 1, Placa = "AAA1111", Marca = "Fiat", Modelo = "Palio", Ano = 2019, ClienteId = 1 },
            new() { Id = 2, Placa = "BBB2222", Marca = "VW",   Modelo = "Gol",   Ano = 2020, ClienteId = 2 }
        };

        var veiculosDto = veiculos.Select(v => new VeiculoDto { Id = v.Id, Placa = v.Placa, ClienteId = v.ClienteId }).ToList();

        _serviceMock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(veiculos);
        _mapperMock.Setup(m => m.Map<IEnumerable<VeiculoDto>>(veiculos)).Returns(veiculosDto);

        // Act
        var response = await _sut.ObterTodosAsync();

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(2, response.getResponse.Count());
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterTodosAsync())
            .ThrowsAsync(new Exception("Erro no banco"));

        // Act
        var response = await _sut.ObterTodosAsync();

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    #region ObterPorClienteIdAsync

    [Fact]
    public async Task ObterPorClienteIdAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var clienteId = "1";
        var veiculos = new List<Veiculo>
        {
            new() { Id = 1, Placa = "CCC3333", Marca = "Chevrolet", Modelo = "Onix", Ano = 2021, ClienteId = 1 }
        };

        var veiculosDto = veiculos.Select(v => new VeiculoDto { Id = v.Id, Placa = v.Placa, ClienteId = v.ClienteId }).ToList();

        _serviceMock.Setup(s => s.ObterPorClienteIdAsync(clienteId)).ReturnsAsync(veiculos);
        _mapperMock.Setup(m => m.Map<IEnumerable<VeiculoDto>>(veiculos)).Returns(veiculosDto);

        // Act
        var response = await _sut.ObterPorClienteIdAsync(clienteId);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Single(response.getResponse);
    }

    [Fact]
    public async Task ObterPorClienteIdAsync_QuandoServicoLancaExcecao_DeveRetornarResponseComErro()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.ObterPorClienteIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Erro ao consultar veículos do cliente"));

        // Act
        var response = await _sut.ObterPorClienteIdAsync("99");

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    #region ObterPorIdAsync

    [Fact]
    public async Task ObterPorIdAsync_QuandoVeiculoEncontrado_DeveRetornarResponseSemErros()
    {
        // Arrange
        var id = "1";
        var veiculo = new Veiculo { Id = 1, Placa = "DDD4444", Marca = "BMW", Modelo = "X1", Ano = 2024, ClienteId = 1 };
        var veiculoDto = new VeiculoDto { Id = 1, Placa = "DDD4444", Marca = "BMW", Modelo = "X1", Ano = 2024, ClienteId = 1 };

        _serviceMock.Setup(s => s.ObterPorIdAsync(id)).ReturnsAsync(veiculo);
        _mapperMock.Setup(m => m.Map<VeiculoDto>(veiculo)).Returns(veiculoDto);

        // Act
        var response = await _sut.ObterPorIdAsync(id);

        // Assert
        Assert.False(response.hasErrors);
        Assert.Equal(veiculoDto, response.getResponse);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoVeiculoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";

        _serviceMock.Setup(s => s.ObterPorIdAsync(id)).ReturnsAsync((Veiculo?)null);

        // Act
        var response = await _sut.ObterPorIdAsync(id);

        // Assert
        Assert.True(response.hasErrors);
        Assert.Null(response.getResponse);
    }

    #endregion

    #region RemoverAsync

    [Fact]
    public async Task RemoverAsync_QuandoSucesso_DeveRetornarResponseSemErros()
    {
        // Arrange
        var id = "1";

        _serviceMock.Setup(s => s.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        var response = await _sut.RemoverAsync(id);

        // Assert
        Assert.False(response.hasErrors);
        Assert.True(response.getResponse);
        _serviceMock.Verify(s => s.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoVeiculoNaoEncontrado_DeveRetornarResponseComErro()
    {
        // Arrange
        var id = "999";

        _serviceMock
            .Setup(s => s.RemoverAsync(id))
            .ThrowsAsync(new KeyNotFoundException($"Veículo com Id '{id}' não encontrado."));

        // Act
        var response = await _sut.RemoverAsync(id);

        // Assert
        Assert.True(response.hasErrors);
        Assert.False(response.getResponse);
    }

    #endregion
}