

namespace FileTransferService.Functions 
{
    public class FileInfo 
    {
        public string fileName { get; set; } = "";
        public string containerName { get; set; } = "";
        public string groupName { get; set; } = "";
        public EnvironmentImpactLevel impactLevel { get; set; } = EnvironmentImpactLevel.IL5;
        public bool isThreat  { get; set; } = false;
        public string threatType  { get; set; } = "";  
    }

    public enum EnvironmentImpactLevel 
    {
        IL1 = 1,
        IL2 = 2,
        IL3 = 3,
        IL4 = 4,
        IL5 = 5,
        Il6 = 6,
        IL7 = 7
    }
}