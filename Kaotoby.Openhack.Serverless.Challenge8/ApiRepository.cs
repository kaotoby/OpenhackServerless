using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kaotoby.Openhack.Serverless.Challenge8
{
    public class ApiRepository
    {
        public async Task<Product[]> GetProductsAsync()
        {

            using (HttpClient httpClient = new HttpClient())
            {
                string response = await httpClient.GetStringAsync("https://serverlessohproduct.trafficmanager.net/api/GetProducts");
                return JsonConvert.DeserializeObject<Product[]>(response);
            }
        }
        public async Task<Product> GetProductAsync(Guid productId)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string response = await httpClient.GetStringAsync($"https://serverlessohproduct.trafficmanager.net/api/GetProduct?productId={productId}");
                return JsonConvert.DeserializeObject<Product>(response);
            }
        }
        public async Task<User> GetUserAsync(Guid userId)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string response = await httpClient.GetStringAsync($"https://serverlessohuser.trafficmanager.net/api/GetUser?userId={userId}");
                return JsonConvert.DeserializeObject<User>(response);
            }
        }
    }

    public class Product
    {
        public string productId { get; set; }
        public string productName { get; set; }
        public string productDescription { get; set; }
    }

    public class User
    {
        public string userId { get; set; }
        public string userName { get; set; }
        public string fullName { get; set; }
    }
}
