using ieHRMS.Models;
using ieRecruitment.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Plugins;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ieRecruitment.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        
        public HomeController(ILogger<HomeController> logger,IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Login()
        {
         
            HttpContext.Session.SetInt32("CompanyId", 1);
            HttpContext.Session.SetInt32("PostId", 32);
            HttpContext.Session.SetInt32("LocationId", 2);
            HttpContext.Session.SetInt32("JobId", 45);
            var token = HttpContext.Session.GetString("accessToken");
            var tenantUrl = HttpContext.Session.GetString("TenantUrl");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(tenantUrl))
            {
                return Redirect("/" + tenantUrl + "/FromLanding");
            }

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> SignIn(string identifier, string password)
        {
            SignUp _login = new SignUp();
            // Email pattern
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            // Mobile number pattern (10 digits only, adjust if needed)
            string mobilePattern = @"^\d{10}$";

            bool isEmail = Regex.IsMatch(identifier, emailPattern);
            bool isMobile = Regex.IsMatch(identifier, mobilePattern);

            if (!isEmail && !isMobile)
            {
                return Json(new { success = false, message = "Invalid email or mobile number format." });
            }
            if (isEmail)
            {
                _login.Email = identifier;
            }
            if (isMobile)
            {
                _login.Mobile = identifier;
            }
            _login.Password= password;
            _login.loginDetailsModel.CompanyId = HttpContext.Session.GetInt32("CompanyId");
            _login.loginDetailsModel.PostId= HttpContext.Session.GetInt32("PostId"); 
            _login.loginDetailsModel.LocationId= HttpContext.Session.GetInt32("LocationId");
            _login.loginDetailsModel.JobId= HttpContext.Session.GetInt32("JobId");
            _login.tenantSettings.ApplicationCode = _configuration["AppSettings:ApplicationKey"];

            var authApiUrl = _configuration["ApiSettings:AuthenticationURL"] + "Candidate-SignIn";
            try
            {
                using var httpClient = new HttpClient();
                var jsonData = JsonConvert.SerializeObject(_login);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(authApiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Json(new { success = false, message = "Authentication failed", details = responseBody });

                // Deserialize the response
                var json = JObject.Parse(responseBody);

                if (json["success"]?.Value<bool>() == true)
                {
                    var accessToken = json["accessToken"]?.ToString();
                    var user = json["user"];
                    var tenantUrl = user?["tenantUrl"]?.ToString();

                    // Save in session
                    HttpContext.Session.SetString("accessToken", accessToken ?? "");
                    HttpContext.Session.SetString("TenantUrl", tenantUrl ?? "");

                    // Redirect
                    if (string.IsNullOrWhiteSpace(tenantUrl))
                        return Json(new { success = false, message = "Tenant URL missing." });

                    var redirectUrl = $"/{tenantUrl}/FromLanding";
                    return Json(new { success = true, redirectUrl });
                }

                return Json(new { success = false, message = "Invalid credentials." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Exception occurred", error = ex.Message });
            }
        }
        public IActionResult SignUp()
        {
            
            return View();

        }
    }
}
