namespace Formloom.Desktop;

public partial class App
{
    private DesktopSession session = null!;

    protected override void OnInitialized()
    {
        session = new DesktopSession(Generator.GenerateAsync, Saver.SaveAsync, SessionLogger);
    }
}
