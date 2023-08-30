using AzureAADSource.Infrastructure;
using AzureAADSource.Models.DatabaseModels;
using AzureAADSource.Models.Patients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DbContext _dbContext;

        public PatientController(ILogger<PatientController> logger, DbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        private string GetPatientHash(string firstname, string lastname, string ident)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                var mixedIdent = $"{firstname};{lastname};{ident}";
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(mixedIdent));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [HttpGet]
        [Route("pairings")]
        public async Task<List<PatientPairing>> GetPairingsAsync(string username)
        {
            try
            {
                var patientPairings = await _dbContext.GetPatientPairingsByUsernameAsync(username);
                return patientPairings;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Pairings({id})", username);
                throw;
            }
        }

        [HttpPost]
        [Route("onboard")]
        public async Task<PatientPairing> PostOnboardAsync(OnboardRequest model)
        {
            var exists = await _dbContext.GetPatientPairingExistsAsync(model.Username, model.PatientId, model.NISId);
            if (exists)
            {
                throw new Exception("Already exists");
            }
            var newItem = new PatientPairing()
            {
                Id = Guid.NewGuid(),
                PartitionId = "1",
                Username = model.Username,
                PatientId = model.PatientId,
                NISId = model.NISId,
                CommunicationKey = model.CommunicationKey,
                PatientIdentHash = GetPatientHash(model.Firstname, model.Lastname, model.Ident),
                Created = DateTime.Now,
            };
            var patientPairing = await _dbContext.CreatePatientPairingAsync(newItem);
            return patientPairing;
        }

        [HttpGet]
        [Route("users")]
        public async Task<List<PatientPairing>> GetUsersAsync(int patientId, int nisId)
        {
            try
            {
                var patientPairing = await _dbContext.GetPatientPairingByIdAndNISAsync(patientId, nisId);
                return patientPairing;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Users({patientId}, {nisId})", patientId, nisId);
                throw;
            }
        }

    }
}
