using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace FileTransferService.Functions
{
    public class ScannerProxy
    {

        private string hostIp { get; set; }
        private HttpClient client;
        private ILogger log { get; }

        public ScannerProxy(ILogger log, string hostIp)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };
            this.hostIp = hostIp;
            this.log = log;
            client = new HttpClient(handler);
            client.Timeout = Timeout.InfiniteTimeSpan;
        }

        public ScanResults Scan(string fileName, string containerName)
        {
            string url = "https://" + hostIp + "/scan";
            log.LogInformation($"Scanner URL: {url}");

            var form = CreateMultiPartForm(fileName, containerName);
            log.LogInformation("After create multipart form");

            var response = client.PostAsync(url, form).Result;
            log.LogInformation("After post resquest");

            string stringContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            log.LogInformation("After response");

            if (!response.IsSuccessStatusCode)
            {
                log.LogError($"Request Failed, {response.StatusCode}:{stringContent}");
                return null;
            }

            log.LogInformation($"Request Success Status Code:{response.StatusCode}");

            var responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(stringContent);
            var scanResults = new ScanResults();
            scanResults.isThreat = Convert.ToBoolean(responseDictionary["isThreat"]);
            scanResults.threatType = responseDictionary["ThreatType"];

            log.LogInformation($"isThreat: {scanResults.isThreat.ToString()}");
            log.LogInformation($"threatType: {scanResults.threatType}");

            return scanResults;
        }

        public async Task<ScanResults> ScanAsync(string fileName, string containerName)
        {
            log.LogInformation("In ScanAsync");

            string url = "https://" + hostIp + "/scan";
            log.LogInformation($"Scanner URL: {url}");

            var form = CreateMultiPartForm(fileName, containerName);
            log.LogInformation("After create multipart form");

            var response = await client.PostAsync(url, form);
            log.LogInformation("After post resquest");

            string stringContent = await response.Content.ReadAsStringAsync();
            log.LogInformation("After response");

            if (!response.IsSuccessStatusCode)
            {
                log.LogError($"Request Failed, {response.StatusCode}:{stringContent}");
                return null;
            }

            log.LogInformation($"Request Success Status Code:{response.StatusCode}");

            var responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(stringContent);
            var scanResults = new ScanResults();
            scanResults.isThreat = Convert.ToBoolean(responseDictionary["isThreat"]);
            scanResults.threatType = responseDictionary["ThreatType"];

            log.LogInformation($"isThreat: {scanResults.isThreat.ToString()}");
            log.LogInformation($"threatType: {scanResults.threatType}");

            return scanResults;
        }
        private static MultipartFormDataContent CreateMultiPartForm(string blobName, string containerName)
        {
            string boundry = GenerateRandomBoundry();
            MultipartFormDataContent form = new MultipartFormDataContent(boundry);
            
            var blobNameContent = new StringContent(blobName);
            var containerNameContent = new StringContent(containerName);

            blobNameContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            containerNameContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

            form.Add(blobNameContent, "blobName");
            form.Add(containerNameContent, "containerName");            
            return form;
        }

        private static string GenerateRandomBoundry()
        {
            const int maxBoundryLength = 69;
            const string src = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var stringBuilder = new StringBuilder();
            Random random = new Random();
            int length = random.Next(1, maxBoundryLength - 2);
            int numOfHyphens = (maxBoundryLength) - length;

            for (var i = 0; i < length; i++)
            {
                var c = src[random.Next(0, src.Length)];
                stringBuilder.Append(c);
            }
            string randomString = stringBuilder.ToString();
            string boundry = randomString.PadLeft(numOfHyphens, '-');
            return boundry;
        }
    }
}