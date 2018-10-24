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
using System.Collections.Generic;
using Microsoft.Azure.Documents.Client;
using System.Linq;

namespace Kaotoby.Openhack.Serverless.Challenge2
{
    public static class GetRatings
    {
        [FunctionName("GetRatings")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "c2",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection")]
                DocumentClient documentClient,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            Guid userId;
            if (!Guid.TryParse(req.Query["userId"], out userId))
            {
                return new BadRequestResult();
            }

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("c2", "Ratings");
            List<Rating> ratings = documentClient.CreateDocumentQuery<Rating>(collectionUri)
                .Where(rating => rating.userId == userId)
                .ToList();

            return new JsonResult(ratings);
        }
    }
}
