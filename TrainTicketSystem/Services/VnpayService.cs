using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace TrainTicketSystem.Services;

public class VnpayService
{
    private readonly IConfiguration _config;

    public VnpayService(IConfiguration config) => _config = config;

    public string BuildPaymentUrl(int bookingId, decimal amount, string orderInfo, string ipAddress)
    {
        var now = DateTime.Now;

        if (ipAddress == "::1" || ipAddress.Contains(':'))
            ipAddress = "127.0.0.1";

        var safeOrderInfo = $"Thanh toan don hang {bookingId}";

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"]     = ((long)(amount * 100)).ToString(),
            ["vnp_Command"]    = _config["VNPay:Command"]!,
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"]   = _config["VNPay:CurrCode"]!,
            ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
            ["vnp_IpAddr"]     = ipAddress,
            ["vnp_Locale"]     = _config["VNPay:Locale"]!,
            ["vnp_OrderInfo"]  = safeOrderInfo,
            ["vnp_OrderType"]  = "other",
            ["vnp_ReturnUrl"]  = _config["VNPay:ReturnUrl"]!,
            ["vnp_TmnCode"]    = _config["VNPay:TmnCode"]!,
            ["vnp_TxnRef"]     = $"{bookingId}_{now:yyyyMMddHHmmss}",
            ["vnp_Version"]    = _config["VNPay:Version"]!,
        };

        var sb = new StringBuilder();
        foreach (var (key, value) in vnpParams)
        {
            if (string.IsNullOrEmpty(value)) continue;
            sb.Append(WebUtility.UrlEncode(key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(value));
            sb.Append('&');
        }

        var queryString = sb.ToString();

        var signData = queryString.TrimEnd('&');

        var signature = HmacSha512(_config["VNPay:HashSecret"]!, signData);

        return $"{_config["VNPay:BaseUrl"]}?{queryString}vnp_SecureHash={signature}";
    }

    public bool IsValidSignature(HttpRequest request)
    {
        var rawQs = request.QueryString.Value;
        if (string.IsNullOrEmpty(rawQs)) return false;

        var vnpSecureHash = request.Query["vnp_SecureHash"].ToString();
        if (string.IsNullOrEmpty(vnpSecureHash)) return false;

        var pos = rawQs.IndexOf("&vnp_SecureHash", StringComparison.Ordinal);
        if (pos < 0) return false;

        var rspRaw = rawQs.Substring(1, pos - 1);

        var computedHash = HmacSha512(_config["VNPay:HashSecret"]!, rspRaw);
        return string.Equals(computedHash, vnpSecureHash, StringComparison.OrdinalIgnoreCase);
    }

    public static string HmacSha512(string secret, string data)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        return hmac.ComputeHash(dataBytes)
                   .Aggregate(new StringBuilder(), (sb, b) => sb.Append(b.ToString("x2")))
                   .ToString();
    }
}
