var builder = WebApplication.CreateBuilder(args);

// Configure services
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
