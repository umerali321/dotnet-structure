namespace SkillsetsBackend.Application.Companies.Commands.SetCompanyLicense;

/// <summary>Switches a company onto a License plan with an explicit start/end date - used both to
/// convert a Trial company to a paid License and to renew/extend an existing License.</summary>
public record SetCompanyLicenseCommand(DateOnly StartDate, DateOnly EndDate);
