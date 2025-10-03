namespace CorUI.Services;

public interface IViewStorage
{
    bool GetBool(string key);
    string GetString(string key);
    float GetFloat(string key);
    int GetInt(string key);
    
    void SetBool(string key, bool value);
    void SetString(string key, string value);
    void SetFloat(string key, float value);
    void SetInt(string key, int value);
}