using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AzureAADSource.Models;
using AzureAADSource.Infrastructure;

namespace AzureAADSource.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ClaimsController : ControllerBase
    {
        private string _version;
        private readonly DbContext _dbContext;

        public ClaimsController(DbContext dbContext) 
        {
            _version = "1.0";
            _dbContext = dbContext;
        }

        /// <summary>
        /// Získá AADB2C Flow Enrichment pro License.
        /// </summary>
        /// <param name="request">Požadavek z AADB2C flow.</param>
        /// <returns>Odpověď pro AADB2C Flow Enrichment obohacenou o data licencí uživatele.</returns>
        /// <response code="200">Požadavek byl zpracován úspěšně.</response>
        /// <response code="401">Chyba autentizace.</response>
        /// <response code="403">Chyba autorizace.</response>
        [HttpGet]
        [HttpPost]
        public async Task<FlowTokenEnrichmentLicenseResponse> Get(FlowTokenEnrichmentRequest request)
        {
            var patientPairings = await _dbContext.GetPatientPairingsByUsernameAsync(request.ObjectId);

            var mappings = patientPairings.ConvertAll(item => new ClinicMapping() { FacilityId = item.NISId, PatientId = item.PatientId });

            var serialized = JsonSerializer.Serialize(mappings);

            //var serialized = JsonSerializer.Serialize(request);

            return new FlowTokenEnrichmentLicenseResponse { Clinics = serialized, Version = _version };
        }
    }
}
