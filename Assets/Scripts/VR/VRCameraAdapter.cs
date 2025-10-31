using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

/// <summary>
/// VR相机适配器
/// 将VRCameraController与现有的CameraController和CameraModule预制件集成
/// 保持原有功能的同时添加VR交互
/// </summary>
public class VRCameraAdapter : MonoBehaviour
{
    [Header("组件引用")]
    public VRCameraController vrCameraController;
    public CameraController legacyCameraController;
    public VRCustomExposureController exposureController;

    [Header("UI适配")]
    public CameraModuleAdapter cameraModuleAdapter;
    public bool enableLegacyUI = true;
    public bool showVRUIOverlay = true;

    [Header("控制同步")]
    public bool syncParametersToLegacy = true;
    public bool syncParametersFromLegacy = false;
    public float syncInterval = 0.1f;

    [Header("交互模式")]
    public bool allowVRLegacySwitch = true;
    public KeyCode switchModeKey = KeyCode.Tab;

    // 状态管理
    private bool useVRControls = true;
    private bool isInitialized = false;
    private Coroutine syncCoroutine;

    // 原有UI组件引用
    private Slider legacyISOSlider;
    private Slider legacyApertureSlider;
    private Slider legacyShutterSlider;
    private Slider legacyFocalLengthSlider;
    private Slider legacyFocusDistanceSlider;

    // 原有预览组件
    private RawImage legacyPreviewUI;

    /// <summary>
    /// 相机模块适配器
    /// </summary>
    [System.Serializable]
    public class CameraModuleAdapter
    {
        [Header("Canvas组件")]
        public Canvas worldSpaceCanvas;
        public bool adaptCanvasForVR = true;
        public float vrCanvasScale = 0.001f;
        public Vector3 vrCanvasOffset = new Vector3(0, 0.05f, 0.1f);

        [Header("滑块交互")]
        public bool enableVRSliderInteraction = true;
        public float vrSliderSensitivity = 2f;
        public bool showSliderValueLabels = true;

        [Header("UI增强")]
        public bool add3DHighlights = true;
        public bool addHoverEffects = true;
        public float highlightIntensity = 2f;
    }

    void Start()
    {
        StartCoroutine(InitializeAdapter());
    }

    /// <summary>
    /// 初始化适配器
    /// </summary>
    private IEnumerator InitializeAdapter()
    {
        // 等待VR相机控制器初始化
        yield return new WaitForSeconds(0.2f);

        // 自动查找组件
        FindComponents();

        // 初始化适配功能
        InitializeCameraModule();
        InitializeParameterSync();
        InitializeUIEnhancements();

        // 设置事件监听
        SetupEventListeners();

        isInitialized = true;
        Debug.Log("VRCameraAdapter initialized successfully");

        // 启动参数同步
        if (syncCoroutine == null)
        {
            syncCoroutine = StartCoroutine(ParameterSyncCoroutine());
        }
    }

    /// <summary>
    /// 查找组件
    /// </summary>
    private void FindComponents()
    {
        // 查找VR相机控制器
        if (vrCameraController == null)
        {
            vrCameraController = FindObjectOfType<VRCameraController>();
        }

        // 查找传统相机控制器
        if (legacyCameraController == null)
        {
            legacyCameraController = FindObjectOfType<CameraController>();
        }

        // 查找曝光控制器
        if (exposureController == null)
        {
            exposureController = FindObjectOfType<VRCustomExposureController>();
        }

        // 如果没有找到VR组件，创建它们
        if (vrCameraController == null && gameObject != null)
        {
            vrCameraController = gameObject.AddComponent<VRCameraController>();
        }

        if (exposureController == null && gameObject != null)
        {
            exposureController = gameObject.AddComponent<VRCustomExposureController>();
        }

        Debug.Log($"Components found - VR: {vrCameraController != null}, Legacy: {legacyCameraController != null}, Exposure: {exposureController != null}");
    }

    /// <summary>
    /// 初始化相机模块适配
    /// </summary>
    private void InitializeCameraModule()
    {
        if (cameraModuleAdapter == null || cameraModuleAdapter.worldSpaceCanvas == null)
        {
            // 查找CameraModule预制件的Canvas
            GameObject cameraModule = GameObject.Find("CameraModule");
            if (cameraModule != null)
            {
                Transform canvasTransform = cameraModule.transform.Find("Module/WorldSpaceCanvas");
                if (canvasTransform != null)
                {
                    if (cameraModuleAdapter == null)
                    {
                        cameraModuleAdapter = new CameraModuleAdapter();
                    }
                    cameraModuleAdapter.worldSpaceCanvas = canvasTransform.GetComponent<Canvas>();
                }
            }
        }

        if (cameraModuleAdapter?.worldSpaceCanvas != null && cameraModuleAdapter.adaptCanvasForVR)
        {
            AdaptCanvasForVR();
        }

        // 查找原有UI组件
        FindLegacyUIComponents();
    }

    /// <summary>
    /// 适配Canvas为VR
    /// </summary>
    private void AdaptCanvasForVR()
    {
        Canvas canvas = cameraModuleAdapter.worldSpaceCanvas;

        if (vrCameraController?.cameraModel != null)
        {
            // 将Canvas附加到相机模型
            canvas.transform.SetParent(vrCameraController.cameraModel.transform);
            canvas.transform.localPosition = cameraModuleAdapter.vrCanvasOffset;
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one * cameraModuleAdapter.vrCanvasScale;
        }

        // 启用VR交互
        if (cameraModuleAdapter.enableVRSliderInteraction)
        {
            EnableVRSliderInteraction();
        }
    }

    /// <summary>
    /// 启用VR滑块交互
    /// </summary>
    private void EnableVRSliderInteraction()
    {
        Slider[] sliders = cameraModuleAdapter.worldSpaceCanvas.GetComponentsInChildren<Slider>();

        foreach (var slider in sliders)
        {
            // 添加VR交互组件
            VRSliderInteraction vrInteraction = slider.gameObject.GetComponent<VRSliderInteraction>();
            if (vrInteraction == null)
            {
                vrInteraction = slider.gameObject.AddComponent<VRSliderInteraction>();
            }

            vrInteraction.Initialize(slider, cameraModuleAdapter.vrSliderSensitivity);

            // 添加3D高亮效果
            if (cameraModuleAdapter.add3DHighlights)
            {
                AddSliderHighlight(slider);
            }
        }
    }

    /// <summary>
    /// 添加滑块高亮效果
    /// </summary>
    private void AddSliderHighlight(Slider slider)
    {
        // 为滑块添加发光效果
        Image handleImage = slider.handleRect?.GetComponent<Image>();
        if (handleImage != null)
        {
            // 这里可以添加材质或Shader来实现高亮效果
            // 暂时使用颜色变化
            Color originalColor = handleImage.color;

            // 可以通过事件来控制高亮
            // slider.onValueChanged.AddListener((value) => {
            //     handleImage.color = Color.Lerp(originalColor, Color.yellow, 0.5f);
            // });
        }
    }

    /// <summary>
    /// 查找原有UI组件
    /// </summary>
    private void FindLegacyUIComponents()
    {
        if (cameraModuleAdapter?.worldSpaceCanvas == null) return;

        // 查找所有滑块
        Slider[] sliders = cameraModuleAdapter.worldSpaceCanvas.GetComponentsInChildren<Slider>();
        foreach (var slider in sliders)
        {
            // 根据滑块名称识别功能
            if (slider.name.ToLower().Contains("iso") || slider.name.Contains("1"))
            {
                legacyISOSlider = slider;
            }
            else if (slider.name.ToLower().Contains("aperture") || slider.name.Contains("2"))
            {
                legacyApertureSlider = slider;
            }
            else if (slider.name.ToLower().Contains("shutter"))
            {
                legacyShutterSlider = slider;
            }
            else if (slider.name.ToLower().Contains("fl") || slider.name.Contains("4"))
            {
                legacyFocalLengthSlider = slider;
            }
            else if (slider.name.ToLower().Contains("fd") || slider.name.Contains("3"))
            {
                legacyFocusDistanceSlider = slider;
            }
        }

        // 查找预览UI
        RawImage[] rawImages = cameraModuleAdapter.worldSpaceCanvas.GetComponentsInChildren<RawImage>();
        foreach (var rawImage in rawImages)
        {
            if (rawImage.name.ToLower().Contains("preview"))
            {
                legacyPreviewUI = rawImage;
                break;
            }
        }

        Debug.Log($"Legacy UI components found - ISO: {legacyISOSlider != null}, Aperture: {legacyApertureSlider != null}, Preview: {legacyPreviewUI != null}");
    }

    /// <summary>
    /// 初始化参数同步
    /// </summary>
    private void InitializeParameterSync()
    {
        if (vrCameraController != null && legacyCameraController != null)
        {
            // 将VR相机控制器的相机引用设置为传统控制器的相机
            if (legacyCameraController.photographyCamera != null)
            {
                vrCameraController.photographyCamera = legacyCameraController.photographyCamera;
                vrCameraController.previewUI = legacyCameraController.previewUI;
            }

            // 复制滑块引用
            if (legacyCameraController.isoSlider != null)
                vrCameraController.isoSlider = legacyCameraController.isoSlider;
            if (legacyCameraController.apertureSlider != null)
                vrCameraController.apertureSlider = legacyCameraController.apertureSlider;
            if (legacyCameraController.shutterSlider != null)
                vrCameraController.shutterSlider = legacyCameraController.shutterSlider;
            if (legacyCameraController.focalLengthSlider != null)
                vrCameraController.focalLengthSlider = legacyCameraController.focalLengthSlider;
            if (legacyCameraController.focusDistanceSlider != null)
                vrCameraController.focusDistanceSlider = legacyCameraController.focusDistanceSlider;
        }

        // 设置曝光控制器的相机引用
        if (exposureController != null && vrCameraController != null)
        {
            exposureController.SetVRCameraController(vrCameraController);
        }
    }

    /// <summary>
    /// 初始化UI增强
    /// </summary>
    private void InitializeUIEnhancements()
    {
        if (!showVRUIOverlay) return;

        // 这里可以添加VR特定的UI增强功能
        // 比如3D按钮、手势提示等
    }

    /// <summary>
    /// 设置事件监听
    /// </summary>
    private void SetupEventListeners()
    {
        // 监听VR相机控制器的参数变化
        if (vrCameraController != null)
        {
            vrCameraController.OnParameterChanged.AddListener(OnVRParameterChanged);
        }

        // 监听曝光控制器的事件
        if (exposureController != null)
        {
            exposureController.OnExposureChanged += OnExposureChanged;
            exposureController.OnDepthOfFieldChanged += OnDepthOfFieldChanged;
            exposureController.OnMotionBlurChanged += OnMotionBlurChanged;
        }
    }

    /// <summary>
    /// VR参数变化事件
    /// </summary>
    private void OnVRParameterChanged()
    {
        if (!syncParametersToLegacy || legacyCameraController == null) return;

        // 同步参数到传统控制器
        SyncVRToLegacy();
    }

    /// <summary>
    /// 曝光变化事件
    /// </summary>
    private void OnExposureChanged(float exposureValue)
    {
        // 可以在这里添加UI反馈或视觉效果
        Debug.Log($"Exposure compensation: {exposureValue:F2}");
    }

    /// <summary>
    /// 景深变化事件
    /// </summary>
    private void OnDepthOfFieldChanged(float apertureValue)
    {
        // 可以在这里添加UI反馈或视觉效果
        Debug.Log($"Depth of field aperture: f/{apertureValue:F1}");
    }

    /// <summary>
    /// 动态模糊变化事件
    /// </summary>
    private void OnMotionBlurChanged(float blurIntensity)
    {
        // 可以在这里添加UI反馈或视觉效果
        Debug.Log($"Motion blur intensity: {blurIntensity:F2}");
    }

    /// <summary>
    /// 参数同步协程
    /// </summary>
    private IEnumerator ParameterSyncCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(syncInterval);

            if (syncParametersFromLegacy && legacyCameraController != null && vrCameraController != null)
            {
                SyncLegacyToVR();
            }
        }
    }

    /// <summary>
    /// 从VR同步到传统
    /// </summary>
    private void SyncVRToLegacy()
    {
        if (vrCameraController?.photographyCamera == null || legacyCameraController?.photographyCamera == null) return;

        // 同步相机参数
        legacyCameraController.photographyCamera.iso = vrCameraController.photographyCamera.iso;
        legacyCameraController.photographyCamera.aperture = vrCameraController.photographyCamera.aperture;
        legacyCameraController.photographyCamera.shutterSpeed = vrCameraController.photographyCamera.shutterSpeed;
        legacyCameraController.photographyCamera.focalLength = vrCameraController.photographyCamera.focalLength;
        legacyCameraController.photographyCamera.focusDistance = vrCameraController.photographyCamera.focusDistance;

        // 同步滑块值
        if (legacyCameraController.isoSlider != null && vrCameraController.isoSlider != null)
            legacyCameraController.isoSlider.value = vrCameraController.isoSlider.value;
        if (legacyCameraController.apertureSlider != null && vrCameraController.apertureSlider != null)
            legacyCameraController.apertureSlider.value = vrCameraController.apertureSlider.value;
        if (legacyCameraController.shutterSlider != null && vrCameraController.shutterSlider != null)
            legacyCameraController.shutterSlider.value = vrCameraController.shutterSlider.value;
        if (legacyCameraController.focalLengthSlider != null && vrCameraController.focalLengthSlider != null)
            legacyCameraController.focalLengthSlider.value = vrCameraController.focalLengthSlider.value;
        if (legacyCameraController.focusDistanceSlider != null && vrCameraController.focusDistanceSlider != null)
            legacyCameraController.focusDistanceSlider.value = vrCameraController.focusDistanceSlider.value;
    }

    /// <summary>
    /// 从传统同步到VR
    /// </summary>
    private void SyncLegacyToVR()
    {
        if (legacyCameraController?.photographyCamera == null || vrCameraController?.photographyCamera == null) return;

        // 同步相机参数
        vrCameraController.photographyCamera.iso = legacyCameraController.photographyCamera.iso;
        vrCameraController.photographyCamera.aperture = legacyCameraController.photographyCamera.aperture;
        vrCameraController.photographyCamera.shutterSpeed = legacyCameraController.photographyCamera.shutterSpeed;
        vrCameraController.photographyCamera.focalLength = legacyCameraController.photographyCamera.focalLength;
        vrCameraController.photographyCamera.focusDistance = legacyCameraController.photographyCamera.focusDistance;

        // 同步滑块值
        if (vrCameraController.isoSlider != null && legacyCameraController.isoSlider != null)
            vrCameraController.isoSlider.value = legacyCameraController.isoSlider.value;
        if (vrCameraController.apertureSlider != null && legacyCameraController.apertureSlider != null)
            vrCameraController.apertureSlider.value = legacyCameraController.apertureSlider.value;
        if (vrCameraController.shutterSlider != null && legacyCameraController.shutterSlider != null)
            vrCameraController.shutterSlider.value = legacyCameraController.shutterSlider.value;
        if (vrCameraController.focalLengthSlider != null && legacyCameraController.focalLengthSlider != null)
            vrCameraController.focalLengthSlider.value = legacyCameraController.focalLengthSlider.value;
        if (vrCameraController.focusDistanceSlider != null && legacyCameraController.focusDistanceSlider != null)
            vrCameraController.focusDistanceSlider.value = legacyCameraController.focusDistanceSlider.value;
    }

    /// <summary>
    /// 切换控制模式
    /// </summary>
    public void SwitchControlMode()
    {
        if (!allowVRLegacySwitch) return;

        useVRControls = !useVRControls;

        // 启用/禁用相应的控制
        if (vrCameraController != null)
        {
            vrCameraController.enabled = useVRControls;
        }

        if (legacyCameraController != null)
        {
            legacyCameraController.enabled = !useVRControls;
        }

        Debug.Log($"Switched to {(useVRControls ? "VR" : "Legacy")} controls");
    }

    /// <summary>
    /// 获取当前控制模式
    /// </summary>
    public bool IsUsingVRControls()
    {
        return useVRControls;
    }

    /// <summary>
    /// 强制同步所有参数
    /// </summary>
    public void ForceSyncParameters()
    {
        if (useVRControls)
        {
            SyncVRToLegacy();
        }
        else
        {
            SyncLegacyToVR();
        }
    }

    void Update()
    {
        // 检测模式切换快捷键
        if (allowVRLegacySwitch && Input.GetKeyDown(switchModeKey))
        {
            SwitchControlMode();
        }
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (vrCameraController != null)
        {
            vrCameraController.OnParameterChanged.RemoveListener(OnVRParameterChanged);
        }

        if (exposureController != null)
        {
            exposureController.OnExposureChanged -= OnExposureChanged;
            exposureController.OnDepthOfFieldChanged -= OnDepthOfFieldChanged;
            exposureController.OnMotionBlurChanged -= OnMotionBlurChanged;
        }

        // 停止协程
        if (syncCoroutine != null)
        {
            StopCoroutine(syncCoroutine);
        }
    }
}