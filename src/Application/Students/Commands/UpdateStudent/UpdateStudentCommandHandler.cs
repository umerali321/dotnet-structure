using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Students.Commands.UpdateStudent;

public class UpdateStudentCommandHandler
{
    private readonly IValidator<UpdateStudentCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISuperAdminAuthenticator _superAdminAuthenticator;

    public UpdateStudentCommandHandler(
        IValidator<UpdateStudentCommand> validator,
        IStudentRepository repository,
        IUserDirectory userDirectory,
        IPermissionService permissionService,
        IRefreshTokenRepository refreshTokenRepository,
        ISuperAdminAuthenticator superAdminAuthenticator)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
        _refreshTokenRepository = refreshTokenRepository;
        _superAdminAuthenticator = superAdminAuthenticator;
    }

    public async Task Handle(int userId, UpdateStudentCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        if (caller.Role == Roles.Student)
        {
            if (caller.DbUserId != userId)
            {
                throw new UnauthorizedAccessException("You can only update your own profile.");
            }

            var currentProfile = await _repository.GetProfileByUserIdAsync(userId, cancellationToken);
            var restrictedFieldsChanged =
                command.Email != user.Email ||
                command.Username != user.Username ||
                command.StudentType != currentProfile?.StudentType;

            if (restrictedFieldsChanged)
            {
                throw new UnauthorizedAccessException("You are only allowed to update your first name, last name, and phone.");
            }

            user.UpdatePersonalInfo(command.FirstName, command.LastName, command.Phone);
        }
        else
        {
            if (!await _permissionService.HasPermissionAsync(caller, Permissions.Students.Update, cancellationToken))
            {
                throw new UnauthorizedAccessException("You do not have permission to update employees.");
            }

            await StudentAuthorization.EnsureCanManageStudentAsync(caller, userId, _userDirectory, _repository, cancellationToken);

            // Username is still fixed at creation regardless of who is editing - never editable, by
            // anyone, so nothing that keys off it (Skillport provisioning, reports) ever forks onto a
            // second identity. Email is different: editing it is a deliberately SuperAdmin-only
            // capability (same "sensitive, hardcoded, no SystemAdmin bypass" bucket as Company CRUD,
            // role CRUD, and per-user permission overrides - see project-systemadmin-rbac) - everyone
            // else, including a Manager/CompanyAdmin with Update permission, keeps silently getting
            // the existing value back.
            var newEmail = caller.IsSuperAdmin ? command.Email : user.Email!;
            var emailChanged = !string.Equals(newEmail, user.Email, StringComparison.Ordinal);
            if (emailChanged &&
                (_superAdminAuthenticator.IsSuperAdminEmail(newEmail) ||
                 await _repository.IdentifierInUseAsync(newEmail, user.Username!, excludeUserId: userId, cancellationToken)))
            {
                throw new AppValidationException([new ValidationFailure(nameof(command.Email), "This email is already in use by another account.")]);
            }

            user.UpdateProfile(newEmail, command.Phone, command.FirstName, command.LastName, user.Username!);

            var profile = await _repository.GetProfileByUserIdAsync(userId, cancellationToken);
            profile?.Update(command.StudentType, caller.Email);

            await _repository.SaveChangesAsync(cancellationToken);

            if (emailChanged)
            {
                // So an already-logged-in session can't keep silently refreshing itself forever with
                // the OLD email baked into its claims (RefreshToken rows snapshot Email and re-copy
                // it forward on every refresh rather than re-reading Users) - forces a fresh login,
                // which always re-reads Users.Email correctly.
                await _refreshTokenRepository.RevokeAllActiveForUserAsync(userId.ToString(), null, cancellationToken);
            }

            return;
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
