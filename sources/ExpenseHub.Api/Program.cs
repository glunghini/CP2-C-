using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Configuration;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Endpoints;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExpenseHub.Api;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        JwtSettings jwtSettings = JwtSettings.FromConfiguration(builder.Configuration);
        builder.Services.AddExpenseHub(builder.Configuration, jwtSettings);
        builder.Services.AddOpenApi();

        WebApplication app = builder.Build();

        if (jwtSettings.UsesEphemeralKey)
        {
            app.Logger.LogWarning("Jwt:SigningKey não configurada. Uma chave temporária foi gerada e os tokens perdem a validade ao reiniciar.");
        }

        await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, app.Logger, CancellationToken.None);

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");

        app.MapAuthEndpoints();
        app.MapAdminEndpoints();

        await app.RunAsync();
    }
}
