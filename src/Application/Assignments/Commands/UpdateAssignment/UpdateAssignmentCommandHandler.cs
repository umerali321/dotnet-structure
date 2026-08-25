using System.Net;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Application.Assignments.Interfaces;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using ConflictException = SkillsetsBackend.Application.Common.Exceptions.ConflictException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Assignments.Commands.UpdateAssignment;

/// <summary>Manager/CompanyAdmin only. Reuses Permissions.Assignments.Create (editing an
/// assignment you're already allowed to create is the same authority, not a separate grant).
/// Titles can only change when command.CourseIds is provided AND no employee has any progress
/// (In Progress or Completed) on the current titles - otherwise the training itself must be
/// reassigned via cancel + create. The overlap/duplicate safeguard applies only to newly-added
/// employees; existing ones already legitimately hold this assignment.</summary>
public class UpdateAssignmentCommandHandler
{
    private readonly IValidator<UpdateAssignmentCommand> _validator;
    private readonly IAssignmentRepository _repository;
    private readonly IAssignmentQueryService _queryService;
    private readonly ICourseLibraryQueryService _courseLibraryQueryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IStudentRepository _studentRepository;
    private readonly IPermissionService _permissionService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UpdateAssignmentCommandHandler> _logger;

    public UpdateAssignmentCommandHandler(
        IValidator<UpdateAssignmentCommand> validator,
        IAssignmentRepository repository,
        IAssignmentQueryService queryService,
        ICourseLibraryQueryService courseLibraryQueryService,
        IUserDirectory userDirectory,
        IStudentRepository studentRepository,
        IPermissionService permissionService,
        IEmailSender emailSender,
        ILogger<UpdateAssignmentCommandHandler> logger)
    {
        _validator = validator;
        _repository = repository;
        _queryService = queryService;
        _courseLibraryQueryService = courseLibraryQueryService;
        _userDirectory = userDirectory;
        _studentRepository = studentRepository;
        _permissionService = permissionService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<CreateAssignmentResultDto> Handle(int assignmentId, UpdateAssignmentCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Assignments.Create, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to edit training assignments.");
        }

        var assignment = await _repository.GetByIdAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundException("Assignment", assignmentId);

        await StudentAuthorization.EnsureCanManageCompanyAsync(caller, assignment.CompanyId, _userDirectory, cancellationToken);

        if (assignment.IsCancelled)
        {
            throw new ConflictException("This assignment has been cancelled and cannot be edited.");
        }

        var distinctEmployeeIds = command.EmployeeUserIds.Distinct().ToList();
        foreach (var employeeId in distinctEmployeeIds)
        {
            await StudentAuthorization.EnsureCanManageStudentAsync(caller, employeeId, _userDirectory, _studentRepository, cancellationToken);
        }

        var currentDto = await _queryService.GetDtoAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundException("Assignment", assignmentId);
        var currentEmployeeIds = currentDto.Employees.Select(e => e.StudentUserId).ToList();
        var currentCourseIds = currentDto.Titles.Select(t => t.CourseId).ToList();

        var titlesChanging = command.CourseIds is not null;
        var effectiveCourseIds = currentCourseIds;

        if (titlesChanging)
        {
            var anyProgress = currentDto.Employees.Any(e => e.TitleProgress.Any(t => t.Status != "NotStarted"));
            if (anyProgress)
            {
                throw new ConflictException(
                    "This assignment's titles cannot be changed because at least one employee has already started or completed a title. Cancel this assignment and create a new one instead.");
            }

            effectiveCourseIds = command.CourseIds!.Distinct().ToList();
            var courses = await _courseLibraryQueryService.GetCoursesByIdsAsync(effectiveCourseIds, cancellationToken);
            var missing = effectiveCourseIds.Where(id => !courses.Any(c => c.CourseId == id)).ToList();
            if (missing.Count > 0)
            {
                throw new AppValidationException(
                    [new FluentValidation.Results.ValidationFailure(nameof(command.CourseIds), $"Course(s) not found or inactive: {string.Join(", ", missing)}")]);
            }
        }

        var newlyAdded = distinctEmployeeIds.Except(currentEmployeeIds).ToList();
        if (newlyAdded.Count > 0)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var overlaps = await _repository.FindActiveOverlapsAsync(newlyAdded, effectiveCourseIds, today, cancellationToken);
            if (overlaps.Count > 0 && !command.ConfirmDespiteWarnings)
            {
                var warnings = overlaps
                    .Select(o => $"{o.StudentName} already has \"{o.CourseTitle}\" in an active or scheduled assignment.")
                    .ToList();
                return new CreateAssignmentResultDto(null, warnings);
            }
        }

        var removed = currentEmployeeIds.Except(distinctEmployeeIds).ToList();

        await _repository.UpdateEmployeesAsync(assignmentId, distinctEmployeeIds, cancellationToken);
        if (titlesChanging)
        {
            await _repository.UpdateTitlesAsync(assignmentId, effectiveCourseIds, cancellationToken);
        }
        assignment.UpdateStartDate(command.StartDate);
        assignment.MarkUpdated(caller.DbUserId);
        await _repository.SaveChangesAsync(cancellationToken);

        await SendAddedEmailsAsync(newlyAdded, assignment, cancellationToken);
        await SendRemovedEmailsAsync(removed, cancellationToken);

        var dto = await _queryService.GetDtoAsync(assignmentId, cancellationToken);
        return new CreateAssignmentResultDto(dto, []);
    }

    private async Task SendAddedEmailsAsync(IReadOnlyList<int> employeeIds, Domain.Assignments.Assignment assignment, CancellationToken cancellationToken)
    {
        foreach (var employeeId in employeeIds)
        {
            try
            {
                var user = await _studentRepository.GetUserAsync(employeeId, cancellationToken);
                if (user?.Email is null)
                {
                    continue;
                }

                await _emailSender.SendAsync(
                    user.Email,
                    user.FirstName,
                    "New training assigned to you",
                    BuildAddedEmailBody(user.FirstName ?? "there", assignment),
                    purpose: "AssignmentUpdated",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send assignment-added notification email to employee {EmployeeId}", employeeId);
            }
        }
    }

    private async Task SendRemovedEmailsAsync(IReadOnlyList<int> employeeIds, CancellationToken cancellationToken)
    {
        foreach (var employeeId in employeeIds)
        {
            try
            {
                var user = await _studentRepository.GetUserAsync(employeeId, cancellationToken);
                if (user?.Email is null)
                {
                    continue;
                }

                await _emailSender.SendAsync(
                    user.Email,
                    user.FirstName,
                    "A training assignment was updated",
                    BuildRemovedEmailBody(user.FirstName ?? "there"),
                    purpose: "AssignmentUpdated",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send assignment-removed notification email to employee {EmployeeId}", employeeId);
            }
        }
    }

    private static string BuildAddedEmailBody(string firstName, Domain.Assignments.Assignment assignment) => $$"""
        <div style="background-color:#f4f4f5;padding:32px 16px;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e7e5e3;">
            <tr>
              <td style="background:#c81322;padding:22px 28px;">
                <div style="color:#ffffff;font-size:18px;font-weight:700;letter-spacing:0.5px;">SKILLSETS</div>
                <div style="color:#ffd9dc;font-size:12px;margin-top:4px;">New training assigned</div>
              </td>
            </tr>
            <tr>
              <td style="padding:28px;">
                <p style="margin:0 0 16px;color:#1a1918;font-size:14px;line-height:1.6;">Hi {{WebUtility.HtmlEncode(firstName)}},</p>
                <p style="margin:0 0 20px;color:#1a1918;font-size:14px;line-height:1.6;">
                  You've been added to an existing training assignment. Your 30-day Focus Session window is below -
                  sign in to your dashboard to see the full list of titles and get started.
                </p>
                <div style="background:#f7f6f5;border:1px solid #e7e5e3;border-radius:10px;padding:16px 20px;text-align:center;margin-bottom:20px;">
                  <span style="font-size:14px;color:#1a1918;">{{assignment.StartDate:MMM d, yyyy}} &ndash; {{assignment.EndDate:MMM d, yyyy}}</span>
                </div>
                <p style="margin:0;color:#6b6663;font-size:12px;line-height:1.6;">
                  Sign in to SkillSets and check "My Training" on your dashboard for the full details.
                </p>
              </td>
            </tr>
          </table>
        </div>
        """;

    private static string BuildRemovedEmailBody(string firstName) => $$"""
        <div style="background-color:#f4f4f5;padding:32px 16px;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e7e5e3;">
            <tr>
              <td style="background:#c81322;padding:22px 28px;">
                <div style="color:#ffffff;font-size:18px;font-weight:700;letter-spacing:0.5px;">SKILLSETS</div>
                <div style="color:#ffd9dc;font-size:12px;margin-top:4px;">Training assignment updated</div>
              </td>
            </tr>
            <tr>
              <td style="padding:28px;">
                <p style="margin:0 0 16px;color:#1a1918;font-size:14px;line-height:1.6;">Hi {{WebUtility.HtmlEncode(firstName)}},</p>
                <p style="margin:0;color:#1a1918;font-size:14px;line-height:1.6;">
                  You've been removed from a training assignment on your dashboard by your manager or company admin.
                  No further action is needed from you for this assignment.
                </p>
              </td>
            </tr>
          </table>
        </div>
        """;
}
