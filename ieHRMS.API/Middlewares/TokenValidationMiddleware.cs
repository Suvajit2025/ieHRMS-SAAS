using Jose;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace ieHRMS.API.Middlewares
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _jwtSecret;

        public TokenValidationMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _jwtSecret = config["JwtSettings:SecretKey"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;

            // ✅ Skip validation for login/registration endpoints
            if (path != null && path.StartsWith("/Auth", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var appCode = context.Request.Headers["Application-Code"].FirstOrDefault(); // ✅ Step 1: Extract Application-Code

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var key = Encoding.UTF8.GetBytes(_jwtSecret);

                    // 🔐 Decrypt the JWE token
                    var payloadJson = JWT.Decode(token, key, JweAlgorithm.DIR, JweEncryption.A256GCM);

                    // ✅ Parse the payload (optional: you can map to ClaimsPrincipal if needed)
                    var payloadDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);

                    var claims = payloadDict
                                .Select(kv => new Claim(kv.Key, kv.Value?.ToString() ?? string.Empty))
                                .ToList(); // ✅ Convert to List so you can add to it

                    if (!string.IsNullOrEmpty(appCode))
                    {
                        claims.Add(new Claim("applicationcode", appCode)); // ✅ Works now
                    }

                    var identity = new ClaimsIdentity(claims, "jwt");
                    context.User = new ClaimsPrincipal(identity);

                    // Continue
                    await _next(context);
                    return;
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized: Invalid or expired token.\n" + ex.Message);
                    return;
                }
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Token missing");
                return;
            }
        }
    }
}
