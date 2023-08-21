using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureAADSource.Infrastructure;
using AzureAADSource.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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

        public BlobController(CipherTools cipherTools)
        {
            _cipherTools = cipherTools;
        }

        private enum CipherType
        {
            Plain,
            ChaChaPoly,
            Rsa
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
                    if (cipherType == CipherType.Rsa)
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
            if (cipherType == CipherType.Rsa)
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

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {
                // Create derived key for ChaChaPoly cipher
                var privateKey = _cipherTools.ReadPrivateKey(serverPrivateKeyPem);
                var publicKey = _cipherTools.ReadPublicKey(mobilePublicKeyPem);
                byte[] derivedKey = _cipherTools.DeriveKey(privateKey, publicKey);

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
                BlobClient blobClientRsa = containerClient.GetBlobClient("patient_data.rjson");

                var downloadPlain = await EstimateDownload(blobClientPlain, CipherType.Plain);
                var downloadChaCha = await EstimateDownload(blobClientChaCha, CipherType.ChaChaPoly, derivedKey);
                var downloadRsa = await EstimateDownload(blobClientRsa, CipherType.Rsa, derivedKey);

                var message = Utils.GenerateLargeMessage(20000);

                var uploadPlain = await EstimateUpload(blobClientPlain, message, CipherType.Plain);
                var uploadChaCha = await EstimateUpload(blobClientChaCha, message, CipherType.ChaChaPoly, derivedKey);
                var uploadRsa = await EstimateUpload(blobClientRsa, message, CipherType.Rsa, derivedKey);

                var responseTest = $"Download estimates: Plain {downloadPlain}ms, ChaCha {downloadChaCha}ms, Rsa {downloadRsa}ms; Upload estimates: File size = {message.Length}B, Plain {uploadPlain}ms, ChaCha {uploadChaCha}ms, Rsa {uploadRsa}ms";
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
