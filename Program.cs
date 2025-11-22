using SelfCheckoutKiosk.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register database service
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

// Register HTTP client factory for Magneti payment service
builder.Services.AddHttpClient("MagnetiPaymentClient")
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "SelfCheckoutKiosk/1.0");
    });

// Register Magneti payment service (supports REST API and simulation modes)
builder.Services.AddScoped<IMagnetiPaymentService, MagnetiPaymentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Ensure correct MIME for .webmanifest
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".webmanifest"] = "application/manifest+json";
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
