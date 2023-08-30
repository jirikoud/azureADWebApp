using Newtonsoft.Json;

namespace AzureAADSource.Models.DatabaseModels
{
    public class PatientPairing
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public string PartitionId { get; set; }
        public string Username { get; set; }

        public int PatientId { get; set; }

        public int NISId { get; set; }
        public string CommunicationKey { get; set; }
        public string PatientIdentHash { get; set; }
        public DateTime Created { get; set; }
    }
}
