using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System;
using System.Threading;
using FileTransferService.Core;

namespace FileTransferService.Functions
{
    public class ScannerProxy
    {

        private string hostIp { get; set; }
        private HttpClient client;
        private ILogger log { get; }

        public ScannerProxy(string hostIp, ILogger log)
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

        public async Task<bool> ScanAsync(TransferInfo transferInfo)
        {
            log.LogInformation($"Initializing scan request at: {DateTime.Now}");

            string url = "https://" + hostIp + "/scan";
            log.LogInformation($"Scanner URL: {url}");

            var response = await client.PostAsync(url, JsonContent.Create(transferInfo));
            log.LogInformation($"Posting scan request at: {DateTime.Now}");

            if (!response.IsSuccessStatusCode)
            {
                log.LogError($"Request Failed, {response.StatusCode}");
                return false;
            }

            log.LogInformation($"Request Success Status Code:{response.StatusCode}");
            return true;

        }
    }
}