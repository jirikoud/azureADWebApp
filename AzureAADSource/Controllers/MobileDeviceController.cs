using AzureAADSource.Infrastructure;
using AzureAADSource.Models.DatabaseModels;
using AzureAADSource.Models.MobileDevices;
using Microsoft.AspNetCore.Mvc;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MobileDeviceController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DbContext _dbContext;

        public MobileDeviceController(ILogger<AppointmentController> logger, DbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        [Route("list")]
        public async Task<List<MobileDevice>> GetListAsync()
        {
            try
            {
                var mobileDevices = await _dbContext.ListMobileDevicesAsync();
                return mobileDevices;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "List");
                throw;
            }
        }

        [HttpGet]
        [Route("detail/{id}")]
        public async Task<MobileDevice> GetDetailAsync(string id)
        {
            try
            {
                var guidId = Guid.Parse(id);
                var mobileDevice = await _dbContext.FindMobileDevice(guidId);
                return mobileDevice;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Detail({id})", id);
                throw;
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<MobileDevice> PostCreateAsync()
        {
            try
            {
                var newItem = new MobileDevice()
                {
                    Id = Guid.NewGuid(),
                    PartitionId = "1",
                    HardwareDevice = "iPhone10,6",
                    OperatingSystem = "iOS 16.1.1",
                    AppVersion = "1.18.0 (17)",
                    DeviceInfo = null,
                    PushToken = "c_Y3lo7whUTbnR7Xq1b0Oo:APA91bHXQvXl3VSFTPkm9rWc1piR8AW441vSQc1QVCNktUSbPSn4FnKcWV5AvFIdaI3_neF3lWrm90YUIw05GSbx6MNi_11A0HZxtU-4L7lmp-l8xo1KHOAbmA6vyT7BxxDVMb-k7dgx",
                    Username = "koudelka@cyberfox.cz",
                    LastAction = DateTime.Now,
                    Language = "cs",
                    Created = DateTime.Now,
                    DebugUsername = "JK",
                };
                var mobileDevice = await _dbContext.CreateMobileDeviceAsync(newItem);
                return mobileDevice;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Create");
                throw;
            }
        }

        [HttpPost]
        [Route("update")]
        public async Task<bool> PostUpdateAsync(UpdateModel model)
        {
            try
            {
                var guidId = Guid.Parse(model.Id);
                var mobileDevice = await _dbContext.FindMobileDevice(guidId);
                mobileDevice.LastAction = DateTime.Now;
                mobileDevice.DeviceInfo = model.DeviceInfo;
                var isSuccess = await _dbContext.UpdateMobileDevice(mobileDevice);
                return isSuccess;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Update({id}, {deviceInfo})", model.Id, model.DeviceInfo);
                throw;
            }
        }

    }
}
