using ieHRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ieRecruitment.UI.Controllers
{
    public class VerifyController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public VerifyController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }
        public async Task<IActionResult> Index(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return View("VerificationError", "Invalid verification link.");

            try
            {
                // Read JWT token without validating signature
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

                string tenantCode = claims.TryGetValue("TenanatKey", out var tCode) ? tCode : null;
                string applicationCode = claims.TryGetValue("ApplicationKey", out var aCode) ? aCode : null;
                string extractedPurpose = claims.TryGetValue("Purpose", out var purpose) ? purpose : null;

                // OPTIONAL: Validate expiration manually
                if (claims.TryGetValue("Expiry", out var expiryClaim) && DateTime.TryParse(expiryClaim, out var expiryTime))
                {
                    if (expiryTime < DateTime.Now)
                        return View(new VerificationResultModel { Status = 0, Message = "Link is Expired." });
                }

                // Prepare POST request to backend API
                var client = _httpClientFactory.CreateClient();
                var apiUrl = _config["ApiSettings:AuthenticationURL"] + "validation";

                var payload = new
                {
                    Token = token,
                    Purpose = extractedPurpose,
                    TenantCode = tenantCode,
                    ApplicationCode = applicationCode
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var apiResult = JsonConvert.DeserializeObject<VerificationResultModel>(json);
                    return View(apiResult);
                }
                else
                {
                    return View(new VerificationResultModel { Status = 0, Message = "Server error during verification." });
                }
            }
            catch (Exception ex)
            {
                return View(new VerificationResultModel { Status = -1, Message = $"Invalid token: {ex.Message}" });
            }
        }

    }
}