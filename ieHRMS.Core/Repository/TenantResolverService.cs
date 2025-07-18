using Microsoft.Extensions.Configuration;
using ieHRMS.Core.Interface; 
using ieHRMS.Core.DataAccess; 
using ieHRMS.Core.Config;
using ieHRMS.Models;

namespace ieHRMS.Core.Repository
{
    public class TenantResolverService : ITenantResolverService
    {
        private readonly IConfiguration _configuration;
        private readonly AdoDataAccess _ado;

        public TenantResolverService(IConfiguration configuration)
        {
            _configuration = configuration;
            string defaultConn = configuration.GetConnectionString("Default"); // default tenant mapping DB
            _ado = new AdoDataAccess(defaultConn);
        }
         
        public async Task<TenantConnectionInfo> ResolveConnectionAsync(Dictionary<string, string> context)
        {
            var parameters = context.ToDictionary(kvp => "@" + kvp.Key, kvp => (object)kvp.Value);

            var dt = await _ado.Dt_ProcessAsync("Proc_GetTenantConnection",parameters.Keys.ToArray(),parameters.Values.ToArray());

            if (dt.Rows.Count == 0)
                throw new Exception("Tenant connection not found.");

            return new TenantConnectionInfo
            {
                ConnectionString = dt.Rows[0]["ConnectionString"].ToString(),
                Schema = dt.Rows[0]["Schema"].ToString(),
                TenantId = Convert.ToInt32(dt.Rows[0]["TenantId"]),
                ApplicationId = Convert.ToInt32(dt.Rows[0]["ApplicationId"]),
                TenantName = dt.Rows[0]["TenantName"].ToString(),
                TenantUrl = dt.Rows[0]["TenantUrl"].ToString(),
                TenantCode = dt.Rows[0]["TenantCode"].ToString(),
                ApplicationName = dt.Rows[0]["ApplicationName"].ToString(),
                ApplicationCode = dt.Rows[0]["ApplicationCode"].ToString(),
                ApplicationURL = dt.Rows[0]["ApplicationURL"].ToString()

            };
        }
        public async Task<MailSetting> ResolveMailConnectionAsync(Dictionary<string, string> context)
        {
            var parameters = context.ToDictionary(kvp => "@" + kvp.Key, kvp => (object)kvp.Value);

            var dt = await _ado.Dt_ProcessAsync("Proc_GetMailConnection", parameters.Keys.ToArray(), parameters.Values.ToArray());

            if (dt.Rows.Count == 0)
                throw new Exception("Mail SetUp For Model not found.");

            return new MailSetting
            {
                TenantId = Convert.ToInt32(dt.Rows[0]["TenantId"]),
                ApplicationId = Convert.ToInt32(dt.Rows[0]["ApplicationId"]),
                TenantCode = dt.Rows[0]["TenantCode"].ToString(),
                ApplicationCode = dt.Rows[0]["ApplicationCode"].ToString(),
                FromEmail = dt.Rows[0]["FromEmail"].ToString().Trim(),
                Username = dt.Rows[0]["Username"].ToString().Trim(),
                EncodedPassword = dt.Rows[0]["Password"].ToString().Trim(),
                SMTPServer = dt.Rows[0]["SMTPServer"].ToString().Trim(),
                SMTPPort = Convert.ToInt32(dt.Rows[0]["SMTPPort"]),
                EnableSSL = Convert.ToBoolean(dt.Rows[0]["EnableSSL"]) ,
                ApplicationUrl = dt.Rows[0]["ApplicationUrl"].ToString().Trim()
            };
        }
    }
}
