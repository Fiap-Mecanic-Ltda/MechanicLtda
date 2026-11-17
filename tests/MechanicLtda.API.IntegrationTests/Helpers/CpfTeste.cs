namespace MechanicLtda.API.IntegrationTests.Helpers;

/// <summary>
/// Gera CPFs válidos e diferentes a cada chamada: o cadastro recusa CPF/CNPJ repetido, então
/// testes que criam vários clientes não podem usar sempre o mesmo documento.
/// </summary>
public static class CpfTeste
{
    public static string Novo()
    {
        var digitos = new int[11];
        for (var i = 0; i < 9; i++)
            digitos[i] = Random.Shared.Next(10);

        digitos[9]  = Verificador(digitos, 9);
        digitos[10] = Verificador(digitos, 10);
        return string.Concat(digitos);
    }

    private static int Verificador(int[] digitos, int tamanho)
    {
        var soma = 0;
        for (var i = 0; i < tamanho; i++)
            soma += digitos[i] * (tamanho + 1 - i);
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
