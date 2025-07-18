using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface ITokenGenerator
    { 
        string GenerateToken(Dictionary<string, object> claimsData, double expiryMinutes, bool isRefreshToken);
        string GenerateEncryptedToken(Dictionary<string, object> payload, double expiryMinutes, bool isRefreshToken);
    }

}
