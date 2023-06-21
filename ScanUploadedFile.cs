using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace FileTransferService.Functions
{
    public class ScanUploadedFile
    {
        [FunctionName("ScanUploadedFile")]
        public async Task Run([BlobTrigger("%newfiles_container%/{name}", 
                              Connection = "uploadstorage_conn")] Stream myBlob, 
                              string name,
                              [DurableClient] IDurableOrchestrationClient orchestrationClient,
                              ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            ScanInfo scanInfo = new ScanInfo() 
            {
                FileName = name,
                ContainerName = Environment.GetEnvironmentVariable("newfiles_container")
            };
            await orchestrationClient.StartNewAsync("OrchestateScanUploadedFiles", scanInfo);
        }
    }
}
