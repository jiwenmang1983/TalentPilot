using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TalentPilot.Api;
using TalentPilot.Api.Data;
using TalentPilot.Api.Middleware;
using TalentPilot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TalentPilot API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Database
builder.Services.AddDbContext<TalentPilotDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Port=3306;Database=talentpilot;User=root;Password=password;";
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] ?? "TalentPilotSecretKey2026VeryLongAndSecure";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "TalentPilot";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "TalentPilotUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Register services
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<OperationLogService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<CandidateConsentService>();
builder.Services.AddScoped<JobPostService>();
builder.Services.AddScoped<ResumeCollectionService>();
builder.Services.AddScoped<ResumeParsingService>();
builder.Services.AddScoped<MatchingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseJwtMiddleware();
app.UseAuthorization();

app.MapControllers();

// Initialize seed data
await SeedData.InitializeAsync(app.Services);

app.Run();
