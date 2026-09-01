namespace FmuApiDomain.Connectivity.Models;

public static class CrptEspHosts
{
    public const string GroupCrpt = "ЦРПТ / ФН";
    public const string GroupEsp = "АО ЕСП";
    public const string GroupMgm = "МГМ";

    public static IReadOnlyList<CrptEspHost> Defaults { get; } =
    [
        new("https://ts-reg.crpt.ru", GroupCrpt),

        new("https://cdn01.crpt.ru", GroupCrpt),
        new("https://cdn02.crpt.ru", GroupCrpt),
        new("https://cdn03.crpt.ru", GroupCrpt),
        new("https://cdn04.crpt.ru", GroupCrpt),
        new("https://cdn05.crpt.ru", GroupCrpt),
        new("https://cdn06.crpt.ru", GroupCrpt),
        new("https://cdn07.crpt.ru", GroupCrpt),
        new("https://cdn08.crpt.ru", GroupCrpt),
        new("https://cdn09.crpt.ru", GroupCrpt),
        new("https://cdn10.crpt.ru", GroupCrpt),
        new("https://cdn11.crpt.ru", GroupCrpt),

        new("https://cdn01-ts.crpt.ru", GroupCrpt),
        new("https://cdn02-ts.crpt.ru", GroupCrpt),
        new("https://cdn03-ts.crpt.ru", GroupCrpt),
        new("https://cdn04-ts.crpt.ru", GroupCrpt),
        new("https://cdn05-ts.crpt.ru", GroupCrpt),
        new("https://cdn06-ts.crpt.ru", GroupCrpt),
        new("https://cdn07-ts.crpt.ru", GroupCrpt),
        new("https://cdn08-ts.crpt.ru", GroupCrpt),
        new("https://cdn09-ts.crpt.ru", GroupCrpt),
        new("https://cdn10-ts.crpt.ru", GroupCrpt),
        new("https://cdn11-ts.crpt.ru", GroupCrpt),

        new("https://stats.ao-esp.ru", GroupEsp),
        new("https://api.ao-esp.ru", GroupEsp),

        new("https://cdn01.am.crptech.ru", GroupMgm),
        new("https://cdn02.am.crptech.ru", GroupMgm),
        new("https://cdn03.am.crptech.ru", GroupMgm)
    ];
}
