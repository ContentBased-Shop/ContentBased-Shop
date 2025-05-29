using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

public class VnPayLibrary
{
    private readonly SortedList<string, string> requestData = new SortedList<string, string>();
    private readonly SortedList<string, string> responseData = new SortedList<string, string>();

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            requestData[key] = value;
        }
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            responseData[key] = value;
        }
    }

    public string GetResponseData(string key)
    {
        responseData.TryGetValue(key, out var value);
        return value;
    }

    public SortedList<string, string> GetRequestData()
    {
        return new SortedList<string, string>(requestData);
    }

    public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
    {
        var query = new StringBuilder();
        foreach (var item in requestData)
        {
            query.Append($"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}&");
        }

        var signData = query.ToString().TrimEnd('&');
        var signHash = HmacSHA512(vnp_HashSecret, signData);
        var fullUrl = $"{baseUrl}?{signData}&vnp_SecureHash={signHash}";

        // Ghi log để debug
        System.Diagnostics.Debug.WriteLine($"Sign Data: {signData}");
        System.Diagnostics.Debug.WriteLine($"Secure Hash: {signHash}");

        return fullUrl;
    }

    public bool ValidateSignature(string vnp_HashSecret)
    {
        var rawData = new StringBuilder();
        foreach (var item in responseData)
        {
            if (item.Key != "vnp_SecureHash" && item.Key != "vnp_SecureHashType")
            {
                rawData.Append($"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}&");
            }
        }

        var checkData = rawData.ToString().TrimEnd('&');
        string receivedHash = GetResponseData("vnp_SecureHash");
        string expectedHash = HmacSHA512(vnp_HashSecret, checkData);

        // Ghi log để debug
        System.Diagnostics.Debug.WriteLine("Response Data:");
        foreach (var item in responseData)
        {
            System.Diagnostics.Debug.WriteLine($"Key: {item.Key}, Value: {item.Value}");
        }
        System.Diagnostics.Debug.WriteLine($"Raw data to sign: {checkData}");
        System.Diagnostics.Debug.WriteLine($"Expected hash: {expectedHash}");
        System.Diagnostics.Debug.WriteLine($"Received hash: {receivedHash}");
        System.Diagnostics.Debug.WriteLine($"Hash match: {string.Equals(receivedHash, expectedHash, StringComparison.OrdinalIgnoreCase)}");

        return string.Equals(receivedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private string HmacSHA512(string key, string inputData)
    {
        using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }
    }
}