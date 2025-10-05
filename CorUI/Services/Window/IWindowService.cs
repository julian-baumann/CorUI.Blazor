namespace CorUI.Services;

public interface IWindowService
{
    Task OpenWindow(Window window);
    Task OpenDialog(Dialog window);
}