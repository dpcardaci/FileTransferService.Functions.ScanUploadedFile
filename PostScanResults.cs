using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace FileTransferService.Functions
{
    public class PostScanResults
    {
        private HttpClient httpClient = new HttpClient();

        [FunctionName("PostScanResults")]
        public async Task Run([ActivityTrigger] FileInfo fileInfo, ILogger log) 
        {
            var scanresultprocessorUri = Environment.GetEnvironmentVariable("scanresultprocessor_uri");
            log.LogInformation($"scanresultprocessorUri: {scanresultprocessorUri}");

            string jsonFileInfo = JsonSerializer.Serialize(fileInfo);
            log.LogInformation($"jsonFileInfo: {jsonFileInfo}");

            StringContent payload = new StringContent(jsonFileInfo, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(scanresultprocessorUri, payload);
        
            Stream responseStream = response.Content.ReadAsStream();
            StreamReader responseStreamReader = new StreamReader(responseStream, Encoding.UTF8);

            log.LogInformation($"Response Message: {responseStreamReader.ReadToEnd()}");
        }
    }
}