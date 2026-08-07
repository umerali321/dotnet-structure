using System.Text;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common.Interfaces;
using SkillsetsBackend.Domain.Identity;
using SkillsetsBackend.Infrastructure.Auth;
using SkillsetsBackend.Infrastructure.Authorization;
using SkillsetsBackend.Infrastructure.Options;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Infrastructure.Students;
using SkillsetsBackend.Application.Companies.Interfaces;
using SkillsetsBackend.Infrastructure.Companies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationHandler, CompanyContextHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Roles.SuperAdmin, policy => policy.RequireRole(Roles.SuperAdmin));
            options.AddPolicy(Roles.Manager, policy => policy.RequireRole(Roles.Manager));
            options.AddPolicy(Roles.Student, policy => policy.RequireRole(Roles.Student));
            options.AddPolicy("CompanyContext", policy => policy.Requirements.Add(new CompanyContextRequirement()));
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
        services.AddSingleton<ILegacyCredentialVerifier, LegacyCredentialVerifier>();

        // Scoped: both depend on the scoped ApplicationDbContext.
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserDirectory, UserDirectory>();

        services.AddScoped<ICurrentCompanyContext, CurrentCompanyContext>();

        services.AddScoped<IStudentQueryService, StudentQueryService>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ICompanyQueryService, CompanyQueryService>();

        return services;
    }
}
