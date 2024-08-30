using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Extensions.Options;

public class ImageController : Controller
{
    private readonly AzureCognitiveServicesSettings _settings;
    private readonly ILogger<ImageController> _logger;

    // Konstruktor som initialiserar inställningar och logger
    public ImageController(IOptions<AzureCognitiveServicesSettings> options, ILogger<ImageController> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    // Visar startsidan där användare kan ange en bild-URL
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    // Hanterar POST-begäran för att analysera en bild
    public async Task<IActionResult> AnalyzeImage(string imageUrl)
    {
        // Kontrollera om URL är tom eller ogiltig
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
            // Autentisera och analysera bilden
            var client = Authenticate(_settings.CustomVision.PredictionEndpoint, _settings.CustomVision.PredictionKey);
            var analysisResult = await AnalyzeImageUrl(client, imageUrl);

            // Skapa ett resultatobjekt för att visa analysresultat
            var model = new ImageAnalysisResult
            {
                ImageUrl = imageUrl,
                Tags = analysisResult.Predictions.Select(p => p.TagName).ToList(),
                Description = analysisResult.Predictions.FirstOrDefault()?.TagName
            };

            TempData["AnalysisResult"] = model;

            return RedirectToAction("Result");
        }
        catch (Exception ex)
        {
            // Logga eventuella fel och visa ett felmeddelande
            _logger.LogError(ex, "Error analyzing image with URL: {ImageUrl}", imageUrl);
            ViewBag.ErrorMessage = $"An error occurred while analyzing the image: {ex.Message}";
            return View("Index");
        }
    }

    // Visar resultatet från analysen
    public IActionResult Result()
    {
        var model = TempData["AnalysisResult"] as ImageAnalysisResult;

        if (model == null)
        {
            _logger.LogWarning("No analysis result found in TempData.");
            return RedirectToAction("Index");
        }

        return View(model);
    }

    // Skapa en klient för att kommunicera med Custom Vision API
    private CustomVisionPredictionClient Authenticate(string endpoint, string key)
    {
        return new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(key))
        { Endpoint = endpoint };
    }

    // Analysera bilden med hjälp av Custom Vision API
    private async Task<ImagePrediction> AnalyzeImageUrl(CustomVisionPredictionClient client, string imageUrl)
    {
        var projectId = _settings.CustomVision.ProjectId;
        var publishedModelName = _settings.CustomVision.PublishedModelName;

        using (var stream = await new System.Net.Http.HttpClient().GetStreamAsync(imageUrl))
        {
            var result = await client.ClassifyImageAsync(Guid.Parse(projectId), publishedModelName, stream);
            return result;
        }
    }
}
