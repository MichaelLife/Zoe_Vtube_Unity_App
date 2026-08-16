using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    private string filePath;

    public static readonly string Keyword = "19048024";
    public bool EncryptData = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("More than one SaveManager in the scene");
        }

        filePath = Application.persistentDataPath + "/saveFile.json";
    }

    public void SaveData(SaveData data)
    {
        bool needsEncryption = EncryptData;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, needsEncryption ? EncryptDecrypt(json) : json);

        Debug.Log("Game saved in --> " + filePath);
    }

    public SaveData LoadData()
    {
        if (File.Exists(filePath))
        {
            bool needsDecryption = EncryptData;

            string json = File.ReadAllText(filePath);
            SaveData data = JsonUtility.FromJson<SaveData>(needsDecryption ? EncryptDecrypt(json) : json);

            Debug.Log("Game loaded");
            return data;
        }
        else
        {
            Debug.Log("Could not find saved game data, creating new saveFile");
            OSF_Script.instance.SaveData();
            return null;
        }
    }

    public string EncryptDecrypt(string data)
    {
        string result = "";

        for (int i = 0; i < data.Length; i++)
        {
            result += (char)(data[i] ^ Keyword[i % Keyword.Length]);
        }

        return result;
    }
}
