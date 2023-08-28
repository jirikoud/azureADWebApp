using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using AzureAADSource.Models.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private const string QUEUE_NAME = "patient-profile";
        private readonly QueueClient queueClient;

        public QueueController() 
        {
            // Retrieve the connection string for use with the application. 
            string? connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            // Create a BlobServiceClient object 
            this.queueClient = new QueueClient(connectionString, QUEUE_NAME, new QueueClientOptions();
        }

        [HttpGet]
        public async Task<string> GetAsync()
        {
            var azureResponse = await queueClient.ReceiveMessageAsync();
            if (azureResponse.Value != null)
            {
                var content = Encoding.UTF8.GetString(Convert.FromBase64String(azureResponse.Value.Body.ToString()));
                return content;
            }
            return "No message";
        }

        [HttpPost]
        public async Task PostMessage(MessageModel model)
        {
            await queueClient.SendMessageAsync(model.Message);
        }
    }
}
