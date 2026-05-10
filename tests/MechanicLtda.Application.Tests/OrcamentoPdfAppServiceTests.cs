using MechanicLtda.Application.AppServices;
using MechanicLtda.Application.DTOs;
using Xunit;

namespace MechanicLtda.Application.Tests;

public class OrcamentoPdfAppServiceTests
{
    private readonly OrcamentoPdfAppService _sut;

    public OrcamentoPdfAppServiceTests()
    {
        _sut = new OrcamentoPdfAppService();
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static OrcamentoPdfDto CriarDto(
        int id                  = 1,
        int ordemServicoId      = 10,
        DateTime? validade      = null,
        string? clienteTelefone = "11999999999") =>
        new()
        {
            Id                  = id,
            OrdemServicoId      = ordemServicoId,
            DataGeracao         = DateTime.UtcNow,
            Validade            = validade ?? DateTime.UtcNow.AddDays(30),
            ValorTotalPecas     = 500m,
            ValorTotalInsumos   = 200m,
            ValorTotalGeral     = 700m,
            DescricaoProblema   = "Barulho no motor",
            StatusOrdemServico  = "Em Execução",
            ClienteNome         = "João Silva",
            ClienteEmail        = "joao@email.com",
            ClienteTelefone     = clienteTelefone,
            VeiculoMarca        = "Toyota",
            VeiculoModelo       = "Corolla",
            VeiculoPlaca        = "ABC1234",
            VeiculoAno          = 2022
        };

    // ─── OrcamentoPdfDto ────────────────────────────────────────────────────────

    #region OrcamentoPdfDto

    [Fact]
    public void OrcamentoPdfDto_QuandoInstanciado_DevePermitirAtribuicaoDasPropriedades()
    {
        // Arrange
        var dataGeracao = DateTime.UtcNow;
        var validade    = dataGeracao.AddDays(30);

        // Act
        var dto = new OrcamentoPdfDto
        {
            Id                 = 42,
            OrdemServicoId     = 7,
            DataGeracao        = dataGeracao,
            Validade           = validade,
            ValorTotalPecas    = 300m,
            ValorTotalInsumos  = 100m,
            ValorTotalGeral    = 400m,
            DescricaoProblema  = "Troca de óleo",
            StatusOrdemServico = "Finalizada",
            ClienteNome        = "Maria Souza",
            ClienteEmail       = "maria@email.com",
            ClienteTelefone    = "11988887777",
            VeiculoMarca       = "Honda",
            VeiculoModelo      = "Civic",
            VeiculoPlaca       = "XYZ5678",
            VeiculoAno         = 2020
        };

        // Assert
        Assert.Equal(42,            dto.Id);
        Assert.Equal(7,             dto.OrdemServicoId);
        Assert.Equal(dataGeracao,   dto.DataGeracao);
        Assert.Equal(validade,      dto.Validade);
        Assert.Equal(300m,          dto.ValorTotalPecas);
        Assert.Equal(100m,          dto.ValorTotalInsumos);
        Assert.Equal(400m,          dto.ValorTotalGeral);
        Assert.Equal("Troca de óleo",   dto.DescricaoProblema);
        Assert.Equal("Finalizada",       dto.StatusOrdemServico);
        Assert.Equal("Maria Souza",      dto.ClienteNome);
        Assert.Equal("maria@email.com",  dto.ClienteEmail);
        Assert.Equal("11988887777",      dto.ClienteTelefone);
        Assert.Equal("Honda",            dto.VeiculoMarca);
        Assert.Equal("Civic",            dto.VeiculoModelo);
        Assert.Equal("XYZ5678",          dto.VeiculoPlaca);
        Assert.Equal(2020,               dto.VeiculoAno);
    }

    [Fact]
    public void OrcamentoPdfDto_ClienteTelefone_DeveAceitarNulo()
    {
        // Act
        var dto = new OrcamentoPdfDto { ClienteTelefone = null };

        // Assert
        Assert.Null(dto.ClienteTelefone);
    }

    [Fact]
    public void OrcamentoPdfDto_Validade_DeveAceitarNulo()
    {
        // Act
        var dto = new OrcamentoPdfDto { Validade = null };

        // Assert
        Assert.Null(dto.Validade);
    }

    #endregion

    // ─── Gerar ──────────────────────────────────────────────────────────────────

    #region Gerar

    [Fact]
    public void Gerar_ComDtoCompleto_DeveRetornarBytesNaoVazios()
    {
        // Arrange
        var dto = CriarDto();

        // Act
        var resultado = _sut.Gerar(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado);
    }

    [Fact]
    public void Gerar_ComValidadeNula_NaoDeveLancarExcecao()
    {
        // Arrange
        var dto = CriarDto(validade: null);
        dto.Validade = null;

        // Act
        var resultado = _sut.Gerar(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado);
    }

    [Fact]
    public void Gerar_ComClienteTelefoneNulo_NaoDeveLancarExcecao()
    {
        // Arrange
        var dto = CriarDto(clienteTelefone: null);

        // Act
        var resultado = _sut.Gerar(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado);
    }

    [Fact]
    public void Gerar_ComValoresZerados_DeveRetornarPdfValido()
    {
        // Arrange
        var dto = CriarDto();
        dto.ValorTotalPecas   = 0m;
        dto.ValorTotalInsumos = 0m;
        dto.ValorTotalGeral   = 0m;

        // Act
        var resultado = _sut.Gerar(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado);
    }

    [Fact]
    public void Gerar_ComDadosMinimos_DeveRetornarPdfValido()
    {
        // Arrange
        var dto = new OrcamentoPdfDto
        {
            Id                 = 1,
            OrdemServicoId     = 1,
            DataGeracao        = DateTime.UtcNow,
            Validade           = null,
            ValorTotalPecas    = 0m,
            ValorTotalInsumos  = 0m,
            ValorTotalGeral    = 0m,
            DescricaoProblema  = string.Empty,
            StatusOrdemServico = string.Empty,
            ClienteNome        = string.Empty,
            ClienteEmail       = string.Empty,
            ClienteTelefone    = null,
            VeiculoMarca       = string.Empty,
            VeiculoModelo      = string.Empty,
            VeiculoPlaca       = string.Empty,
            VeiculoAno         = 0
        };

        // Act
        var resultado = _sut.Gerar(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado);
    }

    [Fact]
    public void Gerar_DeveSempreRetornarPdfComAssinaturaPdf()
    {
        // Arrange
        var dto = CriarDto();

        // Act
        var resultado = _sut.Gerar(dto);

        // Assert — PDFs começam com "%PDF"
        Assert.True(resultado.Length >= 4);
        Assert.Equal(0x25, resultado[0]); // '%'
        Assert.Equal(0x50, resultado[1]); // 'P'
        Assert.Equal(0x44, resultado[2]); // 'D'
        Assert.Equal(0x46, resultado[3]); // 'F'
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Gerar_ComDiferentesIds_DeveRetornarPdfValido(int id)
    {
        // Arrange
        var dto = CriarDto(id: id);

        // Act
        var resultado = _sut.Gerar(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado);
    }

    #endregion
}