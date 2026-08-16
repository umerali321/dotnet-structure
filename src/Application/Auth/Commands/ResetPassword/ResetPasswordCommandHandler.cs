using System.Net;
using System.Security.Cryptography;
using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler
{
    private readonly IValidator<ResetPasswordCommand> _validator;
    private readonly IUserDirectory _userDirectory;
    private readonly IUserCredentialRepository _credentialRepository;
    private readonly ILoginActivityLogRepository _activityLogRepository;
    private readonly IEmailSender _emailSender;

    public ResetPasswordCommandHandler(
        IValidator<ResetPasswordCommand> validator,
        IUserDirectory userDirectory,
        IUserCredentialRepository credentialRepository,
        ILoginActivityLogRepository activityLogRepository,
        IEmailSender emailSender)
    {
        _validator = validator;
        _userDirectory = userDirectory;
        _credentialRepository = credentialRepository;
        _activityLogRepository = activityLogRepository;
        _emailSender = emailSender;
    }

    public async Task<ResetPasswordResultDto> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var email = command.Email.Trim();
        var resolved = await FindResettableAccountAsync(email, cancellationToken);

        if (resolved is not { } account)
        {
            await _activityLogRepository.AddAsync(LoginActivityLog.PasswordResetFailed(email), cancellationToken);
            await _activityLogRepository.SaveChangesAsync(cancellationToken);
            return new ResetPasswordResultDto(false);
        }

        var newPassword = GenerateRandomPassword();

        var user = await _credentialRepository.GetUserAsync(account.UserId, cancellationToken);
        if (user is null)
        {
            // Shouldn't happen (the account was just resolved above), but fail closed rather than
            // emailing a password that was never actually set.
            await _activityLogRepository.AddAsync(LoginActivityLog.PasswordResetFailed(email), cancellationToken);
            await _activityLogRepository.SaveChangesAsync(cancellationToken);
            return new ResetPasswordResultDto(false);
        }

        user.SetPassword(newPassword);
        await _credentialRepository.SaveChangesAsync(cancellationToken);

        await _emailSender.SendAsync(
            email,
            account.FirstName,
            "Your SkillSets password has been reset",
            BuildResetEmailBody(account.FirstName, newPassword),
            cancellationToken: cancellationToken);

        await _activityLogRepository.AddAsync(LoginActivityLog.PasswordResetSucceeded(email, account.UserId), cancellationToken);
        await _activityLogRepository.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResultDto(true);
    }

    private async Task<(int UserId, string FirstName)?> FindResettableAccountAsync(string email, CancellationToken cancellationToken)
    {
        var directoryUser = await _userDirectory.FindByIdentifierAsync(email, cancellationToken);
        if (directoryUser is null || directoryUser.Email is null
            || !string.Equals(directoryUser.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            // Reject username-only matches - this flow is specifically an email lookup.
            return null;
        }

        var activeRoles = await _userDirectory.GetActiveCompanyRolesAsync(directoryUser.UserId, cancellationToken);
        var isStudentOrManager = activeRoles.Any(r =>
            Roles.Normalize(r.RoleName) is Roles.Student or Roles.Manager);

        return isStudentOrManager ? (directoryUser.UserId, directoryUser.FirstName ?? "there") : null;
    }

    private static string GenerateRandomPassword()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 100_000_000;
        return value.ToString("D8");
    }

    private static string BuildResetEmailBody(string firstName, string newPassword) => $$"""
        <div style="background-color:#f4f4f5;padding:32px 16px;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e7e5e3;">
            <tr>
              <td style="background:#c81322;padding:22px 28px;">
                <div style="color:#ffffff;font-size:18px;font-weight:700;letter-spacing:0.5px;">SKILLSETS</div>
                <div style="color:#ffd9dc;font-size:12px;margin-top:4px;">Password reset</div>
              </td>
            </tr>
            <tr>
              <td style="padding:28px;">
                <p style="margin:0 0 16px;color:#1a1918;font-size:14px;line-height:1.6;">Hi {{WebUtility.HtmlEncode(firstName)}},</p>
                <p style="margin:0 0 20px;color:#1a1918;font-size:14px;line-height:1.6;">
                  We received a request to reset your SkillSets password. Your password has been changed to the temporary one below - use it to sign in, then update it from your profile if you'd like.
                </p>
                <div style="background:#f7f6f5;border:1px solid #e7e5e3;border-radius:10px;padding:16px 20px;text-align:center;margin-bottom:20px;">
                  <span style="font-size:24px;font-weight:700;letter-spacing:4px;color:#c81322;">{{newPassword}}</span>
                </div>
                <p style="margin:0;color:#6b6663;font-size:12px;line-height:1.6;">
                  If you didn't request this, please contact our support team right away.
                </p>
              </td>
            </tr>
          </table>
        </div>
        """;
}
