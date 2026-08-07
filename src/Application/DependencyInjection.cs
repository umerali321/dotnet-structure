using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SkillsetsBackend.Application.Auth.Commands.Login;
using SkillsetsBackend.Application.Auth.Commands.Logout;
using SkillsetsBackend.Application.Auth.Commands.Refresh;
using SkillsetsBackend.Application.Auth.Commands.SwitchCompany;

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

        return services;
    }
}
