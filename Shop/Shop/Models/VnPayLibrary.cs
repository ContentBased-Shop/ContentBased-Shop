using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System;

public class VnPayLibrary
{
    private SortedList<string, string> requestData = new SortedList<string, string>();
    private SortedList<string, string> responseData = new SortedList<string, string>();

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            if (!requestData.ContainsKey(key))
            {
                requestData.Add(key, value);
            }
            else
            {
                requestData[key] = value; // Ghi đè nếu đã tồn tại
            }
        }
    }


    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
            responseData.Add(key, value);
    }

    public string GetResponseData(string key)
    {
        responseData.TryGetValue(key, out var value);
        return value;
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

        return fullUrl;
    }

    public bool ValidateSignature(string vnp_HashSecret)
    {
        var rawData = new StringBuilder();
        foreach (var item in responseData)
        {
            if (item.Key != "vnp_SecureHash" && item.Key != "vnp_SecureHashType")
            {
                rawData.Append($"{item.Key}={item.Value}&");
            }
        }

        var checkData = rawData.ToString().TrimEnd('&');
        string receivedHash = GetResponseData("vnp_SecureHash");
        string expectedHash = HmacSHA512(vnp_HashSecret, checkData);
        return receivedHash == expectedHash;
    }

    private string HmacSHA512(string key, string inputData)
    {
        var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
        return BitConverter.ToString(hash).Replace("-", "").ToUpper();
    }
}
