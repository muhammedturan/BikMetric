using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BikMetric.Api.Middleware;
using BikMetric.Application.Common.Behaviors;
using BikMetric.Application.Common.Interfaces;
using BikMetric.Infrastructure.Repositories;
using BikMetric.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate required configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is not configured. Set 'ConnectionStrings:DefaultConnection' in " +
        "appsettings.json or via environment variable 'ConnectionStrings__DefaultConnection'.");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// JWT Authentication
builder.Services.AddAuthentication(options =>
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
    };
});

builder.Services.AddAuthorization();

// Authentication services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Repository
builder.Services.AddScoped<IDapperRepository, DapperRepository>();

// Dynamic query service
builder.Services.AddScoped<IDynamicQueryService, DynamicQueryService>();

// Ollama service
builder.Services.AddHttpClient("Ollama", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434");
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddScoped<IOllamaService, OllamaService>();

// MediatR with validation pipeline
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(BikMetric.Application.Auth.Commands.Login.LoginCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(BikMetric.Application.Auth.Commands.Login.LoginCommand).Assembly);

var app = builder.Build();

// Run migrations and seed on startup
await BikMetric.Infrastructure.Data.MigrationRunner.RunMigrations(app.Configuration);

using (var scope = app.Services.CreateScope())
{
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await BikMetric.Infrastructure.Data.SeedData.Seed(app.Configuration, passwordHasher);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BikMetric API v1");
    });
}

app.UseCors("AllowVue");

// Global exception handling
app.UseMiddleware<ExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
