using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CsvHelper;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Kaotoby.Openhack.Serverless.Challenge6
{
    public static class BatchProcessCsv
    {
        [FunctionName("BatchProcessCsv"), Singleton]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Blob("joseblob", Connection = "MyStorageAccountAppSetting")]CloudBlobContainer myBlob,
            [CosmosDB(
                databaseName: "c6",
                collectionName: "Orders",
                ConnectionStringSetting = "CosmosDBConnection")]
                IAsyncCollector<Order> documents,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Get all files in blob
            BlobResultSegment blobResultSegment = await myBlob.ListBlobsSegmentedAsync(null);
            CloudBlockBlob[] blobItems = blobResultSegment.Results.OfType<CloudBlockBlob>().ToArray();

            // Group batches
            Dictionary<string, CloudBlockBlob[]> batches = blobItems
                .GroupBy(blobItem => blobItem.Name.Split('-')[0])
                .Where(group => group.Count() == 3)
                .ToDictionary(group => group.Key, group => group.ToArray());

            // Process batches
            Dictionary<string, List<Order>> batchesProcessed = new Dictionary<string, List<Order>>();
            foreach (var kvp in batches)
            {
                string batchName = kvp.Key;
                CloudBlockBlob[] batchFiles = kvp.Value;
                log.LogInformation($"Processing batch {batchName}");

                Dictionary<string, Task<Stream>> fileDataMap = null;
                try
                {
                    fileDataMap = batchFiles.ToDictionary(blobItem => blobItem.Name.Split('-')[1], blobItem => blobItem.OpenReadAsync());
                    await Task.WhenAll(fileDataMap.Values);

                    List<Order> orders = ParseCsvData(
                        fileDataMap["ProductInformation.csv"].Result,
                        fileDataMap["OrderLineItems.csv"].Result,
                        fileDataMap["OrderHeaderDetails.csv"].Result);

                    // Save to database
                    IEnumerable<Task> saveTasks = orders.Select(order => documents.AddAsync(order));
                    await Task.WhenAll(saveTasks);

                    // Remove files
                    IEnumerable<Task> removeTasks = batchFiles.Select(blobItem => blobItem.DeleteIfExistsAsync());
                    await Task.WhenAll(removeTasks);

                    // Add to result
                    batchesProcessed[kvp.Key] = orders;
                }
                finally
                {
                    // Dispose files
                    if (fileDataMap != null)
                    {
                        foreach (Task<Stream> file in fileDataMap.Values)
                        {
                            if (file.IsCompletedSuccessfully)
                            {
                                file.Result.Dispose();
                            }
                        }
                    }
                }
            }

            return new JsonResult(batchesProcessed.ToArray());
        }

        public static List<Order> ParseCsvData(Stream productInformationFile, Stream orderLineItemsFile, Stream orderHeaderDetailFile)
        {
            Dictionary<string, Product> products;
            Dictionary<string, OrderLineItem[]> orderLineItems;
            List<Order> orders = new List<Order>();

            using (CsvReader csv = new CsvReader(new StreamReader(productInformationFile)))
            {
                products = csv.GetRecords<dynamic>()
                    .ToDictionary(product => (string)product.productid, product => new Product()
                    {
                        ProductId = Guid.Parse(product.productid),
                        ProductName = product.productname,
                        ProductDescription = product.productdescription
                    });
            }

            using (CsvReader csv = new CsvReader(new StreamReader(orderLineItemsFile)))
            {
                orderLineItems = csv.GetRecords<dynamic>()
                    .GroupBy(item => (string)item.ponumber)
                    .ToDictionary(group => group.Key, group => group
                        .Select(item => new OrderLineItem()
                        {
                            Quantity = int.Parse(item.quantity),
                            UnitCost = decimal.Parse(item.unitcost),
                            TotalCost = decimal.Parse(item.totalcost),
                            TotalTax = decimal.Parse(item.totaltax),
                            Product = products[item.productid]
                        })
                        .ToArray()
                    );
            }

            using (CsvReader csv = new CsvReader(new StreamReader(orderHeaderDetailFile)))
            {
                orders = csv.GetRecords<dynamic>()
                    .Select(order => new Order()
                    {
                        id = order.ponumber,
                        DateTime = DateTime.Parse(order.datetime),
                        TotalCost = decimal.Parse(order.totalcost),
                        TotalTax = decimal.Parse(order.totaltax),
                        Location = new Location()
                        {
                            LocationId = order.locationid,
                            LocationName = order.locationname,
                            LocationAddress = order.locationaddress,
                            LocationPostCode = order.locationpostcode,
                        },
                        OrderLineItems = orderLineItems[order.ponumber]
                    })
                    .ToList();
            }

            return orders;
        }
    }

    public class Order
    {
        public string id { get; set; }
        public DateTime DateTime { get; set; }
        public Location Location { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalTax { get; set; }
        public OrderLineItem[] OrderLineItems { get; set; }
    }

    public class Location
    {
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public string LocationAddress { get; set; }
        public string LocationPostCode { get; set; }
    }

    public class OrderLineItem
    {
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalTax { get; set; }
        public Product Product { get; set; }
    }

    public class Product
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
    }
}
