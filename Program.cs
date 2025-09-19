using MCPServer.Configuration;
using MCPServer.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add configuration options
builder.Services.Configure<ClaudeOptions>(
    builder.Configuration.GetSection(ClaudeOptions.SectionName));
builder.Services.Configure<MCPOptions>(
    builder.Configuration.GetSection(MCPOptions.SectionName));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "MCP Server API",
        Version = "v1",
        Description = "Model Context Protocol Server with Claude AI integration"
    });
});

// Configure HttpClient for Claude API
builder.Services.AddHttpClient<IClaudeApiService, ClaudeApiService>();

// Register application services
builder.Services.AddScoped<IClaudeApiService, ClaudeApiService>();
builder.Services.AddScoped<IMCPToolService, MCPToolService>();

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Server API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.UseRouting();
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MCP Server starting on port 8080");
logger.LogInformation("Swagger UI available at: http://localhost:8080/swagger");

app.Run();
