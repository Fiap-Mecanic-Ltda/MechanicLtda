namespace MechanicLtda.Web.Authorization
{
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Funcionario   = "Funcionario";
        public const string Cliente       = "Cliente";
        public const string Admin = $"{Administrador},{Funcionario}";
        public const string AdminOuCliente = $"{Administrador},{Funcionario},{Cliente}";
    }
}

