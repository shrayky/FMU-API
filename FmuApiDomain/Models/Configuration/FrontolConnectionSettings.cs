
namespace FmuApiDomain.Models.Configuration
{
    public class FrontolConnectionSettings
    {
        public string Path { get; set; } = "";
        public string UserName { get; set; } = "SYSDBA";
        public string Password { get; set; } = "masterkey";

        public bool ConnectionEnable() =>
            (Path.Length > 0 && UserName.Length > 0 && Password.Length > 0);

        public string ConnectionStringBuild()
        {
            if (Path == string.Empty)
                return string.Empty;

            if (UserName == string.Empty)
                return string.Empty;

            if (Password == string.Empty)
                return string.Empty;

            return $"Database={Path};user={UserName};password={Password};Dialect=3;";
        }
    }
}
