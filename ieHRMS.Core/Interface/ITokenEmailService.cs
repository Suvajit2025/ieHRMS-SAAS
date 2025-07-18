using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface ITokenEmailService
    {
        Task SendTokenEmailAsync(string purpose,Guid candidatecode,string email,string userId,string companyName,string connectionString,string MailModule, string? tenanatCode, string? applicationCode);
    }

}
