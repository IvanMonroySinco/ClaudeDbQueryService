using ClaudeDbQueryService.Core.Application.Configuration;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryService.Core.Application;
using Serilog;
// using SincoSoft.MYE.Middleware.Extensions;
using Microsoft.OpenApi.Models;
using SincoSoft.MYE.Common;
using System.Text.Json.Serialization;
using ClaudeDbQueryService.Infrastructure.External;
using ClaudeDbQueryService.Infrastructure.Persistence.DataBase;
using SincoSoft.MYE.Common.Middleware;
using SincoSoft.MYE.MiddlewareErrorHandler;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add configuration options
builder.Services.Configure<ClaudeOptions>(
    builder.Configuration.GetSection(ClaudeOptions.SectionName));
builder.Services.Configure<McpOptions>(
    builder.Configuration.GetSection(McpOptions.SectionName));

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);


builder.Services.AddApplication();
builder.Services.AddExternal(builder.Configuration);
builder.Services.AddPersistence<DataBaseService, IDataBaseService>(builder.Configuration);
builder.Services.AddControllers();

builder.Services.AddScoped<IDataBaseService, DataBaseService>(); // Aseg�rate de registrar tu implementaci�n de IDataBaseService
builder.Services.AddAutoMapper(typeof(Program)); // Registrar AutoMapper

builder.Services.AddEndpointsApiExplorer();
// Configure Swagger with Bearer JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Claude DB Query Service API",
        Version = "v1",
        Description = "Enterprise microservice for AI-powered database query processing with Clean Architecture"
    });

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
            new string[] {}
        }
    });
});


// Add SincoSoft middleware and AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors(c => c.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

app.UseMiddleware<MiddlewareSerilogEnrichment>();
app.UseMiddleware<MiddlewareAuthorizationToken>();
app.UseMiddleware<MiddlewareErrorHandler>();


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Claude DB Query Service API v1");
        c.RoutePrefix = "swagger";
    });
}

// TODO: Add SincoSoft middleware for authentication and error handling
// app.UseSincoSoftMiddleware();

app.MapControllers();
app.UseHttpsRedirection();

app.UseRouting();
app.UseStaticFiles();
app.UseAuthorization();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Claude DB Query Service starting on port 8080");
logger.LogInformation("Swagger UI available at: http://localhost:8080/swagger");

app.Run();
