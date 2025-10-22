using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro; // 确保已导入 TextMeshPro

public class PhotoCamera : MonoBehaviour
{
    [Header("模拟相机参数")]
    [Range(100f, 3200f)] public float isoSimulation = 400f; // 模拟 ISO
    [Range(1.8f, 22f)] public float apertureSimulation = 5.6f; // 模拟光圈
    [Range(0.01f, 1f)] public float shutterSpeedSimulation = 0.1f; // 模拟快门
    [Range(10f, 100f)] public float focalLengthSimulation = 60f; // 模拟焦段

    [Header("URP Volume 控制")]
    public Volume cameraVolume; // 拖入场景中的 Volume GameObject
    
    [Header("UI 显示")]
    public TextMeshProUGUI isoText;
    public TextMeshProUGUI apertureText;
    public TextMeshProUGUI shutterText;
    public TextMeshProUGUI focalLengthText;
    public GameObject infoPanel; // 包含所有参数文本的 UI Panel

    [Header("拍照设置")]
    public string photoFolderName = "CapturedPhotos";
    private string savePath;
    
    private Camera cam;
    
    // URP Volume Overrides 变量
    private DepthOfField depthOfField;
    private MotionBlur motionBlur;
    private ColorAdjustments colorAdjustments; // 使用 ColorAdjustments 来控制曝光
    
    // URP 2022.3+ 中 TryGetSettings 已更名为 TryGet
    // TryGet<T> 负责获取 Volume Profile 中对应的 Volume Component Override
    void Awake()
    {
        cam = GetComponent<Camera>();
        savePath = Path.Combine(Application.persistentDataPath, photoFolderName);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // --- 修正点 1: 将 TryGetSettings 改为 TryGet ---
        if (cameraVolume != null && cameraVolume.profile != null)
        {
            cameraVolume.profile.TryGet<DepthOfField>(out depthOfField);
            cameraVolume.profile.TryGet<MotionBlur>(out motionBlur);
            
            // --- 修正点：获取 ColorAdjustments ---
            cameraVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments);
        }
        else
        {
            Debug.LogWarning("Volume 组件或 Profile 引用丢失！");
        }
    }

    void Update()
    {
        // 1. 更新焦段
        cam.fieldOfView = focalLengthSimulation;

        // 2. 处理按键输入
        bool isHoldingRightClick = Input.GetMouseButton(1);
        
        // 3. 实时更新 UI 和 Volume 参数
        if (isHoldingRightClick)
        {
            UpdateCameraFX();
            UpdateUI();
            if (infoPanel != null) infoPanel.SetActive(true);
        }
        else
        {
            if (infoPanel != null) infoPanel.SetActive(false);
        }

        // 4. 左键拍照
        if (Input.GetMouseButtonDown(0) && isHoldingRightClick)
        {
            TakePic();
        }
    }

    void UpdateCameraFX()
    {
        // 更新 Depth of Field (光圈)
        if (depthOfField != null && depthOfField.IsActive())
        {
            // Focus Distance 用于对焦距离，需要手动设置
            depthOfField.focusDistance.value = 10f; // 假设焦点固定在10米处
            
            // --- 修正点 2: 使用 Aperture 属性而不是 intensity ---
            // 注意：只有在 URP Volume Profile 中将 DepthOfField Mode 设置为 Bokeh 时，Aperture 属性才可用且有效。
            // f-stop 值越小，光圈越大，景深越浅（虚化越强）。
            if (depthOfField.mode.value == DepthOfFieldMode.Bokeh)
            {
                // 直接将模拟的光圈值赋给 Bokeh 的 Aperture 属性
                depthOfField.aperture.value = apertureSimulation;
            }
            // 如果使用 Gaussian 模式，则需要调整 start/end 距离来模拟景深，但不如 Bokeh 真实。
        }

        // --- 修正点：更新 ColorAdjustments (ISO/曝光) ---
        if (colorAdjustments != null && colorAdjustments.IsActive())
        {
            // EV = log2(ISO / 100)
            float ev = Mathf.Log(isoSimulation / 100f, 2f);
            
            // 在 URP 中，ColorAdjustments 的曝光控制属性通常直接是 Exposure
            // 我们使用反射来避免编译错误，但如果确定属性名，直接赋值更优
            // 官方文档显示其属性为 Exposure
            colorAdjustments.postExposure.value = ev;
        }

        // 更新 Motion Blur (快门)
        if (motionBlur != null && motionBlur.IsActive())
        {
            // 快门越慢 (shutterSpeedSimulation 越小)，运动模糊越强
            motionBlur.intensity.value = Mathf.Lerp(0.0f, 1.0f, 1f - shutterSpeedSimulation); 
        }
    }
    
    void UpdateUI()
    {
        if (isoText) isoText.text = $"ISO: {isoSimulation:F0}";
        if (apertureText) apertureText.text = $"光圈: f/{apertureSimulation:F1}";
        // 显示快门分数 (例如 1/100s)
        if (shutterText) shutterText.text = $"快门: 1/{Mathf.RoundToInt(1f / shutterSpeedSimulation):F0}s";
        if (focalLengthText) focalLengthText.text = $"焦段: {focalLengthSimulation:F0}mm";
    }

    public void TakePic()
    {
        // ... (拍照和保存逻辑保持不变) ...
        if (GalleryManager.Instance == null) return;

        // 1. 拍照 (使用 RenderTexture)
        int width = Screen.width;
        int height = Screen.height;
        RenderTexture rt = new RenderTexture(width, height, 24);
        
        cam.targetTexture = rt;
        cam.Render();
        
        RenderTexture.active = rt;
        Texture2D screenTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenTexture.Apply();
        
        cam.targetTexture = null;
        RenderTexture.active = null; 
        DestroyImmediate(rt); 

        // 2. 编码并保存文件
        byte[] bytes = screenTexture.EncodeToPNG();
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Shot_{timestamp}.png";
        string fullFilePath = Path.Combine(savePath, fileName);

        File.WriteAllBytes(fullFilePath, bytes);
        Destroy(screenTexture); 

        // 3. 保存数据
        PhotoEntry newEntry = new PhotoEntry
        {
            fileName = fileName,
            filePath = fullFilePath,
            captureDate = timestamp,
            isoSimulation = isoSimulation,
            apertureSimulation = apertureSimulation,
            shutterSpeedSimulation = shutterSpeedSimulation,
            focalLengthSimulation = focalLengthSimulation
        };

        GalleryManager.Instance.AddPhotoEntry(newEntry);
        
        // 4. 切换场景
        SceneManager.LoadScene("GalleryScene"); 
    }
}