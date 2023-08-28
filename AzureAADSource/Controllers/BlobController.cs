using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureAADSource.Infrastructure;
using AzureAADSource.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlobController : ControllerBase
    {
        private CipherTools _cipherTools;
        private const string serverPrivateKeyPem = "-----BEGIN EC PRIVATE KEY-----\r\nMIIBaAIBAQQgFJU4mP0PhwKmWlMuBOlqn1tYaCFPuotpBMC6defPpa2ggfowgfcC\r\nAQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAAAAAAAAAAAAAA////////////////\r\nMFsEIP////8AAAABAAAAAAAAAAAAAAAA///////////////8BCBaxjXYqjqT57Pr\r\nvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMVAMSdNgiG5wSTamZ44ROdJreBn36QBEEE\r\naxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg9KE5RdiYwpZP40Li/hp/m47n60p8D54W\r\nK84zV2sxXs7LtkBoN79R9QIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8\r\nYyVRAgEBoUQDQgAEnf0O6t2D1/UOj1EUGFsMaQLqXXMWxbwYWmc7kGQlOLGL3Qcj\r\nVms5ws/Te6KklMXPYXO7A+NVeRqarjGohM0GLA==\r\n-----END EC PRIVATE KEY-----";
        private const string mobilePublicKeyPem = "-----BEGIN PUBLIC KEY-----\r\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEbMECUW9nmiTCqouARPQGXr8Auy9R\r\n1JDIKyrLItxIsUYny8MVpvjLV1ASoyLHfRPGEmTIbg0Tq9r+Oj9XT5Oq1w==\r\n-----END PUBLIC KEY-----";

        //private const string privateKeyPemRsa = "-----BEGIN RSA PRIVATE KEY-----\r\n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\r\n-----END RSA PRIVATE KEY-----\r\n";
        //private const string publicKeyPemRsa = "-----BEGIN PUBLIC KEY-----\r\nMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAtRf+yv2tcSfeAoYHUUZO\r\n52/rzDHsceojRKgz4TnLyX0YiERmM8SFkgb1t+Ay5k/f0/uOneo4cnx9yjahiqNA\r\n/vlljetNcf1U/LAxZX8HGvQYU7JYjCb9B3mO03Hr/tacl9xyupyeNB1neYM6aMT/\r\n9zoK1qkEtsfj2+giCewuI4EoHU2QIrMciHN/kubTrDkXPubsFj3pP0mytFX6NdQj\r\nPBdLaEkBzS5oMkKlg7t/Hg5nWcNoe1DqdCwgvt0x4U9ncX1ysIabw1olu+USsyq6\r\nEY2drpplLTaauRNiH4GwJnVz5nYtj5P/1vfbEH5yR3/OwRqlrtcWl3ynq3loc3ct\r\ny9S45XzUio5Kz9oJCR1Zvlkp7yzcvR69PQNPCa2n0LLas1bWPWiMt1AZNitxSOrE\r\ndsLi9mPp7SP/reRVJvYpaCNz/JwXoPEyphNXnNFtT2FklSkmqvmi1is+6UoNW9Da\r\n8/+7t+vNCepZpPuK3sx/kgO7ouIKnsDWo5WIGY+ddHeY4D6bYYEfSET/MKaE6W7R\r\nbDjc/OCJ8mWTYljrz0fXZi/vFu5sLuhG7ns47IerdvXR+OIZS0VVa7DyGGCpRksA\r\np82cc4ty6ORO/wMtmJXFXL15CDLJ3hCqc+ru/8O0Ns1atsCOxG+YeC7K3HbSFXif\r\nH9BXxR6LnmlJqT/g9V05rR0CAwEAAQ==\r\n-----END PUBLIC KEY-----";

        public BlobController(CipherTools cipherTools)
        {
            _cipherTools = cipherTools;
        }

        private enum CipherType
        {
            Plain,
            ChaChaPoly,
            Aes
        }

        private async Task<long> EstimateDownload(BlobClient blobClient, CipherType cipherType, byte[]? derivedKey = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (await blobClient.ExistsAsync())
                {
                    var downStream = await blobClient.OpenReadAsync();
                    if (cipherType == CipherType.Plain)
                    {
                        var memoryStream = new MemoryStream();
                        byte[] buffer = new byte[100000];
                        while (true)
                        {
                            var read = await downStream.ReadAsync(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, read);
                            if (downStream.Position == downStream.Length)
                            {
                                break;
                            }
                        }
                        JsonDocument jsonDocument = JsonDocument.Parse(memoryStream);
                        var jsonModel = JsonSerializer.Deserialize<JsonModel>(jsonDocument);
                    }
                    if (cipherType == CipherType.ChaChaPoly)
                    {
                        var memoryStream = new MemoryStream();
                        byte[] buffer = new byte[100000];
                        while (true)
                        {
                            var read = await downStream.ReadAsync(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, read);
                            if (downStream.Position == downStream.Length)
                            {
                                break;
                            }
                        }
                        var decryptedData = _cipherTools.DecryptWithChaChaPoly(memoryStream.ToArray(), derivedKey!);
                        var json = Encoding.UTF8.GetString(decryptedData);
                        JsonDocument jsonDocument = JsonDocument.Parse(json);
                        var jsonModel = JsonSerializer.Deserialize<JsonModel>(jsonDocument);
                    }
                    if (cipherType == CipherType.Aes)
                    {
                        var memoryStream = new MemoryStream();
                        byte[] buffer = new byte[100000];
                        while (true)
                        {
                            var read = await downStream.ReadAsync(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, read);
                            if (downStream.Position == downStream.Length)
                            {
                                break;
                            }
                        }
                        var decryptedData = _cipherTools.DecryptWithAes(memoryStream.ToArray(), derivedKey!);
                        var json = Encoding.UTF8.GetString(decryptedData);
                        JsonDocument jsonDocument = JsonDocument.Parse(json);
                        var jsonModel = JsonSerializer.Deserialize<JsonModel>(jsonDocument);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private async Task<long> EstimateUpload(BlobClient blobClient, string message, CipherType cipherType, byte[]? derivedKey = null)
        {
            var stopwatch = Stopwatch.StartNew();

            byte[] encryptedContent = new byte[0];
            if (cipherType == CipherType.Plain)
            {
                encryptedContent = Encoding.UTF8.GetBytes(message);
            }
            if (cipherType == CipherType.ChaChaPoly)
            {
                encryptedContent = _cipherTools.EncryptWithChaChaPoly(Encoding.UTF8.GetBytes(message), derivedKey!);
            }
            if (cipherType == CipherType.Aes)
            {
                encryptedContent = _cipherTools.EncryptWithAes(Encoding.UTF8.GetBytes(message), derivedKey!);
            }
            var stream = new MemoryStream(encryptedContent);

            var content = await blobClient.UploadAsync(stream, true);
            var tags = new Dictionary<string, string>()
                {
                    { "patient_id", "12345" }
                };
            blobClient.SetTags(tags);
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private byte[]? GetSecretKey(int nisId, int patientId)
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

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {
                //var rsaKeyPair = _cipherTools.GenerateRSAKeyPair();
                //string? publicKeyRsa = _cipherTools.GetPublicKeyPem(rsaKeyPair);
                //string? privateKeyRsa = _cipherTools.GetPrivateKeyPem(rsaKeyPair);

                // Create derived key for ChaChaPoly cipher
                //var privateKey = _cipherTools.ReadPrivateKey(serverPrivateKeyPem);
                //var publicKey = _cipherTools.ReadPublicKey(mobilePublicKeyPem);
                //byte[] derivedKey = _cipherTools.DeriveKey(privateKey, publicKey);

                //var rsaPublicKey = _cipherTools.CreateRsaKey(publicKeyPemRsa);
                //var rsaPrivateKey = _cipherTools.CreateRsaKey(privateKeyPemRsa);


                // Retrieve the connection string for use with the application. 
                string? connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

                // Create a BlobServiceClient object 
                var blobServiceClient = new BlobServiceClient(connectionString);

                //Create a unique name for the container
                string containerName = "patient-data-cache";

                // Create the container and return a container client object
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobs = containerClient.GetBlobsAsync();
                await foreach (BlobItem blobItem in blobs)
                {
                    Console.WriteLine("\t" + blobItem.Name);
                }

                BlobClient blobClientPlain = containerClient.GetBlobClient("patient_data.json");
                BlobClient blobClientChaCha = containerClient.GetBlobClient("patient_data.cjson");
                BlobClient blobClientRsa = containerClient.GetBlobClient("patient_data.ajson");

                var derivedKey = GetSecretKey(1, 12345);

                var downloadPlain = await EstimateDownload(blobClientPlain, CipherType.Plain);
                var downloadChaCha = await EstimateDownload(blobClientChaCha, CipherType.ChaChaPoly, derivedKey);
                var downloadAes = await EstimateDownload(blobClientRsa, CipherType.Aes, derivedKey);

                var message = Utils.GenerateLargeMessage(20000);

                var uploadPlain = await EstimateUpload(blobClientPlain, message, CipherType.Plain);
                var uploadChaCha = await EstimateUpload(blobClientChaCha, message, CipherType.ChaChaPoly, derivedKey);
                var uploadAes = await EstimateUpload(blobClientRsa, message, CipherType.Aes, derivedKey);

                var responseTest = $"Download estimates: Plain {downloadPlain}ms, ChaCha {downloadChaCha}ms, Aes {downloadAes}ms; Upload estimates: File size = {message.Length}B, Plain {uploadPlain}ms, ChaCha {uploadChaCha}ms, Aes {uploadAes}ms";
                return Ok(responseTest);
            }
            catch (Exception ex)
            {
                var errorText = $"Blob access failed. {ex}";
                Console.WriteLine(errorText);
                return Ok(errorText);
            }
        }
    }
}
