using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace TimerFunctionNIS
{
    public class TimedFunction
    {
        private const string QUEUE_NAME = "patient-profile";

        [FunctionName("TimedFunction")]
        public void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}, past due: {myTimer.IsPastDue}");

            //Adds new items into queue to process by other functions (simulates sync messages from mEx)

            // Retrieve the connection string for use with the application. 
            string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            // Create a BlobServiceClient object 
            var queueClient = new QueueClient(connectionString, QUEUE_NAME);

            var random = new Random();
            var message = $"New patient profile Id={random.Next(10000, 100000)}";
            queueClient.SendMessage(Convert.ToBase64String(Encoding.UTF8.GetBytes(message)));
        }
    }
}
