using System.Text;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Auth;
using SkillsetsBackend.Infrastructure.Options;
using SkillsetsBackend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace SkillsetsBackend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. Set it in appsettings.json, an environment-specific appsettings file, or the ConnectionStrings__DefaultConnection environment variable.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Roles.SuperAdmin, policy => policy.RequireRole(Roles.SuperAdmin));
            options.AddPolicy(Roles.CompanyAdmin, policy => policy.RequireRole(Roles.CompanyAdmin));
            options.AddPolicy(Roles.Employee, policy => policy.RequireRole(Roles.Employee));
        });

        services.AddOptions<SuperAdminSettings>()
            .Bind(configuration.GetSection(SuperAdminSettings.SectionName))
            .ValidateDataAnnotations();

        var superAdminSettings = configuration.GetSection(SuperAdminSettings.SectionName).Get<SuperAdminSettings>();
        if (superAdminSettings is null
            || superAdminSettings.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(superAdminSettings.Email)
            || string.IsNullOrWhiteSpace(superAdminSettings.PasswordHash))
        {
            throw new InvalidOperationException(
                "SuperAdmin settings are not configured. Set SuperAdmin:Id, SuperAdmin:Email, and SuperAdmin:PasswordHash in configuration.");
        }

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISuperAdminAuthenticator, SuperAdminAuthenticator>();
        services.AddSingleton<ITokenService, TokenService>();

        // TODO: swap for an EF-Core-backed IRefreshTokenRepository once a database is connected.
        // See Infrastructure/Auth/InMemoryRefreshTokenRepository.cs for details - no other code
        // needs to change.
        services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();

        return services;
    }
}
