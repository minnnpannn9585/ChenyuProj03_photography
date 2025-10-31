using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;

/// <summary>
/// VR相机控制器
/// 替换现有CameraController，支持Quest3手柄交互
/// </summary>
public class VRCameraController : MonoBehaviour
{
    [Header("相机与预览")]
    public Camera photographyCamera;    // 摄影相机
    public RawImage previewUI;          // 用于显示预览的 RawImage

    [Header("UI组件")]
    public Canvas cameraCanvas;         // 相机UI画布
    public Slider isoSlider;
    public Slider apertureSlider;       // f 值滑块
    public Slider shutterSlider;        // 秒
    public Slider focalLengthSlider;    // mm
    public Slider focusDistanceSlider;
    public TMP_Text isoValueText;
    public TMP_Text apertureValueText;
    public TMP_Text shutterValueText;
    public TMP_Text focalLengthValueText;
    public TMP_Text focusDistanceValueText;
    public Image modeIndicator;         // 当前控制模式指示器

    [Header("手柄设置")]
    public Transform rightHandAnchor;   // 右手锚点（持相机）
    public Transform leftHandAnchor;    // 左手锚点
    public GameObject cameraModel;      // 相机3D模型

    [Header("控制参数")]
    public float parameterChangeSpeed = 50f;    // 参数变化速度
    public float focusControlSensitivity = 0.1f; // 对焦距离控制灵敏度
    public float zoomControlSensitivity = 5f;    // 焦距控制灵敏度

    [Header("拍照设置")]
    public int captureWidth = 1920;
    public int captureHeight = 1080;
    public string folderName = "CapturedPhotos";
    public float photoCaptureEffectDuration = 0.3f;
    public Image flashEffect;           // 拍照闪光效果

    [Header("菜单返回")]
    public float menuHoldDuration = 3f;
    public Image menuProgressBar;
    public GameObject menuFeedbackPanel;

    [Header("音效")]
    public AudioClip captureSound;
    public AudioClip parameterChangeSound;
    public AudioClip modeSwitchSound;
    public AudioSource audioSource;

    [Header("事件")]
    public UnityEvent OnPhotoCaptured = new UnityEvent();
    public UnityEvent OnParameterChanged = new UnityEvent();

    // 控制模式枚举
    public enum ControlMode
    {
        Normal,        // 正常模式 - Grab键控制焦段
        Focus,         // 对焦模式 - 按住X键
        Aperture,      // 光圈模式 - 按住Y键
        Shutter,       // 快门模式 - 按住A键
        ISO            // ISO模式 - 按住B键
    }

    // 私有变量
    private RenderTexture previewRT;
    private string saveDirectory;
    private ControlMode currentMode = ControlMode.Normal;
    private bool isMenuPressed = false;
    private float menuHoldTimer = 0f;
    private bool isCapturing = false;

    // 默认参数值
    private readonly Vector2 defaultISORange = new Vector2(50, 6400);
    private readonly Vector2 defaultApertureRange = new Vector2(1.4f, 22f);
    private readonly Vector2 defaultShutterRange = new Vector2(0.0001f, 1f);
    private readonly Vector2 defaultFocalLengthRange = new Vector2(24f, 200f);
    private readonly Vector2 defaultFocusDistanceRange = new Vector2(0.1f, 10f);

    void Start()
    {
        InitializeCamera();
        InitializeHandHolding();
        InitializeUI();
        InitializeAudio();
    }

    void Update()
    {
        HandleInput();
        HandleMenuReturn();
        UpdateModeIndicator();
    }

    /// <summary>
    /// 初始化相机
    /// </summary>
    private void InitializeCamera()
    {
        // 创建保存目录
        saveDirectory = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(saveDirectory))
            Directory.CreateDirectory(saveDirectory);

        // 创建用于实时预览的 RenderTexture
        previewRT = new RenderTexture(captureWidth, captureHeight, 16, RenderTextureFormat.ARGB32);
        photographyCamera.targetTexture = previewRT;
        previewUI.texture = previewRT;

        // 确保启用物理相机属性
        photographyCamera.usePhysicalProperties = true;

        // 设置滑块范围和默认值
        SetupSliders();

        // 初始化相机参数
        UpdateCameraParameters();

        Debug.Log("VRCameraController initialized");
    }

    /// <summary>
    /// 初始化手持相机
    /// </summary>
    private void InitializeHandHolding()
    {
        // 如果有相机模型，设置到右手位置
        if (cameraModel != null && rightHandAnchor != null)
        {
            cameraModel.transform.SetParent(rightHandAnchor);
            cameraModel.transform.localPosition = Vector3.zero;
            cameraModel.transform.localRotation = Quaternion.identity;
        }

        // 如果有相机模型，将摄影相机作为子相机
        if (cameraModel != null && photographyCamera != null)
        {
            photographyCamera.transform.SetParent(cameraModel.transform);
            photographyCamera.transform.localPosition = Vector3.zero;
            photographyCamera.transform.localRotation = Quaternion.identity;
        }

        // 如果UI画布存在，附加到相机模型
        if (cameraCanvas != null && cameraModel != null)
        {
            cameraCanvas.transform.SetParent(cameraModel.transform);
            cameraCanvas.transform.localPosition = new Vector3(0, 0.05f, 0.1f); // 在相机前方显示UI
            cameraCanvas.transform.localRotation = Quaternion.identity;
            cameraCanvas.transform.localScale = Vector3.one * 0.001f; // 缩小UI到合适大小
        }
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 设置滑块事件
        if (isoSlider != null)
        {
            isoSlider.onValueChanged.AddListener((value) => OnSliderChanged("ISO", value));
        }

        if (apertureSlider != null)
        {
            apertureSlider.onValueChanged.AddListener((value) => OnSliderChanged("Aperture", value));
        }

        if (shutterSlider != null)
        {
            shutterSlider.onValueChanged.AddListener((value) => OnSliderChanged("Shutter", value));
        }

        if (focalLengthSlider != null)
        {
            focalLengthSlider.onValueChanged.AddListener((value) => OnSliderChanged("FocalLength", value));
        }

        if (focusDistanceSlider != null)
        {
            focusDistanceSlider.onValueChanged.AddListener((value) => OnSliderChanged("FocusDistance", value));
        }

        // 隐藏菜单反馈面板
        if (menuFeedbackPanel != null)
        {
            menuFeedbackPanel.SetActive(false);
        }

        // 更新初始UI显示
        UpdateUIValues();
    }

    /// <summary>
    /// 初始化音效
    /// </summary>
    private void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 设置滑块
    /// </summary>
    private void SetupSliders()
    {
        if (isoSlider != null)
        {
            isoSlider.minValue = defaultISORange.x;
            isoSlider.maxValue = defaultISORange.y;
            isoSlider.value = 400f; // 默认ISO 400
        }

        if (apertureSlider != null)
        {
            apertureSlider.minValue = defaultApertureRange.x;
            apertureSlider.maxValue = defaultApertureRange.y;
            apertureSlider.value = 5.6f; // 默认f/5.6
        }

        if (shutterSlider != null)
        {
            shutterSlider.minValue = defaultShutterRange.x;
            shutterSlider.maxValue = defaultShutterRange.y;
            shutterSlider.value = 0.025f; // 默认1/40秒
        }

        if (focalLengthSlider != null)
        {
            focalLengthSlider.minValue = defaultFocalLengthRange.x;
            focalLengthSlider.maxValue = defaultFocalLengthRange.y;
            focalLengthSlider.value = 50f; // 默认50mm
        }

        if (focusDistanceSlider != null)
        {
            focusDistanceSlider.minValue = defaultFocusDistanceRange.x;
            focusDistanceSlider.maxValue = defaultFocusDistanceRange.y;
            focusDistanceSlider.value = 3f; // 默认3米
        }
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        // 检测模式切换按键
        if (OVRInput.GetDown(OVRInput.RawButton.X))
        {
            SetMode(ControlMode.Focus);
        }
        else if (OVRInput.GetUp(OVRInput.RawButton.X))
        {
            SetMode(ControlMode.Normal);
        }

        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            SetMode(ControlMode.Aperture);
        }
        else if (OVRInput.GetUp(OVRInput.RawButton.Y))
        {
            SetMode(ControlMode.Normal);
        }

        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            SetMode(ControlMode.Shutter);
        }
        else if (OVRInput.GetUp(OVRInput.RawButton.A))
        {
            SetMode(ControlMode.Normal);
        }

        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            SetMode(ControlMode.ISO);
        }
        else if (OVRInput.GetUp(OVRInput.RawButton.B))
        {
            SetMode(ControlMode.Normal);
        }

        // 处理Grab键的参数调整
        HandleParameterAdjustment();

        // 处理拍照（右手肩键）
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger) ||
            OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            CapturePhoto();
        }
    }

    /// <summary>
    /// 处理参数调整
    /// </summary>
    private void HandleParameterAdjustment()
    {
        float grabInput = 0f;

        // 检测左右手柄的Grab键
        bool leftGrab = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        bool rightGrab = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

        if (leftGrab && !rightGrab)
        {
            grabInput = -1f; // 左手减少
        }
        else if (!leftGrab && rightGrab)
        {
            grabInput = 1f; // 右手增加
        }
        else if (leftGrab && rightGrab)
        {
            // 两手同时按住，精细控制
            grabInput = 0.5f;
        }

        if (Mathf.Abs(grabInput) > 0.01f)
        {
            switch (currentMode)
            {
                case ControlMode.Normal:
                    // 正常模式：控制焦段（焦距）
                    AdjustFocalLength(grabInput * zoomControlSensitivity * Time.deltaTime);
                    break;
                case ControlMode.Focus:
                    // 对焦模式：控制对焦距离
                    AdjustFocusDistance(grabInput * focusControlSensitivity * Time.deltaTime);
                    break;
                case ControlMode.Aperture:
                    // 光圈模式：控制光圈
                    AdjustAperture(grabInput * parameterChangeSpeed * Time.deltaTime);
                    break;
                case ControlMode.Shutter:
                    // 快门模式：控制快门速度
                    AdjustShutterSpeed(grabInput * parameterChangeSpeed * Time.deltaTime);
                    break;
                case ControlMode.ISO:
                    // ISO模式：控制ISO
                    AdjustISO(grabInput * parameterChangeSpeed * Time.deltaTime);
                    break;
            }

            // 播放参数变化音效
            if (parameterChangeSound != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(parameterChangeSound, 0.3f);
            }
        }
    }

    /// <summary>
    /// 设置控制模式
    /// </summary>
    private void SetMode(ControlMode newMode)
    {
        if (currentMode != newMode)
        {
            currentMode = newMode;

            // 播放模式切换音效
            if (modeSwitchSound != null)
            {
                audioSource.PlayOneShot(modeSwitchSound);
            }

            // 高亮对应的滑块
            HighlightCurrentModeSlider();

            Debug.Log($"Camera control mode changed to: {newMode}");
        }
    }

    /// <summary>
    /// 高亮当前模式对应的滑块
    /// </summary>
    private void HighlightCurrentModeSlider()
    {
        // 重置所有滑块颜色
        ResetSliderColors();

        // 高亮当前模式的滑块
        Slider targetSlider = null;
        switch (currentMode)
        {
            case ControlMode.Normal:
                targetSlider = focalLengthSlider;
                break;
            case ControlMode.Focus:
                targetSlider = focusDistanceSlider;
                break;
            case ControlMode.Aperture:
                targetSlider = apertureSlider;
                break;
            case ControlMode.Shutter:
                targetSlider = shutterSlider;
                break;
            case ControlMode.ISO:
                targetSlider = isoSlider;
                break;
        }

        if (targetSlider != null)
        {
            // 使用DOTween创建高亮效果
            Image fillImage = targetSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.DOColor(Color.yellow, 0.2f);
            }
        }
    }

    /// <summary>
    /// 重置滑块颜色
    /// </summary>
    private void ResetSliderColors()
    {
        Slider[] sliders = { isoSlider, apertureSlider, shutterSlider, focalLengthSlider, focusDistanceSlider };

        foreach (var slider in sliders)
        {
            if (slider != null)
            {
                Image fillImage = slider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.DOColor(Color.white, 0.1f);
                }
            }
        }
    }

    /// <summary>
    /// 调整ISO
    /// </summary>
    private void AdjustISO(float delta)
    {
        if (isoSlider != null)
        {
            float newValue = Mathf.Clamp(isoSlider.value + delta, isoSlider.minValue, isoSlider.maxValue);
            isoSlider.value = newValue;
            OnSliderChanged("ISO", newValue);
        }
    }

    /// <summary>
    /// 调整光圈
    /// </summary>
    private void AdjustAperture(float delta)
    {
        if (apertureSlider != null)
        {
            float newValue = Mathf.Clamp(apertureSlider.value - delta, apertureSlider.minValue, apertureSlider.maxValue);
            apertureSlider.value = newValue;
            OnSliderChanged("Aperture", newValue);
        }
    }

    /// <summary>
    /// 调整快门速度
    /// </summary>
    private void AdjustShutterSpeed(float delta)
    {
        if (shutterSlider != null)
        {
            float newValue = Mathf.Clamp(shutterSlider.value - delta, shutterSlider.minValue, shutterSlider.maxValue);
            shutterSlider.value = newValue;
            OnSliderChanged("Shutter", newValue);
        }
    }

    /// <summary>
    /// 调整焦距
    /// </summary>
    private void AdjustFocalLength(float delta)
    {
        if (focalLengthSlider != null)
        {
            float newValue = Mathf.Clamp(focalLengthSlider.value + delta, focalLengthSlider.minValue, focalLengthSlider.maxValue);
            focalLengthSlider.value = newValue;
            OnSliderChanged("FocalLength", newValue);
        }
    }

    /// <summary>
    /// 调整对焦距离
    /// </summary>
    private void AdjustFocusDistance(float delta)
    {
        if (focusDistanceSlider != null)
        {
            float newValue = Mathf.Clamp(focusDistanceSlider.value + delta, focusDistanceSlider.minValue, focusDistanceSlider.maxValue);
            focusDistanceSlider.value = newValue;
            OnSliderChanged("FocusDistance", newValue);
        }
    }

    /// <summary>
    /// 滑块变化事件
    /// </summary>
    private void OnSliderChanged(string parameterName, float value)
    {
        UpdateCameraParameters();
        UpdateUIValues();
        OnParameterChanged.Invoke();
    }

    /// <summary>
    /// 更新相机参数
    /// </summary>
    private void UpdateCameraParameters()
    {
        if (photographyCamera == null) return;

        // 从滑块读取值并设置相机属性
        int isoValue = Mathf.Max(50, Mathf.RoundToInt(isoSlider?.value ?? 400));
        photographyCamera.iso = isoValue;

        float apertureValue = Mathf.Max(1.0f, apertureSlider?.value ?? 5.6f);
        photographyCamera.aperture = apertureValue;

        float shutterValue = Mathf.Max(0.0001f, shutterSlider?.value ?? 0.025f);
        photographyCamera.shutterSpeed = shutterValue;

        float focalLengthValue = Mathf.Max(1.0f, focalLengthSlider?.value ?? 50f);
        photographyCamera.focalLength = focalLengthValue;

        float focusDistanceValue = Mathf.Max(0.1f, focusDistanceSlider?.value ?? 3f);
        photographyCamera.focusDistance = focusDistanceValue;
    }

    /// <summary>
    /// 更新UI显示值
    /// </summary>
    private void UpdateUIValues()
    {
        if (isoValueText != null && isoSlider != null)
        {
            isoValueText.text = $"ISO: {Mathf.RoundToInt(isoSlider.value)}";
        }

        if (apertureValueText != null && apertureSlider != null)
        {
            apertureValueText.text = $"f/{apertureSlider.value:F1}";
        }

        if (shutterValueText != null && shutterSlider != null)
        {
            float shutter = shutterSlider.value;
            if (shutter >= 1f)
            {
                shutterValueText.text = $"{shutter:F1}s";
            }
            else
            {
                shutterValueText.text = $"1/{Mathf.RoundToInt(1f/shutter)}s";
            }
        }

        if (focalLengthValueText != null && focalLengthSlider != null)
        {
            focalLengthValueText.text = $"{focalLengthSlider.value:F0}mm";
        }

        if (focusDistanceValueText != null && focusDistanceSlider != null)
        {
            float distance = focusDistanceSlider.value;
            if (distance >= 1f)
            {
                focusDistanceValueText.text = $"{distance:F1}m";
            }
            else
            {
                focusDistanceValueText.text = $"{distance * 100:F0}cm";
            }
        }
    }

    /// <summary>
    /// 更新模式指示器
    /// </summary>
    private void UpdateModeIndicator()
    {
        if (modeIndicator == null) return;

        string modeText = "";
        Color modeColor = Color.white;

        switch (currentMode)
        {
            case ControlMode.Normal:
                modeText = "焦段";
                modeColor = Color.green;
                break;
            case ControlMode.Focus:
                modeText = "对焦";
                modeColor = Color.blue;
                break;
            case ControlMode.Aperture:
                modeText = "光圈";
                modeColor = Color.yellow;
                break;
            case ControlMode.Shutter:
                modeText = "快门";
                modeColor = Color.red;
                break;
            case ControlMode.ISO:
                modeText = "ISO";
                modeColor = Color.magenta;
                break;
        }

        // 更新指示器显示
        var textComponent = modeIndicator.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = modeText;
            textComponent.color = modeColor;
        }

        modeIndicator.color = new Color(modeColor.r, modeColor.g, modeColor.b, 0.8f);
    }

    /// <summary>
    /// 拍照并保存
    /// </summary>
    public void CapturePhoto()
    {
        if (isCapturing) return;

        StartCoroutine(CapturePhotoCoroutine());
    }

    /// <summary>
    /// 拍照协程
    /// </summary>
    private System.Collections.IEnumerator CapturePhotoCoroutine()
    {
        isCapturing = true;

        // 播放拍照音效
        if (captureSound != null)
        {
            audioSource.PlayOneShot(captureSound);
        }

        // 闪光效果
        if (flashEffect != null)
        {
            flashEffect.DOFade(1f, 0.1f).SetLoops(2, LoopType.Yoyo);
        }

        // 相机震动效果
        if (cameraModel != null)
        {
            cameraModel.transform.DOShakePosition(0.1f, 0.02f, 10, 90f);
        }

        yield return new WaitForSeconds(0.1f);

        // 执行实际的拍照操作
        string imageName = "photo_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        yield return StartCoroutine(CaptureAndSaveCoroutine(imageName));

        yield return new WaitForSeconds(photoCaptureEffectDuration - 0.1f);

        isCapturing = false;
        OnPhotoCaptured.Invoke();

        Debug.Log($"Photo captured: {imageName}");
    }

    /// <summary>
    /// 拍照并保存到磁盘
    /// </summary>
    private System.Collections.IEnumerator CaptureAndSaveCoroutine(string imageName)
    {
        // 使用临时RenderTexture捕捉画面
        RenderTexture tempRT = new RenderTexture(captureWidth, captureHeight, 16, RenderTextureFormat.ARGB32);
        photographyCamera.targetTexture = tempRT;
        photographyCamera.Render();

        yield return new WaitForEndOfFrame();

        RenderTexture.active = tempRT;
        Texture2D image = new Texture2D(captureWidth, captureHeight, TextureFormat.ARGB32, false);
        image.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        image.Apply();

        // 恢复实时预览
        photographyCamera.targetTexture = previewRT;
        RenderTexture.active = null;
        tempRT.Release();

        // 保存图片
        byte[] bytes = image.EncodeToJPG(90); // 90%质量
        if (bytes != null)
        {
            string savePath = Path.Combine(saveDirectory, imageName + ".jpg");
            File.WriteAllBytes(savePath, bytes);
            Debug.Log("Saved photo to: " + savePath);
        }
        else
        {
            Debug.LogError("Failed to encode image.");
        }

        // 释放临时贴图
        Destroy(image);
    }

    /// <summary>
    /// 处理菜单返回功能
    /// </summary>
    private void HandleMenuReturn()
    {
        // 检测菜单键按下
        bool menuPressed = OVRInput.Get(OVRInput.Button.Start) ||
                          OVRInput.Get(OVRInput.Button.PrimaryHandTrigger) &&
                          OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

        if (menuPressed && !isMenuPressed)
        {
            isMenuPressed = true;
            menuHoldTimer = 0f;

            // 显示反馈面板
            if (menuFeedbackPanel != null)
            {
                menuFeedbackPanel.SetActive(true);
                var feedbackText = menuFeedbackPanel.GetComponentInChildren<TMP_Text>();
                if (feedbackText != null)
                {
                    feedbackText.text = "按住返回主菜单...";
                }

                if (menuProgressBar != null)
                {
                    menuProgressBar.fillAmount = 0f;
                }
            }
        }
        else if (!menuPressed && isMenuPressed)
        {
            isMenuPressed = false;

            // 隐藏反馈面板
            if (menuFeedbackPanel != null)
            {
                menuFeedbackPanel.SetActive(false);
            }

            menuHoldTimer = 0f;
        }

        // 持续按住菜单键
        if (isMenuPressed)
        {
            menuHoldTimer += Time.deltaTime;

            // 更新进度条
            if (menuProgressBar != null)
            {
                menuProgressBar.fillAmount = menuHoldTimer / menuHoldDuration;
            }

            // 检查是否达到返回时间
            if (menuHoldTimer >= menuHoldDuration)
            {
                ReturnToMainMenu();
            }
        }
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    private void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu from photo scene");

        // 重置计时器
        menuHoldTimer = 0f;
        isMenuPressed = false;

        // 隐藏反馈面板
        if (menuFeedbackPanel != null)
        {
            menuFeedbackPanel.SetActive(false);
        }

        // 返回主菜单
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
    }

    /// <summary>
    /// 获取当前控制模式
    /// </summary>
    public ControlMode GetCurrentMode()
    {
        return currentMode;
    }

    /// <summary>
    /// 设置相机到手部
    /// </summary>
    public void AttachToHand(Transform handTransform)
    {
        if (cameraModel != null && handTransform != null)
        {
            cameraModel.transform.SetParent(handTransform);
            cameraModel.transform.localPosition = Vector3.zero;
            cameraModel.transform.localRotation = Quaternion.identity;
        }
    }

    void OnDestroy()
    {
        // 清理DOTween动画
        transform.DOKill();

        // 清理RenderTexture
        if (previewRT != null)
        {
            previewRT.Release();
        }
    }
}