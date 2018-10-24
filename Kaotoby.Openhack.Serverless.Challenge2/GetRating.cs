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
    public static class GetRating
    {
        [FunctionName("GetRating")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "c2",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{Query.ratingId}")]Rating rating,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            if (rating == null)
            {
                return new NotFoundResult();
            }
            else
            {
                return new JsonResult(rating);
            }
        }
    }
}
