using SkillsetsBackend.Application.Auth;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.RosterImport.Commands.PreviewRosterImport;
using SkillsetsBackend.Application.RosterImport.DTOs;
using SkillsetsBackend.Application.RosterImport.Interfaces;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.RosterImport.Commands.SendRosterWelcomeEmails;

/// <param name="Send">The admin's answer to "Send Welcome Emails?". False records the decision and
/// sends nothing - so the prompt stops being offered for this batch either way.</param>
public record SendRosterWelcomeEmailsCommand(int BatchId, bool Send);

/// <summary>
/// Step 6: the deferred welcome emails. Kept out of the import itself on purpose - the admin sees
/// the results first and then decides, so a bad file can be reviewed before anyone is emailed.
///
/// Reuses AccountWelcomeEmail, which is the same path single-account creation uses, so the SMTP
/// settings, notification toggles, template and Email History all apply unchanged. No second email
/// system.
/// </summary>
public class SendRosterWelcomeEmailsCommandHandler
{
    private readonly IRosterImportRepository _repository;
    private readonly AccountWelcomeEmail _welcomeEmail;
    private readonly IPermissionService _permissionService;
    private readonly IUserDirectory _userDirectory;

    public SendRosterWelcomeEmailsCommandHandler(
        IRosterImportRepository repository,
        AccountWelcomeEmail welcomeEmail,
        IPermissionService permissionService,
        IUserDirectory userDirectory)
    {
        _repository = repository;
        _welcomeEmail = welcomeEmail;
        _permissionService = permissionService;
        _userDirectory = userDirectory;
    }

    public async Task<SendRosterWelcomeEmailsResultDto> Handle(
        SendRosterWelcomeEmailsCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        await RosterImportAuthorization.AuthorizeAsync(caller, _permissionService, _userDirectory, cancellationToken);

        var batch = await _repository.GetBatchAsync(command.BatchId, cancellationToken)
            ?? throw new NotFoundException("Roster import batch", command.BatchId);

        if (batch.WelcomeEmailsSentAt is not null)
        {
            return new SendRosterWelcomeEmailsResultDto(command.BatchId, 0, 0,
                $"Welcome emails for this import were already dealt with on "
                + $"{batch.WelcomeEmailsSentAt:yyyy-MM-dd HH:mm} UTC ({batch.WelcomeEmailsSentCount} sent).");
        }

        if (!command.Send)
        {
            // Recorded, not ignored: the batch is now closed to emailing, so a later accidental
            // "yes" cannot surprise people with credentials weeks after the fact.
            await _repository.MarkWelcomeEmailsSentAsync(command.BatchId, 0, cancellationToken);
            return new SendRosterWelcomeEmailsResultDto(command.BatchId, 0, 0, "No welcome emails were sent.");
        }

        var users = await _repository.GetCreatedUsersForBatchAsync(command.BatchId, cancellationToken);

        var sent = 0;
        var failed = 0;
        foreach (var user in users)
        {
            try
            {
                // AccountWelcomeEmail swallows delivery errors by design (see its comment) and logs
                // every attempt to Email History, which is where a failure is diagnosed.
                await _welcomeEmail.SendAsync(user.Email, user.FirstName, user.Password, cancellationToken);
                sent++;
            }
            catch
            {
                failed++;
            }
        }

        await _repository.MarkWelcomeEmailsSentAsync(command.BatchId, sent, cancellationToken);

        return new SendRosterWelcomeEmailsResultDto(command.BatchId, sent, failed,
            failed == 0
                ? $"Welcome emails sent to {sent} new account(s)."
                : $"Welcome emails sent to {sent} account(s); {failed} could not be sent - see Email History.");
    }
}
