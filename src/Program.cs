using System.Reflection;
using System.Text.Json;
using CodeMetricCollector.Services;
using CodeMetricCollector.Services.Strategies;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Code Metric Collector",
        Version = "v1",
        Description = "API that collects code metrics from GitHub repositories. " +
                      "Currently supports counting HTTP endpoints declared in Java (Spring Boot) " +
                      "and .NET (ASP.NET Core) controller classes.",
        Contact = new OpenApiContact
        {
            Name = "Code Metric Collector",
            Url = new Uri("https://github.com")
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IEndpointCounterStrategy, JavaEndpointCounterStrategy>();
builder.Services.AddScoped<IEndpointCounterStrategy, DotNetEndpointCounterStrategy>();
builder.Services.AddScoped<EndpointCountService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Code Metric Collector v1");
    c.RoutePrefix = string.Empty; // serve Swagger UI at root
    c.DisplayRequestDuration();
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
