using AzureAADSource.Infrastructure;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace AzureAADSource
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddSwaggerGen(options =>
            {
                options.CustomSchemaIds(type => type.ToString());
                options.SwaggerDoc("latest", new OpenApiInfo
                {
                    Version = "1.0",
                    Title = "Azure Test API",
                    Description = "API pro komunikaci mobilních aplikací s testovacím backendem.",
                    Contact = new OpenApiContact
                    {
                        Name = "Cyberfox s.r.o.",
                        Email = "info@cyberfox.cz",
                        Url = new Uri("https://www.cyberfox.cz/"),
                    },
                });
                options.AddServer(new OpenApiServer() { Url = "https://liveapi.euc.cybertest.cz", Description = "DEV server" });
                options.AddServer(new OpenApiServer() { Url = "https://live-api-uat.euc.cz", Description = "UAT server" });
                options.AddServer(new OpenApiServer() { Url = "https://live-api.euc.cz", Description = "Production server (živá data)" });
                //options.UseAllOfToExtendReferenceSchemas(); // zpùsobí duplicitní hodnoty v enumech
                options.UseOneOfForPolymorphism();

                // Set the comments path for the Swagger JSON and UI.
                options.EnableAnnotations();
            });
            builder.Services.AddSingleton<CipherTools>();
            builder.Services.AddControllers();
            builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseSwagger(options =>
            {
                options.RouteTemplate = "/swagger/{documentName}/index.json";
            });
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/latest/index.json", "latest");
            });

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCipher();

            app.MapControllers();

            app.Run();
        }
    }
}