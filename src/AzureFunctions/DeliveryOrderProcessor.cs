using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AzureFunctions
{
    public static class DeliveryOrderProcessor
    {
        [FunctionName("DeliveryOrderProcessor")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "eshop-db", 
                collectionName: "eshop-container",
                ConnectionStringSetting = "ConnectionStrings:CosmosDBConnection",
                CreateIfNotExists = true)] out dynamic collector,
            ILogger log)
        {
            var json = req.ReadAsStringAsync().Result;
            collector = json;
            log.LogInformation("Processed {0}", json);

            return new OkObjectResult("ok");
        }
    }
}
