using AzureAADSource.Controllers;
using AzureAADSource.Models.DatabaseModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AzureAADSource.Infrastructure
{
    public class DbContext
    {
        public const string DATABASE_NAME = "mPatient";
        public const string CONTAINER_MOBILE_DEVICES = "mobileDevices";
        public const string CONTAINER_PATIENT_PAIRING = "patientPairing";
        public const string PARTITION_KEY = "1";

        private readonly ILogger _logger;
        private CosmosClient? _cosmosClient = null;

        public DbContext(ILogger<AppointmentController> logger) 
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var endpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");
                var primaryKey = Environment.GetEnvironmentVariable("COSMOS_KEY");

                var cosmosClientOptions = new CosmosClientOptions
                {
                    ApplicationName = "mPatient",
                };
                _cosmosClient = new CosmosClient(endpoint, primaryKey, cosmosClientOptions);

                var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(DATABASE_NAME);

                var containerMobileDevices = await database.Database.CreateContainerIfNotExistsAsync(
                    id: CONTAINER_MOBILE_DEVICES,
                    partitionKeyPath: "/PartitionId",
                    throughput: 400
                );
                var containerUserSettings = await database.Database.CreateContainerIfNotExistsAsync(
                    id: CONTAINER_PATIENT_PAIRING,
                    partitionKeyPath: "/PartitionId",
                    throughput: 400
                );
                //POZOR - součet napříč kontejnery nesmí přesáhnout 1000!
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "CosmosDB init failed");
            }
        }

        public async Task<MobileDevice> CreateMobileDeviceAsync(MobileDevice mobileDevice)
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_MOBILE_DEVICES);
            var createdItem = await container.CreateItemAsync(
                item: mobileDevice,
                partitionKey: new PartitionKey(PARTITION_KEY)
            );
            return createdItem;
        }

        public async Task<List<MobileDevice>> ListMobileDevicesAsync()
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_MOBILE_DEVICES);
            var query = container.GetItemLinqQueryable<MobileDevice>();
            var iterator = query.ToFeedIterator();
            var results = await iterator.ReadNextAsync();
            return results.ToList();
        }

        public async Task<MobileDevice> FindMobileDevice(Guid id)
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_MOBILE_DEVICES);
            var item = await container.ReadItemAsync<MobileDevice>(id.ToString(), new PartitionKey(PARTITION_KEY));
            return item;
        }

        public async Task<bool> UpdateMobileDevice(MobileDevice mobileDevice)
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_MOBILE_DEVICES);
            var response = await container.ReplaceItemAsync<MobileDevice>(mobileDevice, mobileDevice.Id.ToString(), new PartitionKey(PARTITION_KEY));
            return (response.StatusCode == System.Net.HttpStatusCode.OK);
        }

        public async Task<PatientPairing> CreatePatientPairingAsync(PatientPairing patientPairing)
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_PATIENT_PAIRING);
            var createdItem = await container.CreateItemAsync(
                item: patientPairing,
                partitionKey: new PartitionKey(PARTITION_KEY)
            );
            return createdItem;
        }

        public async Task<List<PatientPairing>> GetPatientPairingsByUsernameAsync(string username)
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_PATIENT_PAIRING);
            var query = container.GetItemLinqQueryable<PatientPairing>().Where(item => item.Username == username);
            var iterator = query.ToFeedIterator();
            var results = await iterator.ReadNextAsync();
            return results.ToList();
        }

        public async Task<List<PatientPairing>> GetPatientPairingByIdAndNISAsync(int patientId, int nisId)
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_PATIENT_PAIRING);
            var query = container.GetItemLinqQueryable<PatientPairing>().Where(item => item.PatientId == patientId && item.NISId == nisId);
            var iterator = query.ToFeedIterator();
            var results = await iterator.ReadNextAsync();
            return results.ToList();
        }

        public async Task<bool> GetPatientPairingExistsAsync(string username, int patientId, int nisId)
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_PATIENT_PAIRING);
            var query = container.GetItemLinqQueryable<PatientPairing>().Where(item => item.Username == username && item.PatientId == patientId && item.NISId == nisId);
            var count = await query.CountAsync();
            return (count > 0);
        }

        public async Task<List<PatientPairing>> GetPatientCurrentAsync(DateTime limit)
        {
            var container = _cosmosClient!.GetContainer(DATABASE_NAME, CONTAINER_PATIENT_PAIRING);
            var query = container.GetItemLinqQueryable<PatientPairing>().Where(item => item.Created > limit);
            var iterator = query.ToFeedIterator();
            var results = await iterator.ReadNextAsync();
            return results.ToList();
        }


    }
}
