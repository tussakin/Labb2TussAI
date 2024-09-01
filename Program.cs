var builder = WebApplication.CreateBuilder(args);

var azureCognitiveServicesSettings = builder.Configuration.GetSection("AzureCognitiveServices").Get<AzureCognitiveServicesSettings>();
Console.WriteLine($"Project ID: {azureCognitiveServicesSettings.CustomVision.ProjectId}");
Console.WriteLine($"Published Model Name: {azureCognitiveServicesSettings.CustomVision.PublishedModelName}");

builder.Services.Configure<AzureCognitiveServicesSettings>(builder.Configuration.GetSection("AzureCognitiveServices"));
builder.Services.AddControllersWithViews();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Image}/{action=Index}/{id?}");

app.Run();
