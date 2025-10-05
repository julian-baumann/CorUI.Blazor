using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CorUI.Services;

public sealed class WebWindowService(IJSRuntime jsRuntime, NavigationManager navigationManager) : IWindowService
{
    private TaskCompletionSource<bool>? _dialogTcs;

    public event Func<Dialog, Task>? DialogRequested;

    public async Task OpenWindow(Window window)
    {
        var path = string.IsNullOrWhiteSpace(window.ContentPath) ? "/" : window.ContentPath;
        var url = navigationManager.ToAbsoluteUri(path).ToString();
        try
        {
            await jsRuntime.InvokeVoidAsync("open", url, "_blank");
        }
        catch
        {
            // Best-effort; ignore failures in environments without a window
        }
    }

    public async Task OpenDialog(Dialog dialog)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dialogTcs = tcs;

        var handler = DialogRequested;
        if (handler is not null)
        {
            await handler.Invoke(dialog);
        }

        await tcs.Task;
    }

    public void CloseActiveDialog()
    {
        _dialogTcs?.TrySetResult(true);
        _dialogTcs = null;
    }

    [JSInvokable("CorUI_CloseDialog")]
    public static void CloseDialogFromJs()
    {
        // Static JS interop entrypoint; actual instance hookup is done via message listener in presenter
    }
}


