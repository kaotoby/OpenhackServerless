using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Client;
using System.Linq;
using Microsoft.Azure.Documents;
using System.Collections.Generic;

namespace Kaotoby.Openhack.Serverless.Challenge6
{
    public static class ClearDatabase
    {
        [FunctionName("ClearDatabase")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "c6",
                collectionName: "Orders",
                ConnectionStringSetting = "CosmosDBConnection")]
                DocumentClient documentClient,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("c6", "Orders");
            List<Document> documents = documentClient.CreateDocumentQuery(collectionUri).ToList();
            IEnumerable<Task<ResourceResponse<Document>>> deleteTasks = documents.Select(document => documentClient.DeleteDocumentAsync(document.SelfLink));
            await Task.WhenAll(deleteTasks);

            return new OkObjectResult(documents.Count);
        }
    }
}
