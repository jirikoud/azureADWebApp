using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace AzureAADSource.Models.DatabaseModels
{
    public class MobileDevice
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public string PartitionId { get; set; }

        public string HardwareDevice { get; set; }
        public string OperatingSystem { get; set; }
        public string AppVersion { get; set; }
        public string? DeviceInfo { get; set; }

        public string? PushToken { get; set; }

        public string Username { get; set; }

        public DateTime LastAction { get; set; }

        public string Language { get; set; }

        public DateTime Created { get; set; }
        public string DebugUsername { get; set; }
    }
}
