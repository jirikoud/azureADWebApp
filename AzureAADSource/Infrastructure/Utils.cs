using AzureAADSource.Models;
using System.Text.Json;
using System;

namespace AzureAADSource.Infrastructure
{
    public class Utils
    {
        public static string GenerateLargeMessage(int messageItems)
        {
            var random = new Random();
            var model = new JsonModel()
            {
                Title = "Secret message",
                SubTitle = "Secret subtitle",
                Items = new List<string>(),
            };
            for (int i = 0; i < messageItems; i++)
            {
                model.Items.Add($"Item{random.NextInt64()}");
            }
            string jsonString = JsonSerializer.Serialize(model);
            return jsonString;
        }
    }
}
