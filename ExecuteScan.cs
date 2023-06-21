using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace FileTransferService.Functions
{

    public static class ExecuteScan
    {
        [FunctionName("ExecuteScan")]
        public static async Task<ScanResults> Run([ActivityTrigger] ScanInfo scanInfo, ILogger log) 
        {
            var scannerHost = Environment.GetEnvironmentVariable("windowsdefender_host");
            var scannerPort = Environment.GetEnvironmentVariable("windowsdefender_port");

            log.LogInformation($"Scanner Host: {scannerHost} and Scanner Port: {scannerPort}");

            ScannerProxy scanner = new ScannerProxy(log, scannerHost);
            ScanResults scanResults = await scanner.ScanAsync(scanInfo.FileName, scanInfo.ContainerName);

            if(scanResults == null) 
            {
                log.LogInformation("Error: failure to acquire scan results.");
                throw(new Exception("Error: failure to acquire scan results."));
            }
            log.LogInformation($"Scan Results isThreat: {scanResults.isThreat.ToString()} and threatType: {scanResults.threatType}");

            return scanResults;
        }

    }
    
}