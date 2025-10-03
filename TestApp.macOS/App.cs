using CorUI;

namespace TestApp.macOS;

public class App : ICorUIApplication
{
    public Window StartWindow => new()
    {
        ContentPath = "/",
        Width = 600,
        Height = 500,
        MacWindowOptions =
        {
            MacTrafficLightStyle = MacTrafficLightStyle.Expanded
        }
    };
}