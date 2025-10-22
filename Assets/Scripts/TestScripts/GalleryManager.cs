using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class PhotoEntry
{
    public string fileName;
    public string filePath;
    public string captureDate;

    // 模拟相机参数
    public float isoSimulation;
    public float apertureSimulation;
    public float shutterSpeedSimulation;
    public float focalLengthSimulation;
}

[System.Serializable]
public class GalleryData
{
    public List<PhotoEntry> photos = new List<PhotoEntry>();
}

public class GalleryManager : MonoBehaviour
{
    public static GalleryManager Instance;

    private GalleryData currentGalleryData = new GalleryData();
    private string jsonFileName = "photo_gallery_data.json";
    private string fullPath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        fullPath = Path.Combine(Application.persistentDataPath, jsonFileName);
        LoadGalleryData();
    }

    public void LoadGalleryData()
    {
        if (File.Exists(fullPath))
        {
            string jsonString = File.ReadAllText(fullPath);
            currentGalleryData = JsonUtility.FromJson<GalleryData>(jsonString);
            Debug.Log($"成功加载 {currentGalleryData.photos.Count} 张照片数据.");
        }
        else
        {
            currentGalleryData = new GalleryData();
        }
    }

    public void SaveGalleryData()
    {
        string jsonString = JsonUtility.ToJson(currentGalleryData, true);
        File.WriteAllText(fullPath, jsonString);
    }

    public void AddPhotoEntry(PhotoEntry newEntry)
    {
        currentGalleryData.photos.Add(newEntry);
        SaveGalleryData();
    }
    
    public GalleryData GetGalleryData()
    {
        return currentGalleryData;
    }
}