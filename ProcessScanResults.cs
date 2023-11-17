using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.EventGrid;
using FileTransferService.Core;

namespace FileTransferService.Functions
{
    public class ProcessScanResults
    {
        private readonly IConfiguration _configuration;
        public ProcessScanResults(IConfiguration configuration) {
            _configuration = configuration;
        }

        [FunctionName("ProcessScanResults")]
        public async Task Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log) 
        {
            if(eventGridEvent.EventType == "Error")
            {
                ScanError scanError = JsonSerializer.Deserialize<ScanError>(eventGridEvent.Data.ToString());
                log.LogError($"Scan Error: {scanError.ErrorMessage}");
                return;
            }
            
            TransferInfo transferInfo = JsonSerializer.Deserialize<TransferInfo>(eventGridEvent.Data.ToString());
            log.LogInformation($"Processing Scan Results for: {transferInfo.FileName} at: {DateTime.Now}");

            Guid id = Guid.NewGuid();  
            string fileName = transferInfo.FileName;
            string destFileName = $"{id}-{fileName}";
            string baseStoragePath = "blob.core.usgovcloudapi.net";
            string accountName = _configuration["UploadStorageAccountName"];
            string accountSas = _configuration["UploadStorageAccountSasToken"];           

            string destContainer = !transferInfo.ScanInfo.IsThreat ? _configuration["UploadCleanFilesContainerName"]
                                                      : _configuration["UploadQuarantineFilesContainerName"];
            if(transferInfo.ScanInfo.IsThreat) {
                log.LogInformation($"Threat detected for: {fileName} at: {DateTime.Now}");
            } else {
                log.LogInformation($"No threat detected for: {fileName} at: {DateTime.Now}");
            }

            string srcContainer = transferInfo.FilePath;

            string destPath = $"https://{accountName}.{baseStoragePath}/{destContainer}/{destFileName}";
            string srcPath = $"https://{accountName}.{baseStoragePath}/{srcContainer}/{fileName}";

            Uri destUri = new Uri(destPath);
            Uri srcUri = new Uri(srcPath);

            AzureSasCredential credential = new AzureSasCredential(accountSas);

            BlobClient destClient = new BlobClient(destUri, credential);
            CopyFromUriOperation copyFromUriOperation = await destClient.StartCopyFromUriAsync(srcUri);
            copyFromUriOperation.WaitForCompletion();
            log.LogInformation($"Copy operation to destination container: {destContainer} completed at: {DateTime.Now}");

            BlobClient srcClient = new BlobClient(srcUri, credential);
            await srcClient.DeleteAsync();

            log.LogInformation($"Delete operation from source container: {srcContainer} completed at: {DateTime.Now}");
            
        }
    }
}