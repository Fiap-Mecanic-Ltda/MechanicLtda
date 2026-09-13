using MechanicLtda.Domain.Services;
using Xunit;

namespace MechanicLtda.Domain.Tests;

/// <summary>
/// Só falha de Sistema dispara alerta. Se uma violação de regra de negócio for
/// classificada como Sistema, o alerta vira ruído; se um erro de infraestrutura for
/// classificado como Negocio, ninguém fica sabendo.
/// </summary>
public class ClassificacaoFalhaTests
{
    [Fact]
    public void Classificar_TransicaoDeStatusInvalida_DeveSerNegocio()
    {
        Assert.Equal(ClassificacaoFalha.Negocio,
            ClassificacaoFalha.Classificar(new InvalidOperationException("A OS só pode ser finalizada em execução.")));
    }

    [Fact]
    public void Classificar_OrdemServicoInexistente_DeveSerNegocio()
    {
        Assert.Equal(ClassificacaoFalha.Negocio,
            ClassificacaoFalha.Classificar(new KeyNotFoundException("OS não encontrada.")));
    }

    [Fact]
    public void Classificar_DadoInvalido_DeveSerNegocio()
    {
        Assert.Equal(ClassificacaoFalha.Negocio,
            ClassificacaoFalha.Classificar(new ArgumentException("VeiculoId inválido")));
    }

    [Fact]
    public void Classificar_ArgumentoNulo_DeveSerSistema()
    {
        // ArgumentNullException deriva de ArgumentException, mas indica bug.
        Assert.Equal(ClassificacaoFalha.Sistema,
            ClassificacaoFalha.Classificar(new ArgumentNullException("ordemServico")));
    }

    [Fact]
    public void Classificar_RecursoJaLiberado_DeveSerSistema()
    {
        // ObjectDisposedException deriva de InvalidOperationException, mas indica bug.
        Assert.Equal(ClassificacaoFalha.Sistema,
            ClassificacaoFalha.Classificar(new ObjectDisposedException("BancoAPIContext")));
    }

    [Theory]
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(NullReferenceException))]
    [InlineData(typeof(Exception))]
    public void Classificar_FalhaDeInfraestruturaOuBug_DeveSerSistema(Type tipo)
    {
        var excecao = (Exception)Activator.CreateInstance(tipo)!;

        Assert.Equal(ClassificacaoFalha.Sistema, ClassificacaoFalha.Classificar(excecao));
    }
}
