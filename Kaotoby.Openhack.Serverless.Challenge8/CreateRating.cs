using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Kaotoby.Openhack.Serverless.Challenge8.Models;
using System.Net.Http;
using System.Text;
using Microsoft.Rest;
using System.Threading;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;

namespace Kaotoby.Openhack.Serverless.Challenge8
{
    public static class CreateRating
    {
        [FunctionName("CreateRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "c8",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection")]
                IAsyncCollector<Rating> ratings,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            Request data;
            try
            {
                data = JsonConvert.DeserializeObject<Request>(requestBody);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestResult();
            }

            ApiRepository repository = new ApiRepository();
            User user = await repository.GetUserAsync(data.userId);
            Product product = await repository.GetProductAsync(data.productId);

            // Validation
            if (user == null || product == null)
            {
                return new NotFoundResult();
            }
            if (data.rating < 0 || data.rating > 5)
            {
                return new BadRequestResult();
            }

            Rating rating = new Rating()
            {
                id = Guid.NewGuid(),
                userId = data.userId,
                productId = data.productId,
                locationName = data.locationName,
                rating = data.rating,
                userNotes = data.userNotes,
                timestamp = DateTime.UtcNow
            };
            rating.sentimentScore = await GetScore(rating.id.ToString(), rating.userNotes);

            // Save to db
            await ratings.AddAsync(rating);

            // Trigger Logic App
            using (HttpClient httpClient = new HttpClient())
            {
                string json = JsonConvert.SerializeObject(rating);
                string url = "https://prod-01.centralus.logic.azure.com:443/workflows/e938333782c84298a604ab2e32be0655/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=sCeibNnsiT8KC7_1YmpuL1bzKEuAleVuFKEzhPXuEes";
                await httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            }

            return new JsonResult(rating);

        }

        private static async Task<double> GetScore(string id, string text)
        {
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = "https://eastus.api.cognitive.microsoft.com"
            };
            List<MultiLanguageInput> multiLanguageInputs = new List<MultiLanguageInput>();
            multiLanguageInputs.Add(new MultiLanguageInput()
            {
                Id = id,
                Text = text,
                Language = "en"
            });
            SentimentBatchResult result = await client.SentimentAsync(new MultiLanguageBatchInput(multiLanguageInputs));
            return result.Documents[0].Score ?? 1.0;
        }

        class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", "39a6ea487b634769a0ae999ec70d2220");
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }

        public class Request
        {
            public Guid userId { get; set; }
            public Guid productId { get; set; }
            public string locationName { get; set; }
            public int rating { get; set; }
            public string userNotes { get; set; }
        }

    }
}
