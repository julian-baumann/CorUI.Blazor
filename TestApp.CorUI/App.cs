using CorUI;

namespace TestApp.macOS;

public class App : ICorUIApplication
{
    public Window StartWindow => new()
    {
        ContentPath = "/",
        Width = 900,
        Height = 700,
        MacWindowOptions =
        {
            MacTrafficLightStyle = MacTrafficLightStyle.Expanded
        }
    };
}