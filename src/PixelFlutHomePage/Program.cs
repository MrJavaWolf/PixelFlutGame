using PixelFlutHomePage;
using PixelFlutHomePage.Services;
using Serilog;



var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
PixelFlutServiceProvider pixelFlutServiceProvider = new PixelFlutServiceProvider()
{
    ServiceProvider = null
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
};
builder.Services.AddSingleton(pixelFlutServiceProvider);
builder.Services.AddSingleton<ExternalGameInputService>();


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddJWolfSwagger(builder.Configuration);



var app = builder.Build();
app.UseSerilogRequestLogging();

// Setup pixel flut client
IHostApplicationLifetime hostApplicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
pixelFlutServiceProvider.ServiceProvider = PixelFlut.Program.Setup(args, hostApplicationLifetime.ApplicationStopped);


app.AddJWolfSwagger();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();


// Run Pixel flut client
Task t = Task.Run(async () =>
{
    await PixelFlut.Program.RunAsync(hostApplicationLifetime.ApplicationStopped, pixelFlutServiceProvider.ServiceProvider);
});

app.Run();


public class PixelFlutServiceProvider
{
    public required ServiceProvider ServiceProvider { get; set; }
}