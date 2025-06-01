using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Shop.Service
{
    public class TranslatorService
    {
        private static readonly string subscriptionKey = "YOUR_SUBSCRIPTION_KEY";
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com";
        private static readonly string location = "southeastasia"; // Thay theo vùng bạn dùng trong Azure

        public async Task<string> TranslateTextAsync(string text, string toLanguage = "es")
        {
            var route = $"/translate?api-version=3.0&to={toLanguage}";
            var uri = endpoint + route;

            var requestBody = new object[] { new { Text = text } };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", location);

                var response = await client.PostAsync(uri, content);
                var result = await response.Content.ReadAsStringAsync();

                // Phân tích JSON để lấy bản dịch
                dynamic jsonResponse = JsonConvert.DeserializeObject(result);
                string translation = jsonResponse[0]["translations"][0]["text"];

                return translation;
            }
        }
    }
}
