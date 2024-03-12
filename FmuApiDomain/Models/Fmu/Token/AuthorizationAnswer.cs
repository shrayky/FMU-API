namespace FmuApiDomain.Models.Fmu.Token
{
    public class AuthorizationAnswer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "pos";
        public int Expired { get; set; } = 0;
        public string Signature { get; set; } = string.Empty;
    }
}
