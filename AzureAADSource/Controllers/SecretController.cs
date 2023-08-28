using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AzureAADSource.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Linq;
using System.Diagnostics;
using System.Text;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecretController : ControllerBase
    {
        private CipherTools _cipherTools;
        private const string SECRET_KEY = "mobileKey";
        private const string SECRET_PATIENT_BLOB_KEY = "NIS-1-PATIENT-12345-BLOB";
        private const string serverPrivateKeyPem = "-----BEGIN EC PRIVATE KEY-----\r\nMIIBaAIBAQQgFJU4mP0PhwKmWlMuBOlqn1tYaCFPuotpBMC6defPpa2ggfowgfcC\r\nAQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAAAAAAAAAAAAAA////////////////\r\nMFsEIP////8AAAABAAAAAAAAAAAAAAAA///////////////8BCBaxjXYqjqT57Pr\r\nvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMVAMSdNgiG5wSTamZ44ROdJreBn36QBEEE\r\naxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg9KE5RdiYwpZP40Li/hp/m47n60p8D54W\r\nK84zV2sxXs7LtkBoN79R9QIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8\r\nYyVRAgEBoUQDQgAEnf0O6t2D1/UOj1EUGFsMaQLqXXMWxbwYWmc7kGQlOLGL3Qcj\r\nVms5ws/Te6KklMXPYXO7A+NVeRqarjGohM0GLA==\r\n-----END EC PRIVATE KEY-----";
        private const string mobilePublicKeyPem = "-----BEGIN PUBLIC KEY-----\r\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEbMECUW9nmiTCqouARPQGXr8Auy9R\r\n1JDIKyrLItxIsUYny8MVpvjLV1ASoyLHfRPGEmTIbg0Tq9r+Oj9XT5Oq1w==\r\n-----END PUBLIC KEY-----";

        public SecretController(CipherTools cipherTools)
        {
            _cipherTools = cipherTools;
        }

        [HttpGet]
        public async Task<string> GetAsync()
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
            var client = new SecretClient(new Uri("https://azure-keyvault-jk.vault.azure.net/"), new DefaultAzureCredential(), options);

            string oldValue = "N/A";
            var stopwatch = Stopwatch.StartNew();
            var secretValue = client.GetSecret(SECRET_KEY);
            if (secretValue.Value != null)
            {
                oldValue = secretValue.Value.Value;
            }
            stopwatch.Stop();
            var readTime = stopwatch.Elapsed;
            stopwatch.Restart();
            var newValue = $"ABCDEF-{DateTime.Now.Ticks}";
            client.SetSecret(SECRET_KEY, newValue);
            stopwatch.Stop();
            var writeTime = stopwatch.Elapsed;

            var privateKey = _cipherTools.ReadPrivateKey(serverPrivateKeyPem);
            var publicKey = _cipherTools.ReadPublicKey(mobilePublicKeyPem);
            byte[] derivedKey = _cipherTools.DeriveKey(privateKey, publicKey);
            var derivedKeyString = Convert.ToBase64String(derivedKey);
            client.SetSecret(SECRET_PATIENT_BLOB_KEY, derivedKeyString);

            return $"Test ok - old value = {oldValue} in {readTime.Milliseconds}ms, new value = {newValue} in {writeTime.Milliseconds}ms";
        }
    }
}
