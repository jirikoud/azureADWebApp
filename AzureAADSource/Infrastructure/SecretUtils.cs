using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace AzureAADSource.Infrastructure
{
    public class SecretUtils
    {
        public static byte[]? GetSecretKey(int nisId, int patientId)
        {
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                }
            };
            //Uses ENVIRONMENT properties 
            var client = new SecretClient(new Uri("https://azure-keyvault-jk.vault.azure.net/"), new DefaultAzureCredential(), options);
            var keyName = $"NIS-{nisId}-PATIENT-{patientId}-BLOB";
            var azureResponse = client.GetSecret(keyName);
            if (azureResponse.Value != null)
            {
                var secretValue = azureResponse.Value.Value;
                var secretData = Convert.FromBase64String(secretValue);
                return secretData;
            }
            return null;
        }
    }
}
