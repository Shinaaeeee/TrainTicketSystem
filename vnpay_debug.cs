// Chạy: dotnet script vnpay_debug.cs
// Hoặc copy vào một Console App và chạy

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

// ---- Parameters từ URL bị lỗi ----
var hashSecret = "50TKHN5WELUL9XR9VHT8S73AVCECRV3F";

// Lấy CHÍNH XÁC từ URL được gửi đi (decode URL-encoded values)
var rawParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
{
    ["vnp_Amount"]     = "10000000",
    ["vnp_Command"]    = "pay",
    ["vnp_CreateDate"] = "20260315151348",
    ["vnp_CurrCode"]   = "VND",
    ["vnp_ExpireDate"] = "20260315152848",
    ["vnp_IpAddr"]     = "127.0.0.1",
    ["vnp_Locale"]     = "vn",
    ["vnp_OrderInfo"]  = "BookingId 11",          // raw (không encode)
    ["vnp_OrderType"]  = "other",
    ["vnp_ReturnUrl"]  = "http://localhost:5103/Payment/VnpayReturn",  // raw
    ["vnp_TmnCode"]    = "HPDO4VEN",
    ["vnp_TxnRef"]     = "11_20260315151348",
    ["vnp_Version"]    = "2.1.0",
};

// Build raw string
var sb = new StringBuilder();
foreach (var kv in rawParams)
{
    if (!string.IsNullOrEmpty(kv.Value))
    {
        if (sb.Length > 0) sb.Append('&');
        sb.Append(kv.Key);
        sb.Append('=');
        sb.Append(kv.Value);
    }
}
var rawHashStr = sb.ToString();

Console.WriteLine("=== RAW HASH STRING ===");
Console.WriteLine(rawHashStr);
Console.WriteLine();

// Compute HMAC-SHA512
var keyBytes  = Encoding.UTF8.GetBytes(hashSecret);
var dataBytes = Encoding.UTF8.GetBytes(rawHashStr);
using var hmac = new HMACSHA512(keyBytes);
var hash = hmac.ComputeHash(dataBytes);
var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

Console.WriteLine("=== COMPUTED SIGNATURE ===");
Console.WriteLine(signature);
Console.WriteLine();
Console.WriteLine("=== EXPECTED (from URL) ===");
Console.WriteLine("0765dbb433a8a5ef761229b584bd131629bd0f4246ec617a13bcce4895fcf945ca9ee11419bc91cd1a0bb665537a60369b551358f9c5fbd587e9aeb521d80643");
Console.WriteLine();
Console.WriteLine("Match: " + signature.Equals("0765dbb433a8a5ef761229b584bd131629bd0f4246ec617a13bcce4895fcf945ca9ee11419bc91cd1a0bb665537a60369b551358f9c5fbd587e9aeb521d80643", StringComparison.OrdinalIgnoreCase));
