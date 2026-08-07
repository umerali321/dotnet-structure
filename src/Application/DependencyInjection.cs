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

        return services;
    }
}
