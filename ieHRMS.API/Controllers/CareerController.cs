using ieHRMS.Core.Interface;
using ieHRMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ieHRMS.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CareerController : ControllerBase
    {
        private readonly ITenantContextProvider _tenantContextProvider;
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ITokenEmailService _tokenEmailService;
        private readonly ITokenGenerator _tokenGenerator;

        public CareerController(ITenantContextProvider tenantContextProvider, IDataService dataService, IConfiguration configuration, ICommonService commonService, IBackgroundTaskQueue taskQueue, ITokenEmailService tokenEmailService, ITokenGenerator tokenGenerator)
        {
            _tenantContextProvider = tenantContextProvider;
            _dataService = dataService;
            _configuration = configuration;
            _commonService = commonService;
            _taskQueue = taskQueue;
            _tokenEmailService = tokenEmailService;
            _tokenGenerator = tokenGenerator;
        }
        [HttpGet("Get-Candidate")]
        public async Task<IActionResult> CandidateDetails()
        {
            // Access claims using extension methods
            var candidateId = User.FindFirst("candidateId")?.Value;
            var candidateCode = User.FindFirst("candidateCode")?.Value;
            var registrationNo = User.FindFirst("registrationNo")?.Value;
            var email = User.FindFirst("email")?.Value;
            var TenanatId = User.FindFirst("TenanatId")?.Value;
            var ApplicationId = User.FindFirst("ApplicationId")?.Value;
            var TenanatCode = User.FindFirst("TenanatCode")?.Value;
            var applicationcode = User.FindFirst("applicationcode").Value;
            // Step 1: Resolve tenant-specific connection
            var connInfo = await _tenantContextProvider.GetCurrentTenantConnectionAsync(TenanatCode, applicationcode);

            if (string.IsNullOrEmpty(candidateId))
                return Unauthorized(new { message = "Invalid token " });

            // You can now use this data to query DB or return info
            var result = new
            {
                CandidateName = "John Doe",
                CandidateId = candidateId,
                CandidateCode = candidateCode,
                Email = email,
                RegistrationNo = registrationNo
            };

            return Ok(result);
        }
    }
}
