using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Kaotoby.Openhack.Serverless.Challenge2.Models;

namespace Kaotoby.Openhack.Serverless.Challenge2
{
    public static class CreateRating
    {
        [FunctionName("CreateRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "c2",
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

            // Save to db
            await ratings.AddAsync(rating);

            return new JsonResult(rating);

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
