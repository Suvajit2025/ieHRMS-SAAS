using ieHRMS.Core.Config;
using ieHRMS.Core.Interface;
using ieHRMS.Models;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Repository
{
    public class TenantContextProvider : ITenantContextProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantResolverService _resolver;

        public TenantContextProvider(IConfiguration configuration, ITenantResolverService resolver)
        {
            _configuration = configuration;
            _resolver = resolver;
        }

        public async Task<TenantConnectionInfo> GetCurrentTenantConnectionAsync(string? tenantCode = null, string? applicationCode = null)
        {
            var context = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(tenantCode))
                context["TenantKey"] = tenantCode;

            if (!string.IsNullOrEmpty(applicationCode))
                context["ApplicationKey"] = applicationCode;

            return await _resolver.ResolveConnectionAsync(context);
        }


        public async Task<MailSetting> GetCurrentMailConnectionAsync(string? tenantCode = null, string? applicationCode = null, string? modelName=null)
        {
          
            var context = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(tenantCode))
                context["TenantKey"] = tenantCode;

            if (!string.IsNullOrEmpty(applicationCode))
                context["ApplicationKey"] = applicationCode;
            if (!string.IsNullOrEmpty(modelName))
                context["ModelName"] = modelName;
             
            return await _resolver.ResolveMailConnectionAsync(context);
        }
    }

}
