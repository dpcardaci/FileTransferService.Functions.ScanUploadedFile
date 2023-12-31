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
                TransferError transferError = JsonSerializer.Deserialize<TransferError>(eventGridEvent.Data.ToString());
                log.LogError($"Transfer Error: {transferError.Message}");
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

                EventGridEvent threatEventGridEvent;
                EventGridPublisherClient threatDetectedPublisher = new EventGridPublisherClient(
                    new Uri(_configuration["ThreatDetectedTopicUri"]),
                    new AzureKeyCredential(_configuration["ThreatDetectedTopicKey"]));

                threatEventGridEvent = new EventGridEvent(
                    "FileTransferService/Threat",
                    "Detected",
                    "1.0",
                    transferInfo
                );

                await threatDetectedPublisher.SendEventAsync(threatEventGridEvent);

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

            EventGridEvent destinationEventGridEvent;

            EventGridPublisherClient destinationPublisher;

            transferInfo.FilePath = destContainer;
            if (transferInfo.ScanInfo.IsThreat)
            {
                destinationPublisher = new EventGridPublisherClient(
                    new Uri(_configuration["ThreatQuarantinedTopicUri"]),
                    new AzureKeyCredential(_configuration["ThreatQuarantinedTopicKey"]));

                destinationEventGridEvent = new EventGridEvent(
                    "FileTransferService/Threat",
                    "Quarantined",
                    "1.0",
                    transferInfo
                );
            }
            else
            {
                destinationPublisher = new EventGridPublisherClient(
                    new Uri(_configuration["TransferStagedTopicUri"]),
                    new AzureKeyCredential(_configuration["TransferStagedTopicKey"]));

                destinationEventGridEvent = new EventGridEvent(
                    "FileTransferService/Transfer",
                    "Staged",
                    "1.0",
                    transferInfo
                );
            }

            await destinationPublisher.SendEventAsync(destinationEventGridEvent);


            BlobClient srcClient = new BlobClient(srcUri, credential);
            await srcClient.DeleteAsync();

            log.LogInformation($"Delete operation from source container: {srcContainer} completed at: {DateTime.Now}");
            
        }
    }
}