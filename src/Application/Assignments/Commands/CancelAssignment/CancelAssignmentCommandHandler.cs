using System.Net;
using Microsoft.Extensions.Logging;
using SkillsetsBackend.Application.Assignments.Interfaces;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Assignments.Commands.CancelAssignment;

/// <summary>Manager/CompanyAdmin only. Cancellation notification emails are best-effort, same as
/// the assignment-created email - a delivery failure never fails the cancellation itself.</summary>
public class CancelAssignmentCommandHandler
{
    private readonly IAssignmentRepository _repository;
    private readonly IAssignmentQueryService _queryService;
    private readonly IUserDirectory _userDirectory;
    private readonly Application.Students.Interfaces.IStudentRepository _studentRepository;
    private readonly IPermissionService _permissionService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<CancelAssignmentCommandHandler> _logger;

    public CancelAssignmentCommandHandler(
        IAssignmentRepository repository,
        IAssignmentQueryService queryService,
        IUserDirectory userDirectory,
        Application.Students.Interfaces.IStudentRepository studentRepository,
        IPermissionService permissionService,
        IEmailSender emailSender,
        ILogger<CancelAssignmentCommandHandler> logger)
    {
        _repository = repository;
        _queryService = queryService;
        _userDirectory = userDirectory;
        _studentRepository = studentRepository;
        _permissionService = permissionService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Handle(int assignmentId, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Assignments.Cancel, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to cancel training assignments.");
        }

        var assignment = await _repository.GetByIdAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundException("Assignment", assignmentId);

        await StudentAuthorization.EnsureCanManageCompanyAsync(caller, assignment.CompanyId, _userDirectory, cancellationToken);

        if (assignment.IsCancelled)
        {
            return;
        }

        var dto = await _queryService.GetDtoAsync(assignmentId, cancellationToken);

        assignment.Cancel();
        await _repository.SaveChangesAsync(cancellationToken);

        if (dto is null)
        {
            return;
        }

        foreach (var employee in dto.Employees)
        {
            try
            {
                var user = await _studentRepository.GetUserAsync(employee.StudentUserId, cancellationToken);
                if (user?.Email is null)
                {
                    continue;
                }

                await _emailSender.SendAsync(
                    user.Email,
                    user.FirstName,
                    "A training assignment was cancelled",
                    BuildCancellationEmailBody(user.FirstName ?? "there"),
                    purpose: "AssignmentCancelled",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send cancellation notification email to employee {EmployeeId}", employee.StudentUserId);
            }
        }
    }

    private static string BuildCancellationEmailBody(string firstName) => $$"""
        <div style="background-color:#f4f4f5;padding:32px 16px;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e7e5e3;">
            <tr>
              <td style="background:#c81322;padding:22px 28px;">
                <div style="color:#ffffff;font-size:18px;font-weight:700;letter-spacing:0.5px;">SKILLSETS</div>
                <div style="color:#ffd9dc;font-size:12px;margin-top:4px;">Training assignment cancelled</div>
              </td>
            </tr>
            <tr>
              <td style="padding:28px;">
                <p style="margin:0 0 16px;color:#1a1918;font-size:14px;line-height:1.6;">Hi {{WebUtility.HtmlEncode(firstName)}},</p>
                <p style="margin:0;color:#1a1918;font-size:14px;line-height:1.6;">
                  A training assignment on your dashboard has been cancelled by your manager or company admin. No
                  further action is needed from you for this assignment.
                </p>
              </td>
            </tr>
          </table>
        </div>
        """;
}
