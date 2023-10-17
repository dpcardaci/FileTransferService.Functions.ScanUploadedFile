using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FileTransferService.Functions.Core;
using Microsoft.Azure.WebJobs;
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
                              Connection = "UploadStorageAccountConnectionString")] Stream blob, 
                              string name,
                              ILogger log)
        {
            log.LogInformation($"Scan processing triggered for: {name} at: {DateTime.Now}");
            
            var scannerHostIp = _configuration["WindowsDefenderHost"];
            log.LogInformation($"Scanner Host: {scannerHostIp}");

            ScannerProxy scanner = new ScannerProxy(scannerHostIp, log);

            // To Do: Add logic to determine impact level
            TransferInfo transferInfo = new TransferInfo 
            {
                FileName = name,
                FilePath = _configuration["UploadNewFilesContainerName"],
                ImpactLevel = EnvironmentImpactLevel.IL5
            };
            
            bool scanRequestAccepted = await scanner.ScanAsync(transferInfo);
            if (scanRequestAccepted) {
                log.LogInformation($"Scan request accepted for: {name} at: {DateTime.Now}");
            } else {
                log.LogError($"Scan request failed for: {name} at: {DateTime.Now}");
            }
        }
    }
}
