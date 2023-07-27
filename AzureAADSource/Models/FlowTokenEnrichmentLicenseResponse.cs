using System.Text.Json.Serialization;

namespace AzureAADSource.Models
{
    public class FlowTokenEnrichmentLicenseResponse
    {
        public string Action { get; set; } = "Continue";

        [JsonPropertyName("extension_clinics")]
        public string Clinics { get; set; }

        public string Version { get; set; }
    }
}
