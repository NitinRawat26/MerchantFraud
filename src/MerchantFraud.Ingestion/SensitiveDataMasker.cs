using System.Net;
using System.Net.Sockets;

namespace MerchantFraud.Ingestion;

public static class SensitiveDataMasker
{
    public static string? MaskIpAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (IPAddress.TryParse(value.Trim(), out var ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();
            return $"{bytes[0]}.{bytes[1]}.0.0";
        }

        return "***";
    }

    public static string? MaskCardNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length >= 12 ? digits[^4..] : null;
    }
}
