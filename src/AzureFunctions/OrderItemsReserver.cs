using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AzureFunctions
{
    public static class OrderItemsReserver
    {
        [FunctionName("OrderItemsReserver")]
        public static async Task Run(
            [ServiceBusTrigger("eshop-queue", Connection = "ServiceBusConnectionString")] string myQueueItem,
            [Blob("orders/bus-{rand-guid}.json", FileAccess.Write)] Stream blob,
            ILogger log)
        {
            using var sw = new StreamWriter(blob);
            await sw.WriteAsync(myQueueItem);

            log.LogInformation($"OrderItemsReserver processed message: {myQueueItem}");
        }
    }
}
