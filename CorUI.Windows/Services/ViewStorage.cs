using CorUI.Services;

namespace CorUI.Windows.Services;

public sealed class ViewStorage : IViewStorage
{
    private static string TransformKey(string key)
    {
        return $"view-storage-{key}";
    }

    public bool GetBool(string key)
    {
        var settings = global::Windows.Storage.ApplicationData.Current.LocalSettings;
        if (settings.Values.TryGetValue(TransformKey(key), out var value) && value is bool result)
        {
            return result;
        }
        return false;
    }

    public string GetString(string key)
    {
        var settings = global::Windows.Storage.ApplicationData.Current.LocalSettings;
        if (settings.Values.TryGetValue(TransformKey(key), out var value) && value is string result)
        {
            return result;
        }
        return string.Empty;
    }

    public float GetFloat(string key)
    {
        var settings = global::Windows.Storage.ApplicationData.Current.LocalSettings;
        if (settings.Values.TryGetValue(TransformKey(key), out var value) && value is float result)
        {
            return result;
        }
        return 0f;
    }

    public int GetInt(string key)
    {
        var settings = global::Windows.Storage.ApplicationData.Current.LocalSettings;
        if (settings.Values.TryGetValue(TransformKey(key), out var value) && value is int result)
        {
            return result;
        }
        return 0;
    }

    public void SetBool(string key, bool value)
    {
        global::Windows.Storage.ApplicationData.Current.LocalSettings.Values[TransformKey(key)] = value;
    }

    public void SetString(string key, string value)
    {
        global::Windows.Storage.ApplicationData.Current.LocalSettings.Values[TransformKey(key)] = value;
    }

    public void SetFloat(string key, float value)
    {
        global::Windows.Storage.ApplicationData.Current.LocalSettings.Values[TransformKey(key)] = value;
    }

    public void SetInt(string key, int value)
    {
        global::Windows.Storage.ApplicationData.Current.LocalSettings.Values[TransformKey(key)] = value;
    }
}


