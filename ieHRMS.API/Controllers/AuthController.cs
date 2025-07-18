using ieHRMS.Core.Interface;
using ieHRMS.Core.Repository;
using ieHRMS.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ieHRMS.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ITenantContextProvider _tenantContextProvider;
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ITokenEmailService _tokenEmailService;
        private readonly ITokenGenerator _tokenGenerator;

        public AuthController(ITenantContextProvider tenantContextProvider, IDataService dataService, IConfiguration configuration, ICommonService commonService, IBackgroundTaskQueue taskQueue, ITokenEmailService tokenEmailService, ITokenGenerator tokenGenerator)
        {
            _tenantContextProvider = tenantContextProvider;
            _dataService = dataService;
            _configuration = configuration;
            _commonService = commonService;
            _taskQueue = taskQueue;
            _tokenEmailService = tokenEmailService;
            _tokenGenerator = tokenGenerator;
        }
        [HttpPost("Candidate-SignUp")]
        public async Task<IActionResult> SignUp([FromBody] SignUp signUp)
        {
            try
            {
                // Step 1: Resolve tenant-specific connection
                var connInfo = await _tenantContextProvider.GetCurrentTenantConnectionAsync(signUp.tenantSettings.TenantCode, signUp.tenantSettings.ApplicationCode);

                // var tenantId = ;
                // Step 2: Dynamically build DbContextOptions
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer(connInfo.ConnectionString);

                // Step 3: Create DbContext manually
                using var dbContext = new ApplicationDbContext(optionsBuilder.Options);

                // Step 4: Manually build UserManager
                var userStore = new UserStore<ApplicationUser>(dbContext);

                var userManager = new UserManager<ApplicationUser>(
                    userStore,
                    optionsAccessor: null,
                    passwordHasher: new PasswordHasher<ApplicationUser>(),
                    userValidators: new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() },
                    passwordValidators: new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() },
                    keyNormalizer: new UpperInvariantLookupNormalizer(),
                    errors: new IdentityErrorDescriber(),
                    services: null,
                    logger: new Logger<UserManager<ApplicationUser>>(new LoggerFactory())
                );

                // Step 5: Check if user exists for the same tenant
                var existingUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == signUp.Email && u.TenantId == connInfo.TenantId);

                if (existingUser != null)
                {
                    return Conflict(new { message = "already registered" });
                }
                // Step 6: Validate the sign-up model
                var parameters = new Dictionary<string, object>
                {
                    {"@TenantId", connInfo.TenantId},
                    {"@ApplicationId",connInfo.ApplicationId},
                    {"@TenanatCode",connInfo.TenantCode },
                    {"@FirstName",(signUp.firstName?? "").ToUpper() },
                    {"@MiddleName",(signUp.middleName?? "").ToUpper() },
                    {"@LastName", (signUp.lastName?? "").ToUpper() },
                    {"@CompanyId",signUp.loginDetailsModel.CompanyId } ,
                    {"@PostId",signUp.loginDetailsModel.PostId },
                    {"@LocationId",signUp.loginDetailsModel.LocationId },
                    {"@JobId",signUp.loginDetailsModel.JobId },
                    {"@Email",signUp.Email },
                    {"@CompanyName",signUp.loginDetailsModel.CompanyName},
                    {"@PostName",signUp.loginDetailsModel.PostName },
                    {"@JobCode",signUp.loginDetailsModel.JobCode },
                    {"@LocationName",signUp.loginDetailsModel.LocationName }
                };

                // Step 6: Call SP_CandidateRegister
                var resultObj = await _dataService.AddTableAsync("SP_CandidateRegister", parameters, connInfo.ConnectionString);

                // Cast object to DataTable
                var resultTable = resultObj as DataTable;

                // Check if SP succeeded by verifying result
                if (resultTable == null || resultTable.Rows.Count == 0 || string.IsNullOrEmpty(resultTable.Rows[0]["CandidateCode"]?.ToString()))
                {
                    return BadRequest(new { message = "Candidate registration failed. No user created." });
                }

                // Extract CandidateCode from SP result
                Guid candidateCode = Guid.Parse(resultTable.Rows[0]["CandidateCode"].ToString());

                long? candidateId = null;

                if (long.TryParse(resultTable.Rows[0]["CandidateId"]?.ToString(), out var parsedCandidateId))
                {
                    candidateId = parsedCandidateId;
                }


                // Step 7: Create Identity user
                var user = new ApplicationUser
                {
                    UserName = signUp.Email,
                    Email = signUp.Email,
                    PhoneNumber = signUp.Mobile,
                    TenantId = connInfo.TenantId,
                    TenantCode = signUp.tenantSettings.TenantCode,
                    CandidateId = candidateId,
                    CandidateCode = candidateCode
                };

                var result = await userManager.CreateAsync(user, signUp.Password);

                // If creation failed, return immediately with errors
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { message = "User creation failed", errors });
                }

                await _taskQueue.QueueBackgroundWorkItem(async cancellationToken =>
                {
                    try
                    {
                        await _tokenEmailService.SendTokenEmailAsync("VerifyMail", candidateCode, signUp.Email, user.Id, signUp.loginDetailsModel.CompanyName, connInfo.ConnectionString, "Recruitment_Application", signUp.tenantSettings.TenantCode, signUp.tenantSettings.ApplicationCode );
                    }
                    catch (Exception ex)
                    {

                    }

                });
                //After successful user creation, Send Mail For Mail Validation.

                return Ok(new { success = true, userId = user.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("validation")]
        public async Task<IActionResult> VerifyEmailToken([FromBody] TokenVerifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Purpose))
                return BadRequest(new { status = 0, message = "Invalid request" });

            try
            {
                // Extract data from JWT
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(request.Token);
                var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

                string tenantCode = claims["TenanatKey"];
                string applicationCode = claims["ApplicationKey"];
                string candidateCode = claims["candidatecode"];
                string purpose = claims["Purpose"];

                // Optional expiry check
                if (claims.TryGetValue("Expiry", out var expiryStr) && DateTime.TryParse(expiryStr, out var expiry))
                {
                    if (expiry < DateTime.Now)
                        return Ok(new { status = 0, message = "Token expired." });
                }

                var connInfo = await _tenantContextProvider.GetCurrentTenantConnectionAsync(tenantCode, applicationCode);

                var spParams = new Dictionary<string, object>
                {
                    { "@CandidateCode", candidateCode },
                    { "@tenantCode", tenantCode },
                    { "@applicationCode", applicationCode },
                    { "@Purpose", purpose }
                };

                var resultTable = await _dataService.GetDataAsync("SP_Verify_CandidateEmailTokens", spParams, connInfo.ConnectionString);
                int status = 0;
                string message = "No data found.";

                if (resultTable != null && resultTable.Rows.Count > 0)
                {
                    var row = resultTable.Rows[0];
                    status = Convert.ToInt32(row["Status"]);
                    message = row["Message"].ToString();
                }

                return Ok(new { status, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = -1, message = ex.Message });
            }
        }

        [HttpPost("Candidate-SignIn")]
        public async Task<IActionResult>Login([FromBody] SignUp SignIn)
        {
            try
            {
                // Step 1: Resolve tenant-specific connection
                var connInfo = await _tenantContextProvider.GetCurrentTenantConnectionAsync(SignIn.tenantSettings.TenantCode, SignIn.tenantSettings.ApplicationCode);
                 
                // Step 2: Build DbContext
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer(connInfo.ConnectionString);
                using var dbContext = new ApplicationDbContext(optionsBuilder.Options);

                // Step 3: Setup UserManager
                var userStore = new UserStore<ApplicationUser>(dbContext);
                var userManager = new UserManager<ApplicationUser>(
                    userStore,
                    null,
                    new PasswordHasher<ApplicationUser>(),
                    new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() },
                    new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() },
                    new UpperInvariantLookupNormalizer(),
                    new IdentityErrorDescriber(),
                    null,
                    new Logger<UserManager<ApplicationUser>>(new LoggerFactory())
                );

                // Step 4: Find user by email & tenant
                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == SignIn.Email);

                if (user == null)
                    return Unauthorized(new { message = "Invalid email or tenant" });

                if (!user.EmailConfirmed)
                    return BadRequest(new { message = "Email is not verified" });

                // Step 5: Check password
                var result = await userManager.CheckPasswordAsync(user, SignIn.Password);
                if (!result)
                    return Unauthorized(new { message = "Incorrect password" });

                // Step 6: Prepare user details
                var userDetails = new
                {
                    user.Id,
                    user.Email,
                    user.PhoneNumber,
                    user.TenantId,
                    user.TenantCode,
                    user.CandidateId,
                    user.CandidateCode
                };

                var parameters = new Dictionary<string, object>
                {
                    {"@CandidateCode", userDetails.CandidateCode} 
                };

                DataTable Result = await _dataService.GetDataAsync("Sp_Get_CandidateRegistration", parameters, connInfo.ConnectionString);

                if (result == null)
                    return BadRequest(new { message = "Candidate registration details not found." });

                var row = Result?.Rows[0];

                // Return user info to UI
                var returnDetails = new
                {
                    user.Email,
                    user.TenantId,
                    user.TenantCode,
                    user.CandidateCode,
                    user.CandidateId,
                    ApplicationId = row["ApplicationId"]?.ToString(),
                    RegistrationId = row["RegistrationId"]?.ToString(),
                    RegistrationNo = row["RegistrationNo"]?.ToString(),
                    TenantUrl=connInfo.TenantUrl
                };

                // Prepare claims for Access Token
                var claimsData = new Dictionary<string, object>
                {
                    { "userId", user.Id },
                    { "email", user.Email },
                    { "tenantId", user.TenantId },
                    { "tenantCode", user.TenantCode },
                    { "candidateCode", user.CandidateCode },
                    { "candidateId", user.CandidateId },
                    { "applicationId", row["ApplicationId"]?.ToString() },
                    { "registrationId", row["RegistrationId"]?.ToString() },
                    { "registrationNo", row["RegistrationNo"]?.ToString() }
                };

                // 4. Generate Tokens
                var accessToken = _tokenGenerator.GenerateEncryptedToken(claimsData, 30, false); // 30 min
                var refreshClaims = new Dictionary<string, object>(claimsData)
                {
                    { "refresh_guid", Guid.NewGuid().ToString() }
                };

                var refreshToken = _tokenGenerator.GenerateEncryptedToken(refreshClaims,(60 * 24 * 30), true); // 30 days

                // Create ClaimsIdentity
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("tenantId", user.TenantId.ToString()),
                    new Claim("tenantCode", user.TenantCode),
                    new Claim("candidateId", user.CandidateId.ToString()),
                    new Claim("candidateCode", user.CandidateCode.ToString()),
                    new Claim("applicationId", row["ApplicationId"]?.ToString() ?? ""),
                    new Claim("registrationId", row["RegistrationId"]?.ToString() ?? ""),
                    new Claim("registrationNo", row["RegistrationNo"]?.ToString() ?? "")
                }, CookieAuthenticationDefaults.AuthenticationScheme);

                // Create principal and sign in
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // Use Antiforgery service to generate and store CSRF token
                var antiforgery = HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
                var tokens = antiforgery.GetAndStoreTokens(HttpContext);


                return Ok(new {success = true,user = returnDetails,accessToken,refreshToken, csrfToken = tokens.RequestToken });
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, new { message = ex.Message }); 
            }
            
        }
    }
}
