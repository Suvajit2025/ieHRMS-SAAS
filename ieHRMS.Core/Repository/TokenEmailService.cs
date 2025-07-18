using ieHRMS.Core.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Repository
{
    public class TokenEmailService : ITokenEmailService
    {
        private readonly ITenantContextProvider _tenantContextProvider;
        private readonly IDataService _dataService;
        private readonly ICommonService _commonService;
        private readonly ITokenGenerator _tokenGenerator;
        public TokenEmailService(ITenantContextProvider tenantContextProvider,IDataService dataService,ICommonService commonService, ITokenGenerator tokenGenerator)
        {
            _tenantContextProvider = tenantContextProvider;
            _dataService = dataService;
            _commonService = commonService;
            _tokenGenerator = tokenGenerator;
        }

        public async Task SendTokenEmailAsync(string purpose,Guid candidatecode, string email,string userId,string companyName,string connectionString, string modulename, string? tenanatCode = null, string? applicationCode = null)
        {
            try
            {
                var mailSetting = await _tenantContextProvider.GetCurrentMailConnectionAsync(tenanatCode, applicationCode,modulename);
                if (mailSetting == null) return;

                var templates = await _dataService.GetDataAsync("SP_Get_EmailTemplates", new Dictionary<string, object>
                {
                    { "@TenanatKey", tenanatCode },
                    { "@ApplicationKey", applicationCode },
                    { "@Purpose", purpose }
                }, connectionString);

                if (templates == null || templates.Rows.Count == 0) return;

                var subjectTemplate = templates.Rows[0]["Subject"]?.ToString();
                var bodyTemplate = templates.Rows[0]["Body"]?.ToString();
                string token = $"{Guid.NewGuid()}";
               
                //Generate JWT token And Send The Mail
                var tokenClaims = new Dictionary<string, object>
                {
                    { "candidatecode", candidatecode.ToString() },
                    { "TenanatKey", tenanatCode },
                    { "ApplicationKey", applicationCode },
                    { "Purpose", purpose },
                    { "Expiry", DateTime.Now.AddHours(12).ToString("yyyy-MM-dd HH:mm:ss") }
                };
                 
                // Generate JWT token
                var jwt = _tokenGenerator.GenerateToken(tokenClaims,(60*12),false); //12 Hours

                string link = $"{mailSetting.ApplicationUrl}/verify?token={jwt}";

                var tokens = new Dictionary<string, string>
                {
                    { "CompanyName", companyName },
                    { "Link", link },
                    { "SuppportMailID", mailSetting.FromEmail }
                };

                var result = EmailFormatter.ApplyTemplate(subjectTemplate, bodyTemplate, tokens);

                // Save token
                var saveParams = new Dictionary<string, object>
                {
                    {"@candidatecode", candidatecode.ToString() },
                    {"@TenanatKey", tenanatCode },
                    {"@ApplicationKey", applicationCode },
                    {"@Token", token },
                    {"@Purpose", purpose },
                    {"@Expiry", DateTime.Now.AddHours(12).ToString("yyyy-MM-dd HH:mm:ss") }
                };

                await _dataService.AddAsync("SP_Save_CandidateEmailTokens", saveParams, connectionString); 
                // Send email
                await _commonService.SendEmailAsync(mailSetting.SMTPServer,mailSetting.EnableSSL,mailSetting.Username,mailSetting.Password,mailSetting.SMTPPort,mailSetting.FromEmail,email,result.Subject,result.Body,null,null);
            }
            catch (Exception ex)
            {
                // TODO: Log error with your logger
            }
        }

        private static class EmailFormatter
        {
            public static (string Subject, string Body) ApplyTemplate(string subjectTemplate,string bodyTemplate,Dictionary<string, string> tokens)
            {
                foreach (var token in tokens)
                {
                    subjectTemplate = subjectTemplate.Replace($"{{{{{token.Key}}}}}", token.Value ?? "");
                    bodyTemplate = bodyTemplate.Replace($"{{{{{token.Key}}}}}", token.Value ?? "");
                }

                return (subjectTemplate, bodyTemplate);
            }
        }
    }

}
