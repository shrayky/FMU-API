using FmuApiDomain.Attributes;

namespace FmuApiDomain.Configuration.Options.Organization;

public class OrganizationConfiguration
{
    [ExchangeableSetting]
    public List<PrintGroupData> PrintGroups { get; set; } = [];


    public void FillIfEmpty()
    {
        if (PrintGroups.Count > 0)
            return;

        PrintGroups.Add(new PrintGroupData { Id = 1 });
    }

    public string XapiKey()
    {
        if (PrintGroups.Count == 0)
            return "";

        var group = PrintGroups[0];

        if (group.XApiKeyExpired)
            return "";

        return group.XAPIKEY;
    }

    public string XapiKey(int id)
    {
        if (PrintGroups.Count == 0)
            return "";

        if (id == 0)
            return XapiKey();

        PrintGroupData? row = PrintGroups.FirstOrDefault(x => x.Id == id);

        if (row == null)
            return PrintGroups[0].XAPIKEY;

        return row.XAPIKEY;
    }

    public PrintGroupData? PrintGroup(int id)
    {
        if (PrintGroups.Count == 0)
            return null;

        if (id == 0)
            return PrintGroups[0];

        return PrintGroups.FirstOrDefault(group => group.Id == id) ?? PrintGroups[0];
    }

    public PrintGroupData? PrintGroupByInn(string? inn)
    {
        var trimmed = (inn ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return null;

        return PrintGroups.FirstOrDefault(group =>
            string.Equals((group.INN ?? string.Empty).Trim(), trimmed, StringComparison.Ordinal));
    }

    public void SetXapiKey(string xapikey)
    {
        SetXapiKey(xapikey, 1);
    }

    public void SetXapiKey(string xapikey, int id)
    {
        PrintGroupData? row = PrintGroups.FirstOrDefault(x => x.Id == id);

        if (row == null)
        {
            row = new PrintGroupData { Id = id, XAPIKEY = xapikey };
            PrintGroups.Add(row);
        }
        else
        {
            row.XAPIKEY = xapikey;
        }
    }

    public void SetXapiKey(string xapikey, int id, string inn)
    {
        PrintGroupData? row = PrintGroups.FirstOrDefault(x => x.Id == id);

        if (row == null)
        {
            row = new PrintGroupData { Id = id, XAPIKEY = xapikey, INN = inn };
            PrintGroups.Add(row);
        }
        else
        {
            row.XAPIKEY = xapikey;
            row.INN = inn;
        }
    }
    public void DeleteXapiKay(int id)
    {
        PrintGroupData? row = PrintGroups.FirstOrDefault(x => x.Id == id);

        if (row == null)
            return;

        PrintGroups.Remove(row);
    }

    public void DeleteXapiKay(string xapikey)
    {
        PrintGroupData? row = PrintGroups.FirstOrDefault(x => x.XAPIKEY == xapikey);

        if (row == null)
            return;

        PrintGroups.Remove(row);
    }

    public PrintGroupData? PrintGroupByXapiKey(string? xapikey)
    {
        if (string.IsNullOrWhiteSpace(xapikey))
            return null;

        return PrintGroups.FirstOrDefault(x => x.XAPIKEY == xapikey);
    }

    public LocalModuleConnection LocalModuleConnection(int organizationId)
    {
        organizationId = organizationId == 0 ? 1 : organizationId;

        var pgData = PrintGroups.FirstOrDefault(x => x.Id == organizationId);

        if (pgData == null)
            return new LocalModuleConnection();

        return pgData.LocalModuleConnection;
    }
}
