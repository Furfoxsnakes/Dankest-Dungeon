using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int playerLevel;
    public int playerHealth;
    public int playerExperience;
    // Add other relevant data fields here
}

public class SaveSystem : MonoBehaviour
{
    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "savefile.json");
    }

    public void SaveGame(SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveFilePath, json);
    }

    public SaveData LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return null; // or return a new SaveData instance with default values
    }
}