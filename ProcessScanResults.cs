using System;
using System.Web;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace FileTransferService.Functions
{
    public class ProcessScanResults
    {
        private readonly IConfiguration _configuration;
        public ProcessScanResults(IConfiguration configuration) {
            _configuration = configuration;
        }

        [FunctionName("ProcessScanResults")]
        public async Task Run([ActivityTrigger] FileInfo fileInfo, ILogger log) 
        {
            Guid id = Guid.NewGuid();
            string fileName = fileInfo.fileName;
            string destFileName = $"{id}-{fileName}";
            string baseStoragePath = "blob.core.usgovcloudapi.net";
            string accountName = _configuration["UploadStorageAccountName"];
            string accountSas = _configuration["UploadStorageAccountSasToken"];           

            string destContainer = !fileInfo.isThreat ? _configuration["UploadCleanFilesContainerName"]
                                                      : _configuration["UploadQuarantineFilesContainerName"];
            string srcContainer = _configuration["UploadNewFilesContainerName"];

            string destPath = $"https://{accountName}.{baseStoragePath}/{destContainer}/{destFileName}";
            string srcPath = $"https://{accountName}.{baseStoragePath}/{srcContainer}/{fileName}";

            Uri destUri = new Uri(destPath);
            Uri srcUri = new Uri(srcPath);

            AzureSasCredential credential = new AzureSasCredential(accountSas);

            BlobClient destClient = new BlobClient(destUri, credential);
            CopyFromUriOperation copyFromUriOperation = await destClient.StartCopyFromUriAsync(srcUri);
            copyFromUriOperation.WaitForCompletion();

            BlobClient srcClient = new BlobClient(srcUri, credential);
            await srcClient.DeleteAsync();
        }
    }
}