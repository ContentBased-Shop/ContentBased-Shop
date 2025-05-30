using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using Shop.Models;

namespace Shop.Controllers
{
    public class NewsController : Controller
    {
        // Response từ NewsAPI
        public class NewsApiResponse
        {
            public string Status { get; set; }
            public int TotalResults { get; set; }
            public List<NewsModel> Articles { get; set; }
        }

        // URL sử dụng endpoint everything, cho phép tìm kiếm theo từ khóa
        private readonly string _apiUrl = "https://newsapi.org/v2/everything?q={0}&language=vi&pageSize=4&apiKey={1}";


        private async Task<List<NewsModel>> FetchNewsArticlesAsync(string keyword)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var articles = new List<NewsModel>();
            string encodedKeyword = Uri.EscapeDataString(keyword);
            string apiKey = ConfigurationManager.AppSettings["NewsApi_ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
                return articles;

            string apiUrl = string.Format(_apiUrl, encodedKeyword, apiKey);

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "MyNewsApp/1.0");

                    var response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<NewsApiResponse>(json);

                    if (result?.Status == "ok")
                        articles = result.Articles ?? new List<NewsModel>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khi gọi News API: " + ex.Message);
            }

            return articles;
        }

        // Action trả về PartialView chứa danh sách bài viết
        public async Task<ActionResult> PartialNews()
        {
            try
            {
                var articles = await FetchNewsArticlesAsync("công nghệ");
                return PartialView("_NewsPartial", articles);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải tin tức: " + ex.Message;
                return PartialView("_NewsPartial", new List<NewsModel>());
            }
        }
    }
}
