using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

public class ImageController : Controller
{
    private readonly AzureCognitiveServicesSettings _settings;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IOptions<AzureCognitiveServicesSettings> options, ILogger<ImageController> logger)
    {
        _settings = options.Value;
        _logger = logger;

        _logger.LogInformation($"Project ID: {_settings.CustomVision.ProjectId}");
        _logger.LogInformation($"Published Model Name: {_settings.CustomVision.PublishedModelName}");
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzeImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            ViewBag.ErrorMessage = "Please provide a valid image URL.";
            return View("Index");
        }

        imageUrl = imageUrl.Trim();

        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
        {
            ViewBag.ErrorMessage = "The URL format is invalid. Please provide a valid URL.";
            return View("Index");
        }

        try
        {
            var client = Authenticate(_settings.CustomVision.PredictionEndpoint, _settings.CustomVision.PredictionKey);
            var analysisResult = await AnalyzeImageUrl(client, imageUrl);

            var model = new ImageAnalysisResult
            {
                ImageUrl = imageUrl,
                Tags = analysisResult.Predictions.Select(p => p.TagName).ToList(),
                Description = analysisResult.Predictions.FirstOrDefault()?.TagName
            };

            TempData["AnalysisResult"] = JsonConvert.SerializeObject(model);

            return RedirectToAction("Result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image with URL: {ImageUrl}", imageUrl);
            ViewBag.ErrorMessage = $"An error occurred while analyzing the image: {ex.Message}";
            return View("Index");
        }
    }

    public IActionResult Result()
    {
        var analysisResultJson = TempData["AnalysisResult"] as string;

        if (string.IsNullOrEmpty(analysisResultJson))
        {
            _logger.LogWarning("No analysis result found in TempData.");
            return RedirectToAction("Index");
        }

        var model = JsonConvert.DeserializeObject<ImageAnalysisResult>(analysisResultJson);
        return View(model);
    }

    private CustomVisionPredictionClient Authenticate(string endpoint, string key)
    {
        return new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(key))
        {
            Endpoint = endpoint
        };
    }

    private async Task<ImagePrediction> AnalyzeImageUrl(CustomVisionPredictionClient client, string imageUrl)
    {
        var projectId = _settings.CustomVision.ProjectId;
        var publishedModelName = _settings.CustomVision.PublishedModelName;

        using (var httpClient = new System.Net.Http.HttpClient())
        {
            var response = await httpClient.GetAsync(imageUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to retrieve image from URL: {imageUrl}");
            }

            var contentType = response.Content.Headers.ContentType;
            if (contentType == null || !contentType.MediaType.StartsWith("image/"))
            {
                throw new InvalidOperationException("The URL does not point to a valid image.");
            }

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                if (stream == null || stream.Length == 0)
                {
                    throw new InvalidOperationException("Failed to obtain a valid stream from the image URL.");
                }

                _logger.LogInformation($"Stream length: {stream.Length}");

                stream.Position = 0;

                if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(publishedModelName))
                {
                    throw new InvalidOperationException("Project ID or Published Model Name is not set.");
                }

                var result = await client.ClassifyImageAsync(Guid.Parse(projectId), publishedModelName, stream);
                return result;
            }
        }
    }
}
