using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctions
{
    public static class DeliveryOrderProcessor
    {
        [FunctionName("DeliveryOrderProcessor")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            //[CosmosDB("eshop-db", "eshop-container", ConnectionStringSetting = "CosmosDBConnection",
            //CreateIfNotExists = true)] out dynamic collector,
            ILogger log)
        {
            string json = req.ReadAsStringAsync().Result;
            log.LogWarning(json);
            //collector = json;

            return new OkObjectResult("ok");
        }
    }
}
