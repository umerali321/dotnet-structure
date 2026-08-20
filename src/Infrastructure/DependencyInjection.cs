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
using SkillsetsBackend.Application.Managers.Interfaces;
using SkillsetsBackend.Infrastructure.Managers;
using SkillsetsBackend.Application.Faqs.Interfaces;
using SkillsetsBackend.Infrastructure.Faqs;
using SkillsetsBackend.Application.SupportContacts.Interfaces;
using SkillsetsBackend.Infrastructure.SupportContacts;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Infrastructure.Skillsoft;
using SkillsetsBackend.Infrastructure.Skillsoft.Olsa;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Infrastructure.CourseLibrary;
using SkillsetsBackend.Application.RoleManagement.Interfaces;
using SkillsetsBackend.Infrastructure.RoleManagement;
using SkillsetsBackend.Application.Dashboard.Interfaces;
using SkillsetsBackend.Infrastructure.Dashboard;
using SkillsetsBackend.Infrastructure.Email;
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

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(3)));

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
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
        services.AddScoped<ILoginActivityLogRepository, LoginActivityLogRepository>();

        services.AddScoped<ICurrentCompanyContext, CurrentCompanyContext>();

        services.AddScoped<IStudentQueryService, StudentQueryService>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ICompanyQueryService, CompanyQueryService>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IManagerQueryService, ManagerQueryService>();
        services.AddScoped<IManagerRepository, ManagerRepository>();
        services.AddScoped<IFaqRepository, FaqRepository>();
        services.AddScoped<ISupportContactRepository, SupportContactRepository>();
        services.AddScoped<ICourseLibraryQueryService, CourseLibraryQueryService>();
        services.AddScoped<ICourseTakenRepository, CourseTakenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();

        services.AddMemoryCache();
        services.AddOptions<SkillsoftSsoSettings>()
            .Bind(configuration.GetSection(SkillsoftSsoSettings.SectionName));
        services.AddOptions<SkillsoftOlsaSettings>()
            .Bind(configuration.GetSection(SkillsoftOlsaSettings.SectionName));
        services.AddOptions<SkillsoftProvisioningSettings>()
            .Bind(configuration.GetSection(SkillsoftProvisioningSettings.SectionName));
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddHttpClient<OlsaSoapClient>();
        services.AddHttpClient<SkillsoftProvisioningClient>();
        services.AddScoped<ActiveLibraryCardResolver>();
        services.AddScoped<StudentManagerResolver>();
        services.AddScoped<SkillportSessionManager>();
        services.AddScoped<ISkillportSessionService>(provider => provider.GetRequiredService<SkillportSessionManager>());
        services.AddScoped<SkillsoftAccessGuard>();
        services.AddScoped<ISkillsoftSsoService, SkillsoftSsoService>();
        services.AddScoped<ISkillsoftCatalogService, SkillsoftCatalogService>();
        services.AddScoped<ISkillsoftTranscriptService, SkillsoftTranscriptService>();
        services.AddScoped<ISkillsoftProvisioningService, SkillsoftProvisioningService>();

        return services;
    }
}
