using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Middleware;
using NotificationService.Repositories;
using NotificationService.Services;
using NotificationService.Models;
using NotificationService.Utilities;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using NotificationService.ErrorHandling;
using NLog;
using NLog.Web;
using NLog.Extensions.Logging;
using NotificationService.BackgroundJobs;
using Microsoft.OpenApi.Models;
using Moq;

namespace NotificationService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var logConfigFile = "Nlog.config";
            var logger = NLog.LogManager.LoadConfiguration(logConfigFile).GetCurrentClassLogger();

            if (!File.Exists(logConfigFile))
            {
                logger.Warn($"Logging configuration file '{logConfigFile}' not found. Using default configuration.");
            }

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Get port from environment variable, default to 8080
                var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

                builder.Configuration
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();

                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection").Replace(@"\\", @"\");
                if (string.IsNullOrEmpty(connectionString))
                {
                    logger.Error("Connection string 'DefaultConnection' not found in appsettings.json.");
                    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
                }

                // Test the connection to the database early on
                using (var connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        await connection.OpenAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to connect to the database.");
                        throw new InvalidOperationException("Database connection failed.");
                    }
                }

                // Add services to the container (ConfigureServices)
                builder.Services.AddHangfire(config => config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(connectionString));

                builder.Services.AddHangfireServer(options =>
                {
                    options.WorkerCount = 20; // Adjust worker count as needed
                    options.Queues = new[] { "default" };
                    options.ShutdownTimeout = TimeSpan.FromMinutes(20); // 20 minutes timeout for server shutdown
                    options.SchedulePollingInterval = TimeSpan.FromMinutes(1); // Adjust polling interval for scheduled jobs (like emails) to be checked every minute
                });

                builder.Services.AddControllers();
                builder.Services.AddScoped<IEmailNotificationRepository, EmailNotificationRepository>();
                builder.Services.AddScoped<IEmailSenderService, EmailSenderService>();
                builder.Services.AddScoped<DBExecutor>();
                builder.Services.AddScoped<EmailJob>();
                builder.Services.AddTransient<EmailSenderService>();
                builder.Services.AddTransient<IDbConnection>(sp => new SqlConnection(connectionString));

                builder.Services.Configure<SmtpSettings>(options =>
                {
                    options.SmtpServer = builder.Configuration["EmailSettings:SmtpServer"]
                                         ?? Environment.GetEnvironmentVariable("EmailSettings__SmtpServer");
                    options.Port = int.Parse(builder.Configuration["EmailSettings:Port"] ?? "587");
                    options.Username = builder.Configuration["EmailSettings:Username"]
                                         ?? Environment.GetEnvironmentVariable("EmailSettings__Username");
                    options.Password = builder.Configuration["EmailSettings:Password"]
                                         ?? Environment.GetEnvironmentVariable("EmailSettings__Password");
                });

                var secretKey = builder.Configuration["JwtSettings:SecretKey"]
                                 ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");
                var issuer = builder.Configuration["JwtSettings:Issuer"]
                               ?? Environment.GetEnvironmentVariable("JwtSettings__Issuer");
                var audience = builder.Configuration["JwtSettings:Audience"]
                                 ?? Environment.GetEnvironmentVariable("JwtSettings__Audience");

                if (string.IsNullOrEmpty(secretKey))
                {
                    logger.Error("JWT Secret Key is missing or invalid.");
                    throw new InvalidOperationException("JWT Secret Key is missing or invalid.");
                }

                builder.Services.Configure<JwtSettings>(options =>
                {
                    options.Issuer = issuer;
                    options.Audience = audience;
                    options.SecretKey = secretKey;
                });

                builder.Services.AddScoped<JwtTokenService>();
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = issuer,
                            ValidAudience = audience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                        };
                    });

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(options =>
                {
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: 'Bearer your-token'",
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

                var app = builder.Build();
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                // Enable Swagger UI in Docker
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NotificationService API v1");
                    c.RoutePrefix = string.Empty; // Swagger available at "/"
                });

                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();

                // Only apply HTTPS redirection if not running in Docker environment
                if (!builder.Environment.IsDevelopment())
                {
                    app.UseHttpsRedirection();
                }

                app.MapControllers();
                app.UseHangfireDashboard("/hangfire");

                // Schedule the recurring job to check and send scheduled emails every minute
                RecurringJob.AddOrUpdate<EmailJob>(x => x.ProcessScheduledEmailsAsync(It.IsAny<CancellationToken>()), "*/1 * * * *");

                // Simple health check endpoint
                app.MapGet("/", () => "Hello, NotificationService is running!");

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Application failed to start.");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}
