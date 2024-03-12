using FmuApiDomain.Models.Configuration;

namespace FmuApiApplication
{
    public static class Constants
    {
        public static Constant Parametrs { get; set; } = new Constant();
        public static bool Online { get; set; } = true;


        public static void Init()
        {
            Parametrs.Init();
        }

    }
}
