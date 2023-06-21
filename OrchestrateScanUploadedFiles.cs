using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace FileTransferService.Functions
{
    public static class OrchestateScanUploadedFiles
    {
        [FunctionName("OrchestateScanUploadedFiles")]
        public static async Task Run([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            ScanInfo scanInfo = context.GetInput<ScanInfo>();
            scanInfo.InstanceId = context.InstanceId;

            ScanResults scanResults = await context.CallActivityAsync<ScanResults>("ExecuteScan", scanInfo);
            FileInfo fileInfo = new FileInfo()
            {
                fileName = scanInfo.FileName,
                containerName = "general",
                groupName = "root",
                impactLevel = EnvironmentImpactLevel.IL5,
                isThreat = scanResults.isThreat,
                threatType = scanResults.threatType
            };
            
            await context.CallActivityAsync("ProcessScanResults", fileInfo);
        }
    }
}