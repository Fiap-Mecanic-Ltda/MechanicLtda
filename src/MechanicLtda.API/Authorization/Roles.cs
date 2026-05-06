namespace MechanicLtda.API.Authorization
{
    /// <summary>
    /// Constantes de roles utilizadas na autorização da API.
    /// </summary>
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Funcionario   = "Funcionario";
        public const string Cliente       = "Cliente";

        /// <summary>Roles com acesso administrativo completo.</summary>
        public const string Admin = $"{Administrador},{Funcionario}";

        /// <summary>Roles com acesso a consultar progresso de OS.</summary>
        public const string AdminOuCliente = $"{Administrador},{Funcionario},{Cliente}";
    }
}