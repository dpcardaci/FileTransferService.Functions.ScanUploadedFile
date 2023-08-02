using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileTransferService.Functions
{
    public class ScanUploadedFile
    {
        private readonly IConfiguration _configuration;
        public ScanUploadedFile(IConfiguration configuration) {
            _configuration = configuration;
        }

        [FunctionName("ScanUploadedFile")]
        public async Task Run([BlobTrigger("%UploadNewFilesContainerName%/{name}", 
                              Connection = "UploadStorageAccountConnectionString")] Stream myBlob, 
                              string name,
                              [DurableClient] IDurableOrchestrationClient orchestrationClient,
                              ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            ScanInfo scanInfo = new ScanInfo() 
            {
                FileName = name,
                ContainerName = _configuration["UploadNewFilesContainerName"]
            };
            await orchestrationClient.StartNewAsync("OrchestateScanUploadedFiles", scanInfo);
        }
    }
}
