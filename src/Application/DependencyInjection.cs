using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SkillsetsBackend.Application.Auth.Commands.Login;
using SkillsetsBackend.Application.Auth.Commands.Logout;
using SkillsetsBackend.Application.Auth.Commands.Refresh;
using SkillsetsBackend.Application.Auth.Commands.ResetPassword;
using SkillsetsBackend.Application.Auth.Commands.CustomerSupportRequest;
using SkillsetsBackend.Application.Auth.Commands.SwitchCompany;
using SkillsetsBackend.Application.Auth.Queries.ListLoginActivityLogs;
using SkillsetsBackend.Application.Students.Commands.AddEmployeeRole;
using SkillsetsBackend.Application.Students.Commands.AssignStudentManager;
using SkillsetsBackend.Application.Students.Commands.ChangeStudentPassword;
using SkillsetsBackend.Application.Students.Commands.CreateStudent;
using SkillsetsBackend.Application.Students.Commands.DeactivateStudent;
using SkillsetsBackend.Application.Students.Commands.ProvisionStudentSkillport;
using SkillsetsBackend.Application.Students.Commands.UpdateStudent;
using SkillsetsBackend.Application.Students.Queries.GetStudentById;
using SkillsetsBackend.Application.Students.Queries.GetStudentCompanies;
using SkillsetsBackend.Application.Students.Queries.GetStudentCredentials;
using SkillsetsBackend.Application.Students.Queries.GetStudentRoles;
using SkillsetsBackend.Application.Students.Queries.ListStudents;
using SkillsetsBackend.Application.Companies.Queries.ListCompanies;
using SkillsetsBackend.Application.Companies.Queries.GetCompanyById;
using SkillsetsBackend.Application.Companies.Queries.GetCompanyLogo;
using SkillsetsBackend.Application.Companies.Commands.CreateCompany;
using SkillsetsBackend.Application.Companies.Commands.UpdateCompany;
using SkillsetsBackend.Application.Companies.Commands.DeactivateCompany;
using SkillsetsBackend.Application.Companies.Commands.ActivateCompany;
using SkillsetsBackend.Application.Companies.Commands.SetCompanyLicense;
using SkillsetsBackend.Application.Companies.Commands.UpdateCompanyLogo;
using SkillsetsBackend.Application.Dashboard.Queries.GetDashboardStats;
using SkillsetsBackend.Application.Dashboard.Queries.GetCourseLibraryUsers;
using SkillsetsBackend.Application.Dashboard.Queries.GetCourseLibrarySessionHistory;
using SkillsetsBackend.Application.Managers.Commands.ActivateManager;
using SkillsetsBackend.Application.Managers.Commands.AddManagerRole;
using SkillsetsBackend.Application.Managers.Commands.ChangeManagerPassword;
using SkillsetsBackend.Application.Managers.Commands.CreateManager;
using SkillsetsBackend.Application.Managers.Commands.DeactivateManager;
using SkillsetsBackend.Application.Managers.Commands.ProvisionManagerSkillport;
using SkillsetsBackend.Application.Managers.Commands.UpdateManager;
using SkillsetsBackend.Application.Managers.Queries.GetManagerById;
using SkillsetsBackend.Application.Managers.Queries.GetManagerCompanies;
using SkillsetsBackend.Application.Managers.Queries.GetManagerCredentials;
using SkillsetsBackend.Application.Managers.Queries.GetManagerRoles;
using SkillsetsBackend.Application.Managers.Queries.ListManagers;
using SkillsetsBackend.Application.Faqs.Queries.ListFaqs;
using SkillsetsBackend.Application.Faqs.Queries.GetFaqById;
using SkillsetsBackend.Application.Faqs.Commands.CreateFaq;
using SkillsetsBackend.Application.Faqs.Commands.UpdateFaq;
using SkillsetsBackend.Application.Faqs.Commands.DeactivateFaq;
using SkillsetsBackend.Application.SupportContacts.Queries.ListSupportContacts;
using SkillsetsBackend.Application.SupportContacts.Queries.GetSupportContactById;
using SkillsetsBackend.Application.SupportContacts.Commands.CreateSupportContact;
using SkillsetsBackend.Application.SupportContacts.Commands.UpdateSupportContact;
using SkillsetsBackend.Application.SupportContacts.Commands.DeactivateSupportContact;
using SkillsetsBackend.Application.CourseLibrary.Queries.GetCourseLibrary;
using SkillsetsBackend.Application.CourseLibrary.Queries.GetCourseLibraryCourseDetail;
using SkillsetsBackend.Application.CourseLibrary.Queries.ListCourseTaken;
using SkillsetsBackend.Application.CourseLibrary.Queries.SearchCourses;
using SkillsetsBackend.Application.CourseLibrary.Commands.TakeCourse;
using SkillsetsBackend.Application.CourseLibrary.Commands.MarkCourseTakenComplete;
using SkillsetsBackend.Application.RoleManagement.Queries.ListPermissions;
using SkillsetsBackend.Application.RoleManagement.Queries.ListRoles;
using SkillsetsBackend.Application.RoleManagement.Queries.GetRoleById;
using SkillsetsBackend.Application.RoleManagement.Queries.GetUserEffectivePermissions;
using SkillsetsBackend.Application.RoleManagement.Queries.GetMyPermissions;
using SkillsetsBackend.Application.RoleManagement.Commands.CreateRole;
using SkillsetsBackend.Application.RoleManagement.Commands.UpdateRolePermissions;
using SkillsetsBackend.Application.RoleManagement.Commands.UpdateRole;
using SkillsetsBackend.Application.RoleManagement.Commands.DeactivateRole;
using SkillsetsBackend.Application.RoleManagement.Commands.ActivateRole;

namespace SkillsetsBackend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<SwitchCompanyCommandHandler>();
        services.AddScoped<CustomerSupportRequestCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();
        services.AddScoped<ListLoginActivityLogsQueryHandler>();

        services.AddScoped<ListStudentsQueryHandler>();
        services.AddScoped<GetStudentByIdQueryHandler>();
        services.AddScoped<GetStudentCompaniesQueryHandler>();
        services.AddScoped<GetStudentRolesQueryHandler>();
        services.AddScoped<GetStudentCredentialsQueryHandler>();
        services.AddScoped<CreateStudentCommandHandler>();
        services.AddScoped<UpdateStudentCommandHandler>();
        services.AddScoped<ChangeStudentPasswordCommandHandler>();
        services.AddScoped<DeactivateStudentCommandHandler>();
        services.AddScoped<ProvisionStudentSkillportCommandHandler>();
        services.AddScoped<AssignStudentManagerCommandHandler>();
        services.AddScoped<AddEmployeeRoleCommandHandler>();

        services.AddScoped<ListCompaniesQueryHandler>();
        services.AddScoped<GetCompanyByIdQueryHandler>();
        services.AddScoped<GetCompanyLogoQueryHandler>();
        services.AddScoped<CreateCompanyCommandHandler>();
        services.AddScoped<UpdateCompanyCommandHandler>();
        services.AddScoped<DeactivateCompanyCommandHandler>();
        services.AddScoped<ActivateCompanyCommandHandler>();
        services.AddScoped<SetCompanyLicenseCommandHandler>();
        services.AddScoped<UpdateCompanyLogoCommandHandler>();
        services.AddScoped<ListManagersQueryHandler>();
        services.AddScoped<GetManagerByIdQueryHandler>();
        services.AddScoped<GetManagerCompaniesQueryHandler>();
        services.AddScoped<GetManagerRolesQueryHandler>();
        services.AddScoped<GetManagerCredentialsQueryHandler>();
        services.AddScoped<CreateManagerCommandHandler>();
        services.AddScoped<UpdateManagerCommandHandler>();
        services.AddScoped<ChangeManagerPasswordCommandHandler>();
        services.AddScoped<ProvisionManagerSkillportCommandHandler>();
        services.AddScoped<DeactivateManagerCommandHandler>();
        services.AddScoped<ActivateManagerCommandHandler>();
        services.AddScoped<AddManagerRoleCommandHandler>();

        services.AddScoped<GetDashboardStatsQueryHandler>();
        services.AddScoped<GetCourseLibraryUsersQueryHandler>();
        services.AddScoped<GetCourseLibrarySessionHistoryQueryHandler>();

        services.AddScoped<ListFaqsQueryHandler>();
        services.AddScoped<GetFaqByIdQueryHandler>();
        services.AddScoped<CreateFaqCommandHandler>();
        services.AddScoped<UpdateFaqCommandHandler>();
        services.AddScoped<DeactivateFaqCommandHandler>();
        services.AddScoped<ListSupportContactsQueryHandler>();
        services.AddScoped<GetSupportContactByIdQueryHandler>();
        services.AddScoped<CreateSupportContactCommandHandler>();
        services.AddScoped<UpdateSupportContactCommandHandler>();
        services.AddScoped<DeactivateSupportContactCommandHandler>();

        services.AddScoped<GetCourseLibraryQueryHandler>();
        services.AddScoped<GetCourseLibraryCourseDetailQueryHandler>();
        services.AddScoped<SearchCoursesQueryHandler>();
        services.AddScoped<TakeCourseCommandHandler>();
        services.AddScoped<MarkCourseTakenCompleteCommandHandler>();
        services.AddScoped<ListCourseTakenQueryHandler>();

        services.AddScoped<ListPermissionsQueryHandler>();
        services.AddScoped<ListRolesQueryHandler>();
        services.AddScoped<GetRoleByIdQueryHandler>();
        services.AddScoped<GetUserEffectivePermissionsQueryHandler>();
        services.AddScoped<GetMyPermissionsQueryHandler>();
        services.AddScoped<CreateRoleCommandHandler>();
        services.AddScoped<UpdateRolePermissionsCommandHandler>();
        services.AddScoped<UpdateRoleCommandHandler>();
        services.AddScoped<DeactivateRoleCommandHandler>();
        services.AddScoped<ActivateRoleCommandHandler>();

        return services;
    }
}
