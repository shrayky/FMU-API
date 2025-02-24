using System.Text;

namespace FmuApiDomain.Configuration.Options
{
    public class LocalModuleConnection
    {
        public bool Enable { get; set; } = false;
        public string ConnectionAddress { get; set; } = @"http://localhost:5995";
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string GetBasicAuthorizationHeader()
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
                return string.Empty;

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{UserName}:{Password}"));
                
            return credentials;
        }

        public bool HasCredentials()
        {
            return !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password);
        }
    }
}
