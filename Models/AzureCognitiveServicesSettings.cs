public class AzureCognitiveServicesSettings
{
    public CustomVisionSettings CustomVision { get; set; }

    public class CustomVisionSettings
    {
        public string PredictionEndpoint { get; set; }
        public string PredictionKey { get; set; }
        public string ProjectId { get; set; }
        public string PublishedModelName { get; set; }
       
    }
}
