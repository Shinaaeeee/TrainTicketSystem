using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace TrainTicketSystem.Services;

/// <summary>
/// VNPay payment URL builder and signature verifier.
///
/// Verified against official VNPay C# demo (PayLib.cs):
/// - BuildPaymentUrl: WebUtility.UrlEncode for BOTH key AND value, remove trailing '&amp;', then HMAC
/// - IsValidSignature: use RAW query string up to (not including) "&amp;vnp_SecureHash"
/// </summary>
public class VnpayService
{
    private readonly IConfiguration _config;

    public VnpayService(IConfiguration config) => _config = config;

    // ------------------------------------------------------------------ //
    //  Build payment URL                                                  //
    // ------------------------------------------------------------------ //
    public string BuildPaymentUrl(int bookingId, decimal amount, string orderInfo, string ipAddress)
    {
        var now = DateTime.Now;

        // VNPay sandbox rejects IPv6
        if (ipAddress == "::1" || ipAddress.Contains(':'))
            ipAddress = "127.0.0.1";

        // OrderInfo: no diacritics, no special chars (VNPay spec)
        var safeOrderInfo = $"Thanh toan don hang {bookingId}";

        // Use SortedDictionary for automatic ASCII-ascending sort (same as VNPay server)
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

        // Build encoded query string: WebUtility.UrlEncode(key)=WebUtility.UrlEncode(value)&...
        // This is EXACTLY what PayLib.cs does on GitHub
        var sb = new StringBuilder();
        foreach (var (key, value) in vnpParams)
        {
            if (string.IsNullOrEmpty(value)) continue;
            sb.Append(WebUtility.UrlEncode(key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(value));
            sb.Append('&');
        }

        // queryString has trailing '&', e.g. "vnp_Amount=10000000&vnp_Command=pay&"
        var queryString = sb.ToString();

        // signData = queryString without trailing '&'
        var signData = queryString.TrimEnd('&');

        var signature = HmacSha512(_config["VNPay:HashSecret"]!, signData);

        // Final URL = baseUrl?{queryString}vnp_SecureHash={sig}  (queryString already has trailing &)
        return $"{_config["VNPay:BaseUrl"]}?{queryString}vnp_SecureHash={signature}";
    }

    // ------------------------------------------------------------------ //
    //  Verify signature from VNPay return callback                       //
    //                                                                    //
    //  CRITICAL: use the RAW query string from the URL (not rebuilt),    //
    //  take the substring from after '?' up to (not incl.) &vnp_Secure  //
    // ------------------------------------------------------------------ //
    public bool IsValidSignature(HttpRequest request)
    {
        var rawQs = request.QueryString.Value; // e.g. "?vnp_Amount=...&vnp_SecureHash=abc"
        if (string.IsNullOrEmpty(rawQs)) return false;

        var vnpSecureHash = request.Query["vnp_SecureHash"].ToString();
        if (string.IsNullOrEmpty(vnpSecureHash)) return false;

        // Find position of &vnp_SecureHash in the raw string
        var pos = rawQs.IndexOf("&vnp_SecureHash", StringComparison.Ordinal);
        if (pos < 0) return false;

        // rspRaw = everything after '?' up to (not including) &vnp_SecureHash
        // rawQs starts with '?', so skip index 0
        var rspRaw = rawQs.Substring(1, pos - 1);

        var computedHash = HmacSha512(_config["VNPay:HashSecret"]!, rspRaw);
        return string.Equals(computedHash, vnpSecureHash, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------ //
    //  HMAC-SHA512                                                        //
    // ------------------------------------------------------------------ //
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
