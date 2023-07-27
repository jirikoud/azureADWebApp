using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System;
using AzureAADSource.Models;

namespace AzureAADSource.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ClaimsController : ControllerBase
    {
        private string _version;

        public ClaimsController() {
            _version = "1.0";
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
            var mappings = new List<ClinicMapping>()
            {
                new ClinicMapping(){ FacilityId = 1, PatientId = 123456 },
                new ClinicMapping(){ FacilityId = 11, PatientId = 23456 },
                new ClinicMapping(){ FacilityId = 21, PatientId = 3456 },
            };

            var serialized = JsonSerializer.Serialize(mappings);

            return new FlowTokenEnrichmentLicenseResponse { Clinics = serialized, Version = _version };
        }
    }
}
