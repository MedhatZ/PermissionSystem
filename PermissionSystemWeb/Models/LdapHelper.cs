using System.DirectoryServices.Protocols;
using System.Net;

public record LdapCfg(
    string DomainName,
    string Host,
    string BaseDn,
    string BindUser,
    string BindPassword,
    bool UseSsl
);

public record AdUser
{
    public string? Dn { get; set; }
    public string? DisplayName { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string? SamAccountName { get; set; }
    public string? Upn { get; set; }
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Manager { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? Mobile { get; set; }
    public string? Office { get; set; }
    public string? MemberOf { get; set; }
}

public static class LdapHelper
{
    public static (bool ok, AdUser? user, string? err) FindUser(LdapCfg cfg, string attribute, string value)
    {
        try
        {
            var host = string.IsNullOrWhiteSpace(cfg.Host) ? cfg.DomainName : cfg.Host;
            var id = new LdapDirectoryIdentifier(host, cfg.UseSsl ? 636 : 389);
            using var conn = new LdapConnection(id)
            {
                AuthType = AuthType.Negotiate
            };
            conn.SessionOptions.ProtocolVersion = 3;
            conn.SessionOptions.SecureSocketLayer = cfg.UseSsl;

            var bindUpn = cfg.BindUser.Contains("@")
                ? cfg.BindUser
                : $"{cfg.BindUser}@{cfg.DomainName}";

            conn.Credential = new NetworkCredential(bindUpn, cfg.BindPassword);
            conn.Bind();

            var baseDn = string.IsNullOrWhiteSpace(cfg.BaseDn)
                ? DomainToBaseDn(cfg.DomainName)
                : cfg.BaseDn;

            var filter = $"({attribute}={Escape(value)})";

            var req = new SearchRequest(
                baseDn,
                filter,
                SearchScope.Subtree,
                new[] {
                    "distinguishedName","cn","displayName","givenName","sn","mail",
                    "sAMAccountName","userPrincipalName","employeeID","department",
                    "title","manager","telephoneNumber","mobile","physicalDeliveryOfficeName","memberOf"
                }
            );

            var resp = (SearchResponse)conn.SendRequest(req);
            var entry = resp.Entries.Cast<SearchResultEntry>().FirstOrDefault();
            if (entry == null) return (false, null, "User not found");

            string? Get(string a) =>
                entry.Attributes[a]?.Count > 0 ? entry.Attributes[a][0]?.ToString() : null;

            var user = new AdUser
            {
                Dn = Get("distinguishedName"),
                DisplayName = Get("displayName") ?? Get("cn"),
                GivenName = Get("givenName"),
                Surname = Get("sn"),
                Email = Get("mail"),
                SamAccountName = Get("sAMAccountName"),
                Upn = Get("userPrincipalName"),
                EmployeeId = Get("employeeID"),
                Department = Get("department"),
                Title = Get("title"),
                Manager = Get("manager"),
                TelephoneNumber = Get("telephoneNumber"),
                Mobile = Get("mobile"),
                Office = Get("physicalDeliveryOfficeName"),
                MemberOf = Get("memberOf")
            };

            return (true, user, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private static string Escape(string input) =>
        input.Replace("\\", "\\5c").Replace("(", "\\28").Replace(")", "\\29")
             .Replace("*", "\\2a").Replace("\0", "\\00");

    private static string DomainToBaseDn(string domain) =>
        string.Join(",", domain.Split('.').Select(p => $"DC={p}"));
}
