using ieHRMS.Core.Config;
using ieHRMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface ITenantResolverService
    {
        Task<TenantConnectionInfo> ResolveConnectionAsync(Dictionary<string, string> context);
        Task<MailSetting> ResolveMailConnectionAsync(Dictionary<string, string> context);
    }
}
