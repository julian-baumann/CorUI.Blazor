using CorUI;

namespace TestApp.Windows;

public sealed class AppHost : ICorUIApplication
{
	public Window StartWindow => new()
	{
		ContentPath = "/",
		Width = 900,
		Height = 700
	};
}

