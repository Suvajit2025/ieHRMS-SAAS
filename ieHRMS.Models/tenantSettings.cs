using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Models
{
    public class TenantSettings
    {
        public int? TenantId { get; set; }
        public string? TenantCode { get; set; }
        public string? TenantName { get; set; }=string.Empty;
        public string? TenantType { get; set; }=string.Empty ;
        public int? ApplicationId { get; set; }
        public string? ApplicationName { get; set; }= string.Empty;
        public string? ApplicationCode { get; set; }
    }
    public class TenantSmtpSettings
    {
        public string SmtpServer { get; set; }
        public bool EnableSSL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string FromEmail { get; set; }
    }
    public class BackgroundQueueSettings
    {
        public int MaxConcurrency { get; set; } = 10;
    }
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
    }
    public class VerificationResultModel
    {
        public int Status { get; set; }     // 1 for success, 0/-1 for failure
        public string Message { get; set; }
    }
    public class TenantConnectionInfo
    {
        public string ConnectionString { get; set; }
        public string Schema { get; set; }
        public int TenantId { get; set; }
        public string TenantCode { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string TenantUrl { get; set; } = string.Empty;
        public int ApplicationId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string ApplicationCode { get; set; }
        public string ApplicationURL { get; set; }

    }
    public class MailSetting : TenantConnectionInfo
    {

        public string ModuleName { get; set; }

        public string FromEmail { get; set; }
        public string Username { get; set; }

        private string _password;
        public string EncodedPassword
        {
            // Gets the Base64-encoded string (use only if saving back to DB)
            get => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_password));

            // Sets the password by decoding from Base64
            set => _password = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        // Exposed decoded password for internal use
        public string Password => _password;

        public string SMTPServer { get; set; }
        public int SMTPPort { get; set; }
        public bool EnableSSL { get; set; }
        public string ApplicationUrl { get; set; }
    }

}
