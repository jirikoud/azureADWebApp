using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureAADSource.Infrastructure;
using AzureAADSource.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlobController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {
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

                var message = Utils.GenerateLargeMessage(500000);
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));

                BlobClient blobClient = containerClient.GetBlobClient("patient_data.cjson");
                if (await blobClient.ExistsAsync())
                {
                    var downStream = await blobClient.OpenReadAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(downStream);
                    var jsonModel = JsonSerializer.Deserialize<JsonModel>(jsonDocument);
                }

                var content = await blobClient.UploadAsync(stream, true);
                var tags = new Dictionary<string, string>()
                {
                    { "patient_id", "12345" }
                };
                blobClient.SetTags(tags);

                return Ok();
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
