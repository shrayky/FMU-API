using FmuApiSettings;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://+:{Constants.Parametrs.ServerConfig.}");

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
