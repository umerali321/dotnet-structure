using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SkillsetsBackend.Application.Auth.Commands.Login;
using SkillsetsBackend.Application.Auth.Commands.Logout;
using SkillsetsBackend.Application.Auth.Commands.Refresh;
using SkillsetsBackend.Application.Auth.Commands.SwitchCompany;
using SkillsetsBackend.Application.Students.Commands.ChangeStudentPassword;
using SkillsetsBackend.Application.Students.Commands.CreateStudent;
using SkillsetsBackend.Application.Students.Commands.DeactivateStudent;
using SkillsetsBackend.Application.Students.Commands.UpdateStudent;
using SkillsetsBackend.Application.Students.Queries.GetStudentById;
using SkillsetsBackend.Application.Students.Queries.GetStudentCompanies;
using SkillsetsBackend.Application.Students.Queries.GetStudentRoles;
using SkillsetsBackend.Application.Students.Queries.ListStudents;
using SkillsetsBackend.Application.Companies.Queries.ListCompanies;
using SkillsetsBackend.Application.Managers.Commands.ChangeManagerPassword;
using SkillsetsBackend.Application.Managers.Commands.CreateManager;
using SkillsetsBackend.Application.Managers.Commands.UpdateManager;
using SkillsetsBackend.Application.Managers.Queries.GetManagerById;
using SkillsetsBackend.Application.Managers.Queries.GetManagerCompanies;
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
using SkillsetsBackend.Application.Support.Queries.ListSupportRequests;
using SkillsetsBackend.Application.Support.Queries.GetSupportRequestById;
using SkillsetsBackend.Application.Support.Commands.CreateSupportRequest;
using SkillsetsBackend.Application.Support.Commands.UpdateSupportRequestStatus;

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

        services.AddScoped<ListStudentsQueryHandler>();
        services.AddScoped<GetStudentByIdQueryHandler>();
        services.AddScoped<GetStudentCompaniesQueryHandler>();
        services.AddScoped<GetStudentRolesQueryHandler>();
        services.AddScoped<CreateStudentCommandHandler>();
        services.AddScoped<UpdateStudentCommandHandler>();
        services.AddScoped<ChangeStudentPasswordCommandHandler>();
        services.AddScoped<DeactivateStudentCommandHandler>();

        services.AddScoped<ListCompaniesQueryHandler>();
        services.AddScoped<ListManagersQueryHandler>();
        services.AddScoped<GetManagerByIdQueryHandler>();
        services.AddScoped<GetManagerCompaniesQueryHandler>();
        services.AddScoped<GetManagerRolesQueryHandler>();
        services.AddScoped<CreateManagerCommandHandler>();
        services.AddScoped<UpdateManagerCommandHandler>();
        services.AddScoped<ChangeManagerPasswordCommandHandler>();

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

        services.AddScoped<ListSupportRequestsQueryHandler>();
        services.AddScoped<GetSupportRequestByIdQueryHandler>();
        services.AddScoped<CreateSupportRequestCommandHandler>();
        services.AddScoped<UpdateSupportRequestStatusCommandHandler>();

        return services;
    }
}
