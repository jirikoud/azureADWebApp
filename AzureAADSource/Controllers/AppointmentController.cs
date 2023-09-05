using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using AzureAADSource.Infrastructure;
using AzureAADSource.Models.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using AzureAADSource.Models;
using Org.BouncyCastle.Tls;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("application/json", "application/octet-stream")]
    [Produces("application/json", "application/octet-stream")]
    [SupportsCipher]
    [Authorize]
    public class AppointmentController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DbContext _dbContext;
        private CipherTools _cipherTools;

        public AppointmentController(ILogger<AppointmentController> logger, DbContext dbContext, CipherTools cipherTools)
        {
            _logger = logger;
            _dbContext = dbContext;
            _cipherTools = cipherTools;
        }

        [HttpGet]
        [Route("list")]
        [RequiredScope("access_as_user")]
        public async Task<ActionResult<ListResponseModel>> ListAsync()
        {
            try
            {
                long.TryParse(Request.Headers["If-None-Match"].ToString(), out long eTag);

                // Retrieve the connection string for use with the application. 
                string? connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

                // Create a BlobServiceClient object 
                var blobServiceClient = new BlobServiceClient(connectionString);

                //Create a unique name for the container
                string containerName = "patient-data-cache";

                // Create the container and return a container client object
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                BlobClient blobClientChaCha = containerClient.GetBlobClient("patient_data.cjson");

                if (await blobClientChaCha.ExistsAsync())
                {
                    long timeStamp = 0;
                    var tagResponse = await blobClientChaCha.GetTagsAsync();
                    if (tagResponse.Value != null)
                    {
                        var timeStampTag = tagResponse.Value.Tags["timestamp"];
                        if (timeStampTag != null)
                        {
                            if (long.TryParse(timeStampTag, out timeStamp))
                            {
                                //Etag je stale aktualni
                                if (timeStamp <= eTag)
                                {
                                    return StatusCode(304);
                                }
                            }
                        }
                    }

                    var downStream = await blobClientChaCha.OpenReadAsync();

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
                    //Desifrovaci klic
                    var derivedKey = SecretUtils.GetSecretKey(1, 12345);

                    var decryptedData = _cipherTools.DecryptWithChaChaPoly(memoryStream.ToArray(), derivedKey!);
                    var json = Encoding.UTF8.GetString(decryptedData);
                    JsonDocument jsonDocument = JsonDocument.Parse(json);
                    var jsonModel = JsonSerializer.Deserialize<ListResponseModel>(jsonDocument);
                    if (timeStamp > 0)
                    {
                        Response.Headers.Add("ETag", timeStamp.ToString());
                    }
                    return Ok(jsonModel);
                }

                //var random = new Random();
                //var itemCount = random.Next(1, 10);
                //var list = ListResponseModel.CreateMock(itemCount);
                var emptyList = new ListResponseModel() { List = new List<ListItemModel>() };
                return Ok();
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
