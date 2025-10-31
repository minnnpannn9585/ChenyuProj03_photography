using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using DG.Tweening;
using TMPro;
using System;

/// <summary>
/// VR照片显示控制器
/// 增强版PhotoFrameDisplayFade，专为VR环境优化
/// </summary>
public class VRPhotoDisplay : MonoBehaviour
{
    [Header("基本设置")]
    public string folderName = "CapturedPhotos";
    public float switchInterval = 5f;
    public float fadeDuration = 1f;
    public bool enableRandomOrder = true;

    [Header("VR增强设置")]
    public bool enableVRInteraction = true;
    public bool showPhotoInfo = true;
    public bool enableHoverEffects = true;
    public bool enable3DFrameEffects = true;

    [Header("交互设置")]
    public float hoverScale = 1.05f;
    public float hoverDuration = 0.3f;
    public Color hoverColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("照片信息")]
    public GameObject photoInfoPanel;
    public TMP_Text photoNameText;
    public TMP_Text photoDateText;
    public TMP_Text photoSettingsText;

    [Header("音效")]
    public AudioClip photoChangeSound;
    public AudioClip hoverSound;
    public AudioSource audioSource;

    [Header("灯光效果")]
    public Light frameLight;
    public float lightIntensity = 2f;
    public Color lightColor = Color.white;
    public bool enableLightPulse = true;

    // 私有变量
    private Texture2D[] photos = new Texture2D[0];
    private int currentIndex = 0;
    private Renderer frameRenderer;
    private Material frameMaterialInstance;
    private string[] photoNames = new string[0];
    private string[] photoDates = new string[0];

    // 交互状态
    private bool isHovered = false;
    private bool isPlaying = false;
    private Coroutine switchCoroutine;
    private Coroutine hoverCoroutine;
    private Coroutine lightPulseCoroutine;

    // 原始状态
    private Vector3 originalScale;
    private Color originalColor;
    private float originalLightIntensity;

    // 事件
    public System.Action<Texture2D> OnPhotoChanged;
    public System.Action<string> OnPhotoInfoDisplayed;

    void Start()
    {
        InitializeController();
        LoadPhotos();
        StartPhotoPlayback();
    }

    /// <summary>
    /// 初始化控制器
    /// </summary>
    private void InitializeController()
    {
        // 获取渲染器
        frameRenderer = GetComponent<Renderer>();
        if (frameRenderer == null)
        {
            frameRenderer = GetComponentInChildren<Renderer>();
        }

        if (frameRenderer == null)
        {
            Debug.LogError("VRPhotoDisplay: No Renderer found!");
            enabled = false;
            return;
        }

        // 创建材质实例
        frameMaterialInstance = Instantiate(frameRenderer.material);
        frameRenderer.material = frameMaterialInstance;

        // 保存原始状态
        originalScale = transform.localScale;
        originalColor = frameMaterialInstance.color;

        // 初始化音频源
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 初始化灯光
        if (frameLight != null)
        {
            originalLightIntensity = frameLight.intensity;
            frameLight.color = lightColor;
        }

        Debug.Log($"VRPhotoDisplay initialized for {gameObject.name}");
    }

    /// <summary>
    /// 加载照片
    /// </summary>
    private void LoadPhotos()
    {
        string directory = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.LogWarning($"VRPhotoDisplay: Created directory {directory}");
        }

        // 获取所有jpg文件
        string[] files = Directory.GetFiles(directory, "*.jpg");

        // 如果没有照片，创建默认照片列表
        if (files.Length == 0)
        {
            Debug.LogWarning($"VRPhotoDisplay: No photos found in {directory}");
            return;
        }

        // 加载照片信息
        photos = new Texture2D[files.Length];
        photoNames = new string[files.Length];
        photoDates = new string[files.Length];

        for (int i = 0; i < files.Length; i++)
        {
            try
            {
                byte[] data = File.ReadAllBytes(files[i]);
                Texture2D texture = new Texture2D(2, 2);

                if (texture.LoadImage(data))
                {
                    photos[i] = texture;

                    // 提取文件信息
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    photoNames[i] = FormatPhotoName(fileName);
                    photoDates[i] = GetPhotoDate(files[i]);
                }
                else
                {
                    Debug.LogWarning($"VRPhotoDisplay: Failed to load photo {files[i]}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"VRPhotoDisplay: Error loading photo {files[i]}: {ex.Message}");
            }
        }

        Debug.Log($"VRPhotoDisplay: Loaded {photos.Length} photos from {directory}");

        // 如果启用随机顺序，打乱数组
        if (enableRandomOrder && photos.Length > 1)
        {
            ShuffleArray(photos);
            ShuffleArray(photoNames);
            ShuffleArray(photoDates);
        }
    }

    /// <summary>
    /// 格式化照片名称
    /// </summary>
    private string FormatPhotoName(string fileName)
    {
        // 移除时间戳前缀，只保留有意义的名称
        if (fileName.StartsWith("photo_"))
        {
            string[] parts = fileName.Split('_');
            if (parts.Length >= 2)
            {
                return string.Join("_", parts, 1, parts.Length - 1);
            }
        }
        return fileName;
    }

    /// <summary>
    /// 获取照片日期
    /// </summary>
    private string GetPhotoDate(string filePath)
    {
        try
        {
            DateTime fileTime = File.GetLastWriteTime(filePath);
            return fileTime.ToString("yyyy年MM月dd日 HH:mm");
        }
        catch
        {
            return "未知日期";
        }
    }

    /// <summary>
    /// 打乱数组
    /// </summary>
    private void ShuffleArray<T>(T[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, array.Length);
            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 开始照片播放
    /// </summary>
    private void StartPhotoPlayback()
    {
        if (photos.Length == 0) return;

        isPlaying = true;

        // 选择起始照片
        if (enableRandomOrder)
        {
            currentIndex = UnityEngine.Random.Range(0, photos.Length);
        }
        else
        {
            currentIndex = 0;
        }

        // 显示第一张照片
        SetTexture(photos[currentIndex]);
        ShowPhotoInfo(currentIndex);

        // 开始切换协程
        switchCoroutine = StartCoroutine(PhotoSwitchLoop());
    }

    /// <summary>
    /// 照片切换循环
    /// </summary>
    private IEnumerator PhotoSwitchLoop()
    {
        while (isPlaying && photos.Length > 1)
        {
            yield return new WaitForSeconds(switchInterval);

            // 淡出
            yield return StartCoroutine(FadeAlpha(1f, 0f, fadeDuration));

            // 切换到下一张照片
            currentIndex = (currentIndex + 1) % photos.Length;
            SetTexture(photos[currentIndex]);
            ShowPhotoInfo(currentIndex);

            // 淡入
            yield return StartCoroutine(FadeAlpha(0f, 1f, fadeDuration));

            // 触发事件
            OnPhotoChanged?.Invoke(photos[currentIndex]);
        }
    }

    /// <summary>
    /// 淡入淡出协程
    /// </summary>
    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float alpha = Mathf.Lerp(from, to, t);

            SetMaterialAlpha(alpha);

            // 同时调整灯光强度
            if (frameLight != null)
            {
                float lightIntensity = Mathf.Lerp(originalLightIntensity, 0f, t);
                frameLight.intensity = lightIntensity;
            }

            yield return null;
        }

        SetMaterialAlpha(to);

        // 淡入后重新启动灯光脉冲
        if (to > 0f && enableLightPulse)
        {
            StartLightPulse();
        }
    }

    /// <summary>
    /// 设置材质透明度
    /// </summary>
    private void SetMaterialAlpha(float alpha)
    {
        if (frameMaterialInstance.HasProperty("_BaseColor"))
        {
            Color c = frameMaterialInstance.GetColor("_BaseColor");
            c.a = alpha;
            frameMaterialInstance.SetColor("_BaseColor", c);
        }
        else
        {
            Color c = frameMaterialInstance.color;
            c.a = alpha;
            frameMaterialInstance.color = c;
        }
    }

    /// <summary>
    /// 设置贴图
    /// </summary>
    private void SetTexture(Texture2D texture)
    {
        if (frameMaterialInstance.HasProperty("_BaseMap"))
        {
            frameMaterialInstance.SetTexture("_BaseMap", texture);
        }
        else
        {
            frameMaterialInstance.mainTexture = texture;
        }
    }

    /// <summary>
    /// 显示照片信息
    /// </summary>
    private void ShowPhotoInfo(int index)
    {
        if (!showPhotoInfo || photoInfoPanel == null) return;

        // 激活信息面板
        photoInfoPanel.SetActive(true);

        // 设置文本信息
        if (photoNameText != null && index < photoNames.Length)
        {
            photoNameText.text = photoNames[index];
        }

        if (photoDateText != null && index < photoDates.Length)
        {
            photoDateText.text = photoDates[index];
        }

        // 设置拍摄设置信息
        if (photoSettingsText != null)
        {
            photoSettingsText.text = GetPhotoSettings(index);
        }

        // 触发事件
        OnPhotoInfoDisplayed?.Invoke(photoNames[index]);
    }

    /// <summary>
    /// 获取照片设置信息
    /// </summary>
    private string GetPhotoSettings(int index)
    {
        // 这里可以从文件名或元数据中提取拍摄设置
        // 暂时返回通用信息
        return "设置信息";
    }

    /// <summary>
    /// 开始灯光脉冲
    /// </summary>
    private void StartLightPulse()
    {
        if (lightPulseCoroutine != null)
        {
            StopCoroutine(lightPulseCoroutine);
        }

        lightPulseCoroutine = StartCoroutine(LightPulseCoroutine());
    }

    /// <summary>
    /// 灯光脉冲协程
    /// </summary>
    private IEnumerator LightPulseCoroutine()
    {
        float time = 0f;

        while (frameLight != null && isPlaying)
        {
            time += Time.deltaTime;

            // 使用正弦波创建脉冲效果
            float intensity = originalLightIntensity + Mathf.Sin(time * 2f) * 0.5f;
            frameLight.intensity = intensity;

            yield return null;
        }
    }

    /// <summary>
    /// 鼠标悬停事件（用于VR交互）
    /// </summary>
    public void OnHoverEnter()
    {
        if (!enableVRInteraction || !enableHoverEffects || isHovered) return;

        isHovered = true;

        // 开始悬停动画
        hoverCoroutine = StartCoroutine(HoverCoroutine(true));

        // 播放悬停音效
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound, 0.3f);
        }

        Debug.Log($"VRPhotoDisplay: Hover enter on {gameObject.name}");
    }

    /// <summary>
    /// 鼠标离开事件
    /// </summary>
    public void OnHoverExit()
    {
        if (!enableVRInteraction || !enableHoverEffects || !isHovered) return;

        isHovered = false;

        // 开始恢复动画
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }

        hoverCoroutine = StartCoroutine(HoverCoroutine(false));

        Debug.Log($"VRPhotoDisplay: Hover exit on {gameObject.name}");
    }

    /// <summary>
    /// 悬停协程
    /// </summary>
    private IEnumerator HoverCoroutine(bool isHovering)
    {
        Vector3 targetScale = isHovering ? originalScale * hoverScale : originalScale;
        Color targetColor = isHovering ? hoverColor : originalColor;

        float duration = isHovering ? hoverDuration * 0.5f : hoverDuration;

        // 缩放动画
        transform.DOScale(targetScale, duration)
            .SetEase(Ease.OutBack);

        // 颜色动画
        if (frameMaterialInstance != null)
        {
            frameMaterialInstance.DOColor(targetColor, duration);
        }

        // 灯光增强
        if (frameLight != null && isHovering)
        {
            frameLight.DOIntensity(originalLightIntensity * 1.5f, duration * 0.5f);
        }

        yield return new WaitForSeconds(duration);

        if (!isHovering && frameLight != null)
        {
            frameLight.DOIntensity(originalLightIntensity, duration * 0.5f);
        }
    }

    /// <summary>
    /// 手动切换到下一张照片
    /// </summary>
    public void SwitchToNextPhoto()
    {
        if (photos.Length <= 1) return;

        // 停止当前切换协程
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
        }

        // 播放切换音效
        if (photoChangeSound != null)
        {
            audioSource.PlayOneShot(photoChangeSound, 0.5f);
        }

        // 立即切换
        StartCoroutine(ImmediatePhotoSwitch());
    }

    /// <summary>
    /// 立即照片切换协程
    /// </summary>
    private IEnumerator ImmediatePhotoSwitch()
    {
        // 淡出
        yield return StartCoroutine(FadeAlpha(1f, 0f, fadeDuration * 0.5f));

        // 切换照片
        currentIndex = (currentIndex + 1) % photos.Length;
        SetTexture(photos[currentIndex]);
        ShowPhotoInfo(currentIndex);

        // 淡入
        yield return StartCoroutine(FadeAlpha(0f, 1f, fadeDuration * 0.5f));

        // 重新开始切换循环
        switchCoroutine = StartCoroutine(PhotoSwitchLoop());
    }

    /// <summary>
    /// 手动切换到指定照片
    /// </summary>
    public void SwitchToPhoto(int index)
    {
        if (index < 0 || index >= photos.Length) return;

        currentIndex = index;
        SetTexture(photos[index]);
        ShowPhotoInfo(index);

        OnPhotoChanged?.Invoke(photos[index]);
    }

    /// <summary>
    /// 暂停/恢复照片播放
    /// </summary>
    public void TogglePlayback()
    {
        isPlaying = !isPlaying;

        if (isPlaying)
        {
            // 恢复播放
            switchCoroutine = StartCoroutine(PhotoSwitchLoop());
            Debug.Log("VRPhotoDisplay: Photo playback resumed");
        }
        else
        {
            // 暂停播放
            if (switchCoroutine != null)
            {
                StopCoroutine(switchCoroutine);
            }
            Debug.Log("VRPhotoDisplay: Photo playback paused");
        }
    }

    /// <summary>
    /// 设置切换间隔
    /// </summary>
    public void SetSwitchInterval(float interval)
    {
        switchInterval = Mathf.Max(1f, interval);
    }

    /// <summary>
    /// 设置切换动画时长
    /// </summary>
    public void SetFadeDuration(float duration)
    {
        fadeDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// 启用/禁用随机顺序
    /// </summary>
    public void SetRandomOrder(bool enabled)
    {
        enableRandomOrder = enabled;
    }

    /// <summary>
    /// 获取当前照片数量
    /// </summary>
    public int GetPhotoCount()
    {
        return photos.Length;
    }

    /// <summary>
    /// 获取当前照片索引
    /// </summary>
    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    /// <summary>
    /// 获取当前照片
    /// </summary>
    public Texture2D GetCurrentPhoto()
    {
        return currentIndex < photos.Length ? photos[currentIndex] : null;
    }

    /// <summary>
    /// 重新加载照片
    /// </summary>
    public void ReloadPhotos()
    {
        // 停止当前播放
        isPlaying = false;
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
        }

        // 重新加载
        LoadPhotos();

        // 如果有照片，重新开始播放
        if (photos.Length > 0)
        {
            StartPhotoPlayback();
        }
    }

    /// <summary>
    /// 删除指定照片
    /// </summary>
    public void DeletePhoto(string photoName)
    {
        string directory = Path.Combine(Application.persistentDataPath, folderName);
        string filePath = Path.Combine(directory, photoName + ".jpg");

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"VRPhotoDisplay: Deleted photo {photoName}");
                ReloadPhotos();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"VRPhotoDisplay: Failed to delete photo {photoName}: {ex.Message}");
            }
        }
    }

    void OnDestroy()
    {
        // 清理材质实例
        if (frameMaterialInstance != null)
        {
            DestroyImmediate(frameMaterialInstance);
        }

        // 停止所有协程
        StopAllCoroutines();

        // 清理DOTween动画
        transform.DOKill();

        if (frameLight != null)
        {
            frameLight.DOKill();
        }
    }
}