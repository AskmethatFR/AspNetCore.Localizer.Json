namespace AspNetCore.Localizer.Json.Sample.MAUI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "AspNetCore.Localizer.Json.Sample.MAUI" };
    }
}
