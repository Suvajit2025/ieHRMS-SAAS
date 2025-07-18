using ieHRMS.Core.Config;
using ieHRMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface ITenantContextProvider
    {
        Task<TenantConnectionInfo> GetCurrentTenantConnectionAsync(string? TenanatCode,string? ApplicationCode);
        Task<MailSetting> GetCurrentMailConnectionAsync(string? TenanatCode, string? ApplicationCode,string modelname);
    }
}
