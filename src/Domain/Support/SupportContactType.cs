namespace SkillsetsBackend.Domain.Support;

public static class SupportContactType
{
    public const string Phone = "Phone";
    public const string Email = "Email";
    public const string Address = "Address";
    public const string Website = "Website";
    public const string Facebook = "Facebook";
    public const string Other = "Other";

    public static readonly IReadOnlyCollection<string> All = [Phone, Email, Address, Website, Facebook, Other];

    public static bool IsValid(string contactType) => All.Contains(contactType);
}
