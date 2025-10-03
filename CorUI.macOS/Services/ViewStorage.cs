using CorUI.Services;

namespace CorUI.macOS.Services;

public class ViewStorage : IViewStorage
{
    private static string TransformKey(string key)
    {
        return $"view-storage-{key}";
    }
    
    public bool GetBool(string key)
    {
        return NSUserDefaults.StandardUserDefaults.BoolForKey(TransformKey(key));
    }

    public string GetString(string key)
    {
        return NSUserDefaults.StandardUserDefaults.StringForKey(TransformKey(key)) ?? string.Empty;
    }

    public float GetFloat(string key)
    {
        return NSUserDefaults.StandardUserDefaults.FloatForKey(TransformKey(key));
    }

    public int GetInt(string key)
    {
        return (int)NSUserDefaults.StandardUserDefaults.IntForKey(TransformKey(key));
    }

    public void SetBool(string key, bool value)
    {
        NSUserDefaults.StandardUserDefaults.SetBool(value, TransformKey(key));
    }

    public void SetString(string key, string value)
    {
        NSUserDefaults.StandardUserDefaults.SetString(value, TransformKey(key));
    }

    public void SetFloat(string key, float value)
    {
        NSUserDefaults.StandardUserDefaults.SetFloat(value, TransformKey(key));
    }

    public void SetInt(string key, int value)
    {
        NSUserDefaults.StandardUserDefaults.SetInt(value, TransformKey(key));
    }
}