using Formloom.Desktop.PngExport;
using Formloom.Desktop.Studio;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using PhotinoX.App;

namespace Formloom.Desktop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var builder = PhotinoBlazorApp.CreateBuilder(new PhotinoAppOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = "wwwroot",
        });
        builder.Services.AddSingleton<GenerationService>();
        builder.Services.AddSingleton<PngFileWriter>();
        builder.Services.AddSingleton<PngSaveService>();
        builder.Services.AddTransient<DesktopSession>(services => new DesktopSession(
            (request, options) => services.GetRequiredService<GenerationService>().GenerateAsync(request, options),
            services.GetRequiredService<PngSaveService>().SaveAsync,
            services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DesktopSession>>()));
        builder.RootComponents.Add<App>("#app");
        builder.ConfigureMainWindow(window =>
        {
            window.SetTitle("Formloom").SetSize(1400, 900);
        });

        using var app = builder.Build();
        app.Services.GetRequiredService<PngSaveService>().Initialize(app.MainWindow);
        return app.Run();
    }
}
