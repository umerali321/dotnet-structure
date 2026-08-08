using SkillsetsBackend.Application.Managers.Commands.ChangeManagerPassword;
using SkillsetsBackend.Application.Managers.Commands.CreateManager;
using SkillsetsBackend.Application.Managers.Commands.UpdateManager;

namespace SkillsetsBackend.UnitTests.Managers;

public class ManagerCommandValidatorsTests
{
    [Fact]
    public void CreateManagerCommandValidator_RequiresRequiredFields()
    {
        var validator = new CreateManagerCommandValidator();
        var result = validator.Validate(new CreateManagerCommand("", "", "not-an-email", null, "", "", 0, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateManagerCommand.FirstName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateManagerCommand.Email));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateManagerCommand.Username));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateManagerCommand.Password));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateManagerCommand.CompanyId));
    }

    [Fact]
    public void UpdateManagerCommandValidator_RequiresRequiredFields()
    {
        var validator = new UpdateManagerCommandValidator();
        var result = validator.Validate(new UpdateManagerCommand("", "", "not-an-email", null, ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateManagerCommand.FirstName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateManagerCommand.Email));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateManagerCommand.Username));
    }

    [Fact]
    public void ChangeManagerPasswordCommandValidator_RequiresNewPassword()
    {
        var validator = new ChangeManagerPasswordCommandValidator();
        var result = validator.Validate(new ChangeManagerPasswordCommand("", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeManagerPasswordCommand.NewPassword));
    }
}
