using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileTransferService.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;

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

            string baseStoragePath = "blob.core.usgovcloudapi.net";
            string accountName = _configuration["UploadStorageAccountName"];
            string accountSas = _configuration["UploadStorageAccountSasToken"];
            string newFilesContainer = _configuration["UploadNewFilesContainerName"];
            string blobPath = $"https://{accountName}.{baseStoragePath}/{newFilesContainer}/{name}";

            Uri blobUri = new Uri(blobPath);

            AzureSasCredential credential = new AzureSasCredential(accountSas);
            BlobClient blobClient = new BlobClient(blobUri, credential);

            BlobProperties blobProperties = await blobClient.GetPropertiesAsync();
            var destinationImpactLevel = EnvironmentImpactLevel.Parse<EnvironmentImpactLevel>(blobProperties.Metadata["destinationimpactlevel"]);
            var transferId = blobProperties.Metadata["transferid"];
            var userPrincipalName = blobProperties.Metadata["userprincipalname"];
            var originationDateTime = blobProperties.Metadata["originationdatetime"];

            TransferInfo transferInfo = new TransferInfo 
            {
                TransferId = Guid.Parse(transferId),
                OriginatingUserPrincipalName = userPrincipalName,
                OriginationDateTime = DateTime.Parse(originationDateTime),
                FileName = name,
                FilePath = newFilesContainer,
                ImpactLevel = destinationImpactLevel
            };
            
            EventGridEvent scanEventGridEvent;

            bool scanRequestAccepted = await scanner.ScanAsync(transferInfo);
            if (scanRequestAccepted) {
                log.LogInformation($"Scan request accepted for: {name} at: {DateTime.Now}");

                EventGridPublisherClient scanRequestedPublisher = new EventGridPublisherClient(
                    new Uri(_configuration["ScanRequestedTopicUri"]), 
                    new Azure.AzureKeyCredential(_configuration["ScanRequestedTopicKey"]));

                scanEventGridEvent = new EventGridEvent(
                    "FileTransferService/Scan", 
                    "Requested", 
                    "1.0", 
                    transferInfo
                );

                scanRequestedPublisher.SendEvent(scanEventGridEvent);
                
            } else {
                log.LogError($"Scan request failed for: {name} at: {DateTime.Now}");

                TransferError transferError = new TransferError
                {
                    TransferId = Guid.Parse(transferId),
                    OriginatingUserPrincipalName = userPrincipalName,
                    OriginationDateTime = DateTime.Parse(originationDateTime),
                    FileName = name
                };

                EventGridPublisherClient scanErrorPublisher = new EventGridPublisherClient(
                    new Uri(_configuration["ScanErrorTopicUri"]), 
                    new Azure.AzureKeyCredential(_configuration["ScanErrorTopicKey"]));

                scanEventGridEvent = new EventGridEvent(
                    "FileTransferService/Scan", 
                    "Error",
                     "1.0",
                     transferError
                );

                scanErrorPublisher.SendEvent(scanEventGridEvent);
            }
        }
    }
}
