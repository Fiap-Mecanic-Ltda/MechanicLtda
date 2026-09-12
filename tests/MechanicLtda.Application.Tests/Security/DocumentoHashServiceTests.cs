using MechanicLtda.Application.Security;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace MechanicLtda.Application.Tests.Security;

public class DocumentoHashServiceTests
{
    private const string Chave = "chave-de-teste-do-hash-de-cpf-1234567890";

    /// <summary>
    /// Vetor de contrato com a Function serverless de autenticação: HMAC-SHA256 dos
    /// dígitos "52998224725" com a chave acima. Se este valor mudar, o hash gravado pela
    /// API deixa de casar com o hash calculado pela Lambda e a autenticação por CPF para
    /// de encontrar o cliente.
    /// </summary>
    private const string HashEsperadoDoCpf =
        "f865a3cc9a1cab6bba801ce82a120b4ed304a74dcfbdd31b8ba48c0cc6cc2dd8";

    private static DocumentoHashService CriarServico(string? chave = Chave)
    {
        // A variável de ambiente tem precedência sobre a configuração; limpa para o teste
        // depender apenas do que foi injetado.
        Environment.SetEnvironmentVariable("CPF_HASH_KEY", null);

        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Encryption:CpfCnpjHashKey"]).Returns(chave);

        return new DocumentoHashService(configuration.Object);
    }

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData(" 529 982 247 25 ", "52998224725")]
    [InlineData("", "")]
    public void Normalizar_DeveManterApenasOsDigitos(string entrada, string esperado)
    {
        var sut = CriarServico();

        Assert.Equal(esperado, sut.Normalizar(entrada));
    }

    [Fact]
    public void GerarHash_ComEeSemMascara_DeveProduzirOMesmoHash()
    {
        var sut = CriarServico();

        Assert.Equal(sut.GerarHash("52998224725"), sut.GerarHash("529.982.247-25"));
    }

    [Fact]
    public void GerarHash_DeveHonrarOVetorDeContratoComALambda()
    {
        var sut = CriarServico();

        Assert.Equal(HashEsperadoDoCpf, sut.GerarHash("529.982.247-25"));
    }

    [Fact]
    public void GerarHash_ParaDocumentosDiferentes_DeveProduzirHashesDiferentes()
    {
        var sut = CriarServico();

        Assert.NotEqual(sut.GerarHash("52998224725"), sut.GerarHash("11144477735"));
    }

    [Fact]
    public void GerarHash_ComChavesDiferentes_DeveProduzirHashesDiferentes()
    {
        var comChaveA = CriarServico();
        var comChaveB = CriarServico("outra-chave-de-teste-do-hash-0987654321");

        Assert.NotEqual(comChaveA.GerarHash("52998224725"), comChaveB.GerarHash("52998224725"));
    }

    [Fact]
    public void GerarHash_SemChaveConfigurada_DeveLancarInvalidOperationException()
    {
        var sut = CriarServico(chave: null);

        Assert.Throws<InvalidOperationException>(() => sut.GerarHash("52998224725"));
    }

    [Fact]
    public void GerarHash_ComDocumentoSemDigitos_DeveLancarArgumentException()
    {
        var sut = CriarServico();

        Assert.Throws<ArgumentException>(() => sut.GerarHash("sem digitos"));
    }

    [Fact]
    public void EstaConfigurado_DeveRefletirAPresencaDaChave()
    {
        Assert.True(CriarServico().EstaConfigurado);
        Assert.False(CriarServico(chave: null).EstaConfigurado);
        Assert.False(CriarServico(chave: "   ").EstaConfigurado);
    }
}
