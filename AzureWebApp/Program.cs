using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AzureWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // Program.cs

            var identityUrl = builder.Configuration.GetValue<string>("IdentityUrl");
            var callBackUrl = builder.Configuration.GetValue<string>("CallBackUrl");
            var clientId = builder.Configuration.GetValue<string>("ClientId");
            var clientSecret = builder.Configuration.GetValue<string>("ClientSecret");
            var sessionCookieLifetime = builder.Configuration.GetValue("SessionCookieLifetimeMinutes", 60);

            // Add Authentication services

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, setup =>
            {
                setup.ExpireTimeSpan = TimeSpan.FromMinutes(sessionCookieLifetime);
            })
            .AddOpenIdConnect("oidc", options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = identityUrl?.ToString();
                options.SignedOutRedirectUri = callBackUrl?.ToString() ?? "";
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = "code id_token";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.RequireHttpsMetadata = false;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("offline_access");
                if (clientId != null)
                {
                    options.Scope.Add(clientId);
                }
                options.Events.OnTokenValidated = context =>
                {
                    if (context.TokenEndpointResponse != null)
                    {
                        var identity = (ClaimsIdentity)context.Principal.Identity;
                        identity.AddClaim(new Claim("access_token", context.TokenEndpointResponse.AccessToken));
                        identity.AddClaim(new Claim("refresh_token", context.TokenEndpointResponse.RefreshToken));

                        // so that we don't issue a session cookie but one with a fixed expiration
                        context.Properties.IsPersistent = true;

                        // align expiration of the cookie with expiration of the
                        // access token ? refresh token
                        var token = new JwtSecurityToken(context.TokenEndpointResponse.AccessToken);
                        context.Properties.ExpiresUtc = token.ValidTo;
                    }
                    return Task.CompletedTask;
                };
                options.Events.OnTokenResponseReceived = context =>
                {
                    var message = context.ProtocolMessage;
                    return Task.FromResult(0);
                };
            });

            // Build the app
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}