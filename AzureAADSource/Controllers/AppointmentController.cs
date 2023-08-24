using Azure.Identity;
using AzureAADSource.Infrastructure;
using AzureAADSource.Models.Appointments;
using AzureAADSource.Models.DatabaseModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web.Resource;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("application/json", "application/octet-stream")]
    [Produces("application/json", "application/octet-stream")]
    [SupportsCipher]
    //[Authorize]
    public class AppointmentController : ControllerBase
    {
        private readonly ILogger _logger;

        public AppointmentController(ILogger<AppointmentController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("list")]
        [RequiredScope("access_as_user")]
        public async Task<ActionResult<ListResponseModel>> ListAsync()
        {
            try
            {
                var endpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");
                var primaryKey = Environment.GetEnvironmentVariable("COSMOS_KEY");

                using CosmosClient client = new(
                    accountEndpoint: endpoint!,
                    authKeyOrResourceToken: primaryKey!
                    );
                Database database = await client.CreateDatabaseIfNotExistsAsync("mPAtient");
                Container container = await database.CreateContainerIfNotExistsAsync(
                    id: "devices",
                    partitionKeyPath: "/categoryId",
                    throughput: 400
                );

                Device newItem = new Device()
                {
                    Id = "70b63682-b93a-4c77-aad2-65501347265f",
                    CategoryId = "61dba35b-4f02-45c5-b648-c6badc0cbd79",
                    CategoryName = "gear-surf-surfboards",
                    Name = "Yamba Surfboard",
                    Quantity = 12,
                    Sale = false
                };

                Device createdItem = await container.CreateItemAsync<Device>(
                    item: newItem,
                    partitionKey: new PartitionKey("61dba35b-4f02-45c5-b648-c6badc0cbd79")
                );

                var list = ListResponseModel.CreateMock();
                return Ok(list);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "List");
                return StatusCode(500, exception.Message);
            }
        }

        [HttpGet]
        [Route("detail/{id}")]
        [RequiredScope("access_as_user")]
        public async Task<ActionResult<DetailResponseModel>> DetailAsync(string id)
        {
            try
            {
                var detail = DetailResponseModel.CreateMock(id);
                return Ok(detail);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Detail({id})", id);
                return StatusCode(500, exception.Message);
            }
        }

    }
}
