using AzureAADSource.Infrastructure;
using AzureAADSource.Models.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

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

        public AppointmentController(ILogger<AppointmentController> logger, DbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        [Route("list")]
        [RequiredScope("access_as_user")]
        public async Task<ActionResult<ListResponseModel>> ListAsync()
        {
            try
            {
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
