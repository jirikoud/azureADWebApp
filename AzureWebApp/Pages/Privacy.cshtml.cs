using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AzureWebApp.Pages
{
    [Authorize]
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;

        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? Email { get; set; }
        public string? Ident { get; set; }
        public string? Pets { get; set; }
        public string? Season { get; set; }
        public string? Flow { get; set; }

        public PrivacyModel(ILogger<PrivacyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                this.Firstname = identity.Claims.FirstOrDefault(item => item.Type == "given_name")?.Value;
                this.Lastname = identity.Claims.FirstOrDefault(item => item.Type == "family_name")?.Value;
                this.Email = identity.Claims.FirstOrDefault(item => item.Type == "emails")?.Value;
                this.Ident = identity.Claims.FirstOrDefault(item => item.Type == "oid")?.Value;
                this.Flow = identity.Claims.FirstOrDefault(item => item.Type == "tfp")?.Value;
                this.Pets = identity.Claims.FirstOrDefault(item => item.Type == "extension_LovesPets")?.Value;
                this.Season = identity.Claims.FirstOrDefault(item => item.Type == "extension_FavouriteSeason")?.Value;
            }
        }
    }
}