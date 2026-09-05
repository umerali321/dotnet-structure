using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Managers.Commands.UpdateManager;

public class UpdateManagerCommandHandler
{
    private readonly IValidator<UpdateManagerCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISuperAdminAuthenticator _superAdminAuthenticator;

    public UpdateManagerCommandHandler(
        IValidator<UpdateManagerCommand> validator,
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

    public async Task Handle(int userId, UpdateManagerCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        var user = await _repository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), userId);

        if (!await _permissionService.HasPermissionAsync(caller, Permissions.Managers.Update, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to update managers.");
        }

        if (!caller.IsPlatformAdmin)
        {
            await StudentAuthorization.EnsureCanManageManagerAsync(caller, userId, _userDirectory, cancellationToken);
        }

        // Username is still fixed at creation regardless of who is editing - never editable, by
        // anyone, so nothing that keys off it (Skillport provisioning, reports) ever forks onto a
        // second identity. Email is different: editing it is a deliberately SuperAdmin-only
        // capability (same "sensitive, hardcoded, no SystemAdmin bypass" bucket as Company CRUD,
        // role CRUD, and per-user permission overrides - see project-systemadmin-rbac) - everyone
        // else, including SystemAdmin, keeps silently getting the existing value back.
        var newEmail = caller.IsSuperAdmin ? command.Email : user.Email!;
        var emailChanged = !string.Equals(newEmail, user.Email, StringComparison.Ordinal);
        if (emailChanged &&
            (_superAdminAuthenticator.IsSuperAdminEmail(newEmail) ||
             await _repository.IdentifierInUseAsync(newEmail, user.Username!, excludeUserId: userId, cancellationToken)))
        {
            throw new AppValidationException([new ValidationFailure(nameof(command.Email), "This email is already in use by another account.")]);
        }

        user.UpdateProfile(newEmail, command.Phone, command.FirstName, command.LastName, user.Username!);
        await _repository.SaveChangesAsync(cancellationToken);

        if (emailChanged)
        {
            // So an already-logged-in session can't keep silently refreshing itself forever with the
            // OLD email baked into its claims (RefreshToken rows snapshot Email and re-copy it
            // forward on every refresh rather than re-reading Users) - forces a fresh login, which
            // always re-reads Users.Email correctly.
            await _refreshTokenRepository.RevokeAllActiveForUserAsync(userId.ToString(), null, cancellationToken);
        }
    }
}
