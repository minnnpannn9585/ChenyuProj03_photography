using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PhotoFrameDisplay : MonoBehaviour
{
    public string folderName = "CapturedPhotos"; // 照片文件夹名称
    public float switchInterval = 5f;             // 每张图片显示时间（秒）

    private List<Texture2D> photos = new List<Texture2D>();
    private int currentIndex = 0;
    private Renderer frameRenderer;
    private float timer = 0f;

    void Start()
    {
        frameRenderer = GetComponent<Renderer>();
        LoadPhotos();
        if (photos.Count > 0)
        {
            SetTexture(photos[0]);
        }
    }

    void Update()
    {
        if (photos.Count <= 1) return;
        timer += Time.deltaTime;
        if (timer >= switchInterval)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % photos.Count;
            SetTexture(photos[currentIndex]);
        }
    }

    // 从 persistentDataPath 读取所有 jpg 文件
    void LoadPhotos()
    {
        string dir = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(dir))
        {
            Debug.LogWarning("Photo directory not found: " + dir);
            return;
        }
        string[] files = Directory.GetFiles(dir, "*.jpg");
        foreach (string file in files)
        {
            byte[] data = File.ReadAllBytes(file);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            photos.Add(tex);
        }
    }

    // 将指定纹理设置为材质的贴图
    void SetTexture(Texture2D tex)
    {
        if (frameRenderer != null)
        {
            if (frameRenderer.material.HasProperty("_BaseMap"))
                frameRenderer.material.SetTexture("_BaseMap", tex);
            else
                frameRenderer.material.mainTexture = tex;
        }
    }
}
