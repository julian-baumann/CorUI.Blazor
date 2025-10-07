using CorUI.Services;
using System.Configuration;

namespace CorUI.Wpf;

public sealed class ViewStorage : IViewStorage
{
    private static string TransformKey(string key) => $"view-storage-{key}";

    public bool GetBool(string key)
    {
        return bool.TryParse(GetString(key), out var v) && v;
    }

    public string GetString(string key)
    {
        try
        {
            return ConfigurationManager.AppSettings[TransformKey(key)] ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public float GetFloat(string key)
    {
        return float.TryParse(GetString(key), out var v) ? v : 0f;
    }

    public int GetInt(string key)
    {
        return int.TryParse(GetString(key), out var v) ? v : 0;
    }

    public void SetBool(string key, bool value)
    {
        SetString(key, value ? "true" : "false");
    }

    public void SetString(string key, string value)
    {
        try
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            if (config.AppSettings.Settings[TransformKey(key)] is { } setting)
            {
                setting.Value = value;
            }
            else
            {
                config.AppSettings.Settings.Add(TransformKey(key), value);
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        catch
        {
            // ignore
        }
    }

    public void SetFloat(string key, float value)
    {
        SetString(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public void SetInt(string key, int value)
    {
        SetString(key, value.ToString());
    }
}


