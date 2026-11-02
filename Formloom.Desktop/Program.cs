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
        builder.RootComponents.Add<App>("#app");
        builder.ConfigureMainWindow(window =>
        {
            window.SetTitle("Formloom").SetSize(1400, 900);
        });

        using var app = builder.Build();
        return app.Run();
    }
}
