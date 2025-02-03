namespace ApplicationConfigurationService.Settings
{
    public class DefaultHostsToPing
    {
        public static List<string> Hosts()
        {
            List<string> hosts = [];

            hosts.Add("mail.ru");
            hosts.Add("ya.ru");
            hosts.Add("au124.ru");
            hosts.Add("atol.ru");
            hosts.Add("google.com");

            return hosts;
        }
    }
}
