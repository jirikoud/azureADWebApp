using System;
using System.Text;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace TimerFunctionNIS
{
    public class QueueFunction
    {
        [FunctionName("QueueFunction")]
        public void Run([QueueTrigger("patient-profile", Connection = "AZURE_STORAGE_CONNECTION_STRING")] string message, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {message}");

            var parts = message.Split(new char[] { '=' });
            if (parts.Length == 2)
            {
                var idString = parts[1];
                var patientId = int.Parse(idString);
                if (patientId % 2 == 0)
                {
                    throw new Exception($"PatientId {patientId} is even => poison!");
                }
            }
            else
            {
                throw new Exception($"Invalid format of message '{message}', poison!");
            }
        }
    }
}
