using Microsoft.JSInterop;

namespace CorUI.Services;

public sealed class WebViewStorage : IViewStorage
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, string> _memory = new();

    public WebViewStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool GetBool(string key)
    {
        var s = GetStringInternal(key);
        if (bool.TryParse(s, out var b)) return b;
        if (int.TryParse(s, out var i)) return i != 0;
        return false;
    }

    public string GetString(string key)
    {
        return GetStringInternal(key) ?? string.Empty;
    }

    public float GetFloat(string key)
    {
        var s = GetStringInternal(key);
        if (float.TryParse(s, out var f)) return f;
        return 0f;
    }

    public int GetInt(string key)
    {
        var s = GetStringInternal(key);
        if (int.TryParse(s, out var i)) return i;
        return 0;
    }

    public void SetBool(string key, bool value)
    {
        SetStringInternal(key, value ? "true" : "false");
    }

    public void SetString(string key, string value)
    {
        SetStringInternal(key, value ?? string.Empty);
    }

    public void SetFloat(string key, float value)
    {
        SetStringInternal(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public void SetInt(string key, int value)
    {
        SetStringInternal(key, value.ToString());
    }

    private string? GetStringInternal(string key)
    {
        try
        {
            if (_jsRuntime is IJSInProcessRuntime sync)
            {
                return sync.Invoke<string?>("localStorage.getItem", key);
            }
            else
            {
                // Fallback (server environments). Avoid deadlocks by not blocking on WASM
                return _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key).AsTask().GetAwaiter().GetResult();
            }
        }
        catch
        {
            // Fallback to in-memory store if JS is unavailable
            return _memory.TryGetValue(key, out var v) ? v : null;
        }
    }

    private void SetStringInternal(string key, string value)
    {
        try
        {
            if (_jsRuntime is IJSInProcessRuntime sync)
            {
                sync.InvokeVoid("localStorage.setItem", key, value);
            }
            else
            {
                _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value).AsTask().GetAwaiter().GetResult();
            }
        }
        catch
        {
            _memory[key] = value;
        }
    }
}


