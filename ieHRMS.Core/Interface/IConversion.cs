using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface IConversion
    {
        Task<string> ConvertAmountToINR(double dblAmount, bool boolUseShortFormat = false);
        Task<T> ConvertToNumberAsync<T>(object input) where T : struct, IConvertible;
        Task<T> ConvertToDecimalAsync<T>(object input, int decimalPlaces = 2) where T : struct, IConvertible;
        Task<string> ConvertToStringAsync(object input, int decimalPlaces = 2);
        Task<string> ConvertTopascalStringAsync(string input);
        Task<string> ConvertToCapitalFirstStringAsync(string input);
        Task<string> ConvertToAllCapitalStringAsync(string input);
        

    }
}
