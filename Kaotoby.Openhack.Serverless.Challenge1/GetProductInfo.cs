using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kaotoby.Openhack.Serverless.Challenge1
{
    public static class GetProductInfo
    {
        [FunctionName("GetProductInfo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                Guid productId;
                if (!Guid.TryParse(req.Query["productId"], out productId))
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(requestBody);
                    productId = data.productId;
                }
                
                string message = $"The product name for your product id {productId} is Starfruit Explosion";
                if (req.Method == "POST")
                {
                    message += $" and the description is This starfruit ice cream is out of this world!";
                }

                return (ActionResult)new OkObjectResult(message);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }
        }
    }
}
