using ieHRMS.Core.Interface;
using Serilog;
using System.Globalization;
using System.Numerics;

namespace ieHRMS.Core.Repository
{
    public class ConversionRepository : IConversion
    {

        public ConversionRepository()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/Conversion_log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        public async Task<string> ConvertAmountToINR(double dblAmount, bool boolUseShortFormat = false)
        {
            try
            {
                string strFormattedAmount = "";
                if (!boolUseShortFormat)
                {
                    strFormattedAmount = dblAmount.ToString("#,0.00", System.Globalization.CultureInfo.CreateSpecificCulture("hi-IN"));
                }
                else
                {
                    string strAmt = "", strAmtPart1 = "", strAmtPart2 = "";
                    double dblAmtPart1 = 0, dblAmtPart2 = 0;

                    if (dblAmount < 1000)
                    {
                        strFormattedAmount = dblAmount.ToString("#,0.00", System.Globalization.CultureInfo.CreateSpecificCulture("hi-IN"));
                    }
                    else if (dblAmount >= 1000 && dblAmount < 100000)
                    {
                        strFormattedAmount = (dblAmount / 1000).ToString("0.##") + "K";
                    }
                    else if (dblAmount >= 100000 && dblAmount < 10000000)
                    {
                        strAmt = dblAmount.ToString();
                        strAmtPart1 = strAmt.Substring(0, strAmt.Length - 5);
                        strAmtPart2 = strAmt.Substring(strAmt.Length - 5, 5);

                        if (double.TryParse(strAmtPart1, out dblAmtPart1) && double.TryParse(strAmtPart2, out dblAmtPart2))
                        {
                            if (dblAmtPart2 > 55999)
                            {
                                dblAmtPart1 += 1;
                            }
                            strFormattedAmount = dblAmtPart1.ToString("#,0", System.Globalization.CultureInfo.CreateSpecificCulture("hi-IN")) + "L";
                        }
                        else
                        {
                            Log.Error("Invalid number format during conversion to lakhs.");
                            throw new FormatException("Invalid number format during conversion to lakhs.");
                        }
                    }
                    else if (dblAmount >= 10000000)
                    {
                        strAmt = dblAmount.ToString();
                        strAmtPart1 = strAmt.Substring(0, strAmt.Length - 7);
                        strAmtPart2 = strAmt.Substring(strAmt.Length - 7, 7);

                        if (double.TryParse(strAmtPart1, out dblAmtPart1) && double.TryParse(strAmtPart2, out dblAmtPart2))
                        {
                            if (dblAmtPart2 > 5599999)
                            {
                                dblAmtPart1 += 1;
                            }
                            strFormattedAmount = dblAmtPart1.ToString("#,0", System.Globalization.CultureInfo.CreateSpecificCulture("hi-IN")) + "C";
                        }
                        else
                        {
                            Log.Error("Invalid number format during conversion to crores.");
                            throw new FormatException("Invalid number format during conversion to crores.");
                        }
                    }
                }
                return await Task.FromResult(strFormattedAmount);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error: " + ex.Message);
                return await Task.FromResult("Error: " + ex.Message);
            }
        }

        public async Task<T> ConvertToNumberAsync<T>(object input) where T : struct, IConvertible
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (input == null || string.IsNullOrWhiteSpace(input.ToString()) || input.ToString() == "{}")
                    {
                        return default; // Returns 0 for numeric types
                    }

                    string inputString = input.ToString().Trim();
                    Type targetType = typeof(T);

                    if (targetType == typeof(short) && short.TryParse(inputString, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortResult))
                    {
                        return (T)Convert.ChangeType(shortResult, targetType);
                    }
                    if (targetType == typeof(int) && int.TryParse(inputString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intResult))
                    {
                        return (T)Convert.ChangeType(intResult, targetType);
                    }
                    if (targetType == typeof(long) && long.TryParse(inputString, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longResult))
                    {
                        return (T)Convert.ChangeType(longResult, targetType);
                    }
                    if (targetType == typeof(BigInteger) && BigInteger.TryParse(inputString, NumberStyles.Integer, CultureInfo.InvariantCulture, out BigInteger bigIntResult))
                    {
                        return (T)Convert.ChangeType(bigIntResult, targetType);
                    }

                    return default; // If parsing fails, return 0
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error: " + ex.Message); // Log error (optional)
                    return default;
                }
            });
        }
        public async Task<T> ConvertToDecimalAsync<T>(object input, int decimalPlaces = 2) where T : struct, IConvertible
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (input == null || string.IsNullOrWhiteSpace(input.ToString()) || input.ToString() == "{}")
                    {
                        return default;
                    }

                    string inputString = input.ToString().Trim();
                    Type targetType = typeof(T);
                    NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;
                    CultureInfo culture = CultureInfo.InvariantCulture;

                    if (targetType == typeof(float) && float.TryParse(inputString, styles, culture, out float floatResult))
                    {
                        floatResult = (float)Math.Round(floatResult, decimalPlaces); // ✅ Ensures 2 or 3 decimal places
                        return (T)Convert.ChangeType(floatResult, targetType);
                    }
                    if (targetType == typeof(double) && double.TryParse(inputString, styles, culture, out double doubleResult))
                    {
                        doubleResult = Math.Round(doubleResult, decimalPlaces); // ✅ Ensures 2 or 3 decimal places
                        return (T)Convert.ChangeType(doubleResult, targetType);
                    }
                    if (targetType == typeof(decimal) && decimal.TryParse(inputString, styles, culture, out decimal decimalResult))
                    {
                        decimalResult = Math.Round(decimalResult, decimalPlaces); // ✅ Ensures 2 or 3 decimal places
                        return (T)Convert.ChangeType(decimalResult, targetType);
                    }

                    return default;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error: " + ex.Message);
                    return default;
                }
            });
        }
        public async Task<string> ConvertToStringAsync(object input, int decimalPlaces = 2)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (input == null || string.IsNullOrWhiteSpace(input.ToString()) || input.ToString() == "{}")
                    {
                        return ""; // Default formatted string
                    }

                    if (decimal.TryParse(input.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out decimal decimalResult))
                    {
                        return decimalResult.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
                    }

                    return ""; // Return default value if parsing fails
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error: " + ex.Message);
                    return "";
                }
            });
        }

        public async Task<string> ConvertTopascalStringAsync(string input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        return ""; // ✅ Return empty string for null or whitespace input
                    }

                    // Normalize spacing and convert to Pascal Case
                    TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
                    string result = string.Join(" ",
                        input.Split(new char[] { ' ', '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(word => textInfo.ToTitleCase(word.ToLower()))
                    );

                    return result;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error: " + ex.Message);
                    return ""; // ✅ Return empty string in case of any errors
                }
            });
        }

        public async Task<string> ConvertToCapitalFirstStringAsync(string input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        return ""; // ✅ Return empty string for null or whitespace input
                    }

                    // Convert to title case (capitalizing the first letter of each word)
                    TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
                    string result = textInfo.ToTitleCase(input.ToLower());

                    return result;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error: " + ex.Message);
                    return ""; // ✅ Return empty string in case of any errors
                }
            });
        }
        public async Task<string> ConvertToAllCapitalStringAsync(string input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        return ""; // ✅ Return empty string for null or whitespace input
                    }

                    return input.ToUpperInvariant(); // ✅ Convert to uppercase using InvariantCulture
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error: " + ex.Message);
                    return ""; // ✅ Return empty string in case of any errors
                }
            });
        }
    }
}
