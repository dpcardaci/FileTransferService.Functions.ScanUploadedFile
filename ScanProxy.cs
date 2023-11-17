using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

            //var form = CreateMultiPartForm(transferInfo.FileName, transferInfo.FilePath);

            var response = await client.PostAsync(url, JsonSerializer.Serialize(transferInfo));
            log.LogInformation($"Posting scan request at: {DateTime.Now}");

            if (!response.IsSuccessStatusCode)
            {
                log.LogError($"Request Failed, {response.StatusCode}");
                return false;
            }

            log.LogInformation($"Request Success Status Code:{response.StatusCode}");
            return true;

        }
        // private static MultipartFormDataContent CreateMultiPartForm(string fileName, string filePath)
        // {
        //     string boundry = GenerateRandomBoundry();
        //     MultipartFormDataContent form = new MultipartFormDataContent(boundry);
            
        //     var fileNameContent = new StringContent(fileName);
        //     var filePathContent = new StringContent(filePath);

        //     fileNameContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        //     filePathContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            
        //     form.Add(fileNameContent, "fileName");
        //     form.Add(filePathContent, "filePath");            
        //     return form;
        // }

        // private static string GenerateRandomBoundry()
        // {
        //     const int maxBoundryLength = 69;
        //     const string src = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        //     var stringBuilder = new StringBuilder();
        //     Random random = new Random();
        //     int length = random.Next(1, maxBoundryLength - 2);
        //     int numOfHyphens = (maxBoundryLength) - length;

        //     for (var i = 0; i < length; i++)
        //     {
        //         var c = src[random.Next(0, src.Length)];
        //         stringBuilder.Append(c);
        //     }
        //     string randomString = stringBuilder.ToString();
        //     string boundry = randomString.PadLeft(numOfHyphens, '-');
        //     return boundry;
        // }
    }
}