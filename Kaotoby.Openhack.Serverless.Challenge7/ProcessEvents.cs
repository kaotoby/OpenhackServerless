using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaotoby.Openhack.Serverless.Challenge7
{
    public static class ProcessEvents
    {
        [FunctionName("ProcessEvents")]
        public static async Task Run([EventHubTrigger("challenge7", Connection = "EventHubConnection", ConsumerGroup = "test")]EventData[] eventHubMessages,
            [CosmosDB(
                databaseName: "c7",
                collectionName: "Sales",
                ConnectionStringSetting = "CosmosDBConnection")]
                IAsyncCollector<object> documents,
            ILogger log)
        {
            log.LogInformation($"C# Event Hub trigger function processed a message count: {eventHubMessages.Length}");

            foreach (EventData message in eventHubMessages)
            {
                string body = Encoding.UTF8.GetString(message.Body);
                dynamic sale = JsonConvert.DeserializeObject<dynamic>(body);

                // Save to database
                await documents.AddAsync(sale);
            }
        }
    }

    public class Sale
    {
        public string id { get; set; }
        public SaleHeader header { get; set; }
        public SaleDetail[] details { get; set; }
    }

    public class SaleHeader
    {
        public string salesNumber { get; set; }
        public DateTime dateTime { get; set; }
        public string locationId { get; set; }
        public string locationName { get; set; }
        public string locationAddress { get; set; }
        public string locationPostcode { get; set; }
        public string totalCost { get; set; }
        public string totalTax { get; set; }
    }

    public class SaleDetail
    {
        public string productId { get; set; }
        public string quantity { get; set; }
        public string unitCost { get; set; }
        public string totalCost { get; set; }
        public string totalTax { get; set; }
        public string productName { get; set; }
        public string productDescription { get; set; }
    }

}
