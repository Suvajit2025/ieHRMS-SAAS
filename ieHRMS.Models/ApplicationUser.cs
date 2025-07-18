using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ieHRMS.Models
{
    public class ApplicationUser:IdentityUser 
    {
        public int TenantId { get; set; }
        public long? CandidateId { get; set; }
        public string TenantCode { get; set; }
        public Guid CandidateCode { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
