using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SurlesMobile.Api.Services;
using Serilog;

namespace SurlesMobile.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") 
                             ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        
        services.AddDbContextPool<PortfolioDb>(options => 
            options.UseNpgsql(connectionString));
        
        return services;
    }

    public static IServiceCollection AddAwsServices(this IServiceCollection services)
    {
        services.AddAWSService<IAmazonSecretsManager>();
        services.AddScoped<ISecretsService, AwsSecretsService>();
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        var issuer = Environment.GetEnvironmentVariable("COGNITO__ISSUER");
        var audience = Environment.GetEnvironmentVariable("COGNITO__AUDIENCE");

        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            throw new InvalidOperationException("COGNITO__ISSUER and COGNITO__AUDIENCE environment variables must be set");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = issuer;
                options.TokenValidationParameters = new TokenValidationParameters 
                { 
                    ValidAudience = audience,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        return services;
    }

    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Read", policy => policy.RequireAssertion(context =>
                context.User.IsInRole("viewer") || context.User.IsInRole("editor") || context.User.IsInRole("admin") ||
                context.User.HasClaim("scope", scope => scope.Split(' ').Contains("portfolio/read"))));

            options.AddPolicy("Write", policy => policy.RequireAssertion(context =>
                context.User.IsInRole("editor") || context.User.IsInRole("admin") ||
                context.User.HasClaim("scope", scope => scope.Split(' ').Contains("portfolio/write"))));

            options.AddPolicy("Admin", policy => policy.RequireAssertion(context =>
                context.User.IsInRole("admin") ||
                context.User.HasClaim("scope", scope => scope.Split(' ').Contains("portfolio/admin"))));
        });

        return services;
    }

    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<PortfolioDb>("database")
            .AddNpgSql(
                Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio",
                name: "postgresql");

        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                           ?? new[] { "http://localhost:4200", "https://localhost:4200" };

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngularApp", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() 
            { 
                Title = "SurlesMobile API", 
                Version = "v1",
                Description = "Portfolio management API with JWT authentication and RBAC authorization"
            });
            
            options.AddSecurityDefinition("Bearer", new()
            {
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter: Bearer {token}"
            });
            
            options.AddSecurityRequirement(new()
            {
                {
                    new() 
                    { 
                        Reference = new() 
                        { 
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, 
                            Id = "Bearer" 
                        } 
                    }, 
                    new string[] {} 
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddSerilogLogging(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddSerilog();
        return services;
    }
}