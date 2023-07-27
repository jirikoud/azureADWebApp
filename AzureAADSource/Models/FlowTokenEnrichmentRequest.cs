using System.Text.Json.Serialization;

namespace AzureAADSource.Models
{
    public class FlowTokenEnrichmentRequest
    {
        public class Identity
        {
            public string SignInType { get; set; }

            public string Issuer { get; set; }

            public string IssuerAssignedId { get; set; }
        }

        public string? Email { get; set; }

        public IEnumerable<Identity>? Identities { get; set; }

        public string DisplayName { get; set; }

        public string ObjectId { get; set; }

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }

        public string Step { get; set; }
    }
}
