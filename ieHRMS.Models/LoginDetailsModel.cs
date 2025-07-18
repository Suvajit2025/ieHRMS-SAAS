using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Models
{
    public class LoginDetailsModel
    {
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int? LocationId { get; set; }
        public string? LocationName { get; set; }
        public int? PostId { get; set; }
        public string? PostName { get; set; }
        public int? JobId { get; set; }
        public string? JobCode { get; set; } 

    }
    public class SignUp 
    {
        public string? firstName { get; set; }
        public string? middleName { get; set; }
        public string? lastName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Password { get; set; }
        public string? Username { get; set; }
        public LoginDetailsModel loginDetailsModel { get; set; } = new();
        public TenantSettings tenantSettings { get; set; }=new();
    }
    public class TokenVerifyRequest:TenantSettings
    {
        public string Token { get; set; }
        public string Purpose { get; set; }
    }
    public class LoginResponse
    {
        public bool Success { get; set; }
        public UserDetails User { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class UserDetails
    {
        public string Email { get; set; }
        public int TenantId { get; set; }
        public string? TenantCode { get; set; }
        public string TenantUrl { get; set; }
        public string CandidateCode { get; set; }
        public int CandidateId { get; set; }
        public string ApplicationId { get; set; }
        public string RegistrationId { get; set; }
        public string RegistrationNo { get; set; }
    }

}
