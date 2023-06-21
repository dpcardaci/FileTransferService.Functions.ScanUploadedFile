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

namespace FileTransferService.Functions
{
    public class ProcessScanResults
    {

        [FunctionName("ProcessScanResults")]
        public async Task Run([ActivityTrigger] FileInfo fileInfo, ILogger log) 
        {
            Guid id = Guid.NewGuid();
            string fileName = fileInfo.fileName;
            string destFileName = $"{id}-{fileName}";
            string baseStoragePath = "blob.core.usgovcloudapi.net";
            string accountName = Environment.GetEnvironmentVariable("uploadstorage_name");
            string accountSas = Environment.GetEnvironmentVariable("uploadstorage_sas");

            log.LogInformation($"account sas: {Environment.GetEnvironmentVariable("uploadstorage_sas")}");

            log.LogInformation($"new files container: {Environment.GetEnvironmentVariable("newfiles_container")}");
            log.LogInformation($"clean files container: {Environment.GetEnvironmentVariable("cleanfiles-container")}");
            log.LogInformation($"quarantine files container: {Environment.GetEnvironmentVariable("quarantinefiles_container")}");            

            string destContainer = !fileInfo.isThreat ? Environment.GetEnvironmentVariable("cleanfiles-container") 
                                                      : Environment.GetEnvironmentVariable("quarantinefiles_container");
            string srcContainer = Environment.GetEnvironmentVariable("newfiles_container");;

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