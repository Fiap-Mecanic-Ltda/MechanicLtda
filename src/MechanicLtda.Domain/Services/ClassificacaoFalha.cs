namespace MechanicLtda.Domain.Services
{
    /// <summary>
    /// Separa falha de regra de negócio (transição de status inválida, OS inexistente,
    /// dado inválido) de falha de sistema (banco fora, bug, timeout).
    ///
    /// Só a segunda deve acordar alguém: um alerta que dispara toda vez que um funcionário
    /// tenta finalizar uma OS que ainda não está em execução vira ruído e passa a ser
    /// ignorado — inclusive quando for grave.
    /// </summary>
    public static class ClassificacaoFalha
    {
        public const string Negocio = "Negocio";
        public const string Sistema = "Sistema";

        public static string Classificar(Exception excecao) => excecao switch
        {
            // Derivam de exceções "de negócio", mas indicam bug ou recurso já liberado.
            ObjectDisposedException or ArgumentNullException => Sistema,

            // Tipos que o domínio lança para violação de regra.
            KeyNotFoundException or InvalidOperationException or ArgumentException => Negocio,

            _ => Sistema
        };
    }
}
