using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR博物馆快速设置脚本
/// 一键配置Museum场景支持VR
/// </summary>
public class VRMuseumQuickSetup : MonoBehaviour
{
    [Header("自动配置")]
    public bool autoSetupOnStart = true;
    public bool findPhotoDisplays = true;
    public bool addVRLocomotion = true;
    public bool addVRMuseumController = true;

    [Header("查找设置")]
    public string photoFrameTag = "PictureFrame";
    public bool searchInAllObjects = true;

    [Header("VR组件设置")]
    public bool addLocomotionController = true;
    public VRLocomotionController.LocomotionType defaultLocomotionType = VRLocomotionController.LocomotionType.Hybrid;
    public float defaultMoveSpeed = 3f;

    [Header("UI设置")]
    public bool createInfoCanvas = true;
    public bool showWelcomeMessage = true;
    public bool showControlsPanel = true;
    public bool showStatsPanel = true;
    public bool enableDebugInfo = false;

    [Header("交互设置")]
    public bool enablePhotoInteractions = true;
    public bool enableHoverEffects = true;
    public bool enable3DFrameEffects = true;

    [Header("音效设置")]
    public bool addAudioSource = true;
    public AudioClip defaultClickSound;

    // 找到的组件引用
    private GameObject museumRoot;
    private VRPhotoDisplay[] photoDisplays;
    private Camera mainCamera;
    private CharacterController characterController;
    private VRLocomotionController locomotionController;
    private VRMuseumController museumController;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupVRForMuseum();
        }
    }

    /// <summary>
    /// 为博物馆场景设置VR功能
    /// </summary>
    [ContextMenu("Setup VR for Museum")]
    public void SetupVRForMuseum()
    {
        Debug.Log("开始VR博物馆快速设置...");

        // 1. 查找博物馆根对象
        if (!FindMuseumRoot())
        {
            Debug.LogError("VRMuseumQuickSetup: 未找到Museum场景根对象，请确保在Museum场景中使用");
            return;
        }

        // 2. 查找照片显示组件
        if (findPhotoDisplays)
        {
            FindPhotoDisplays();
        }

        // 3. 添加VR移动系统
        if (addVRLocomotion)
        {
            AddVRLocomotion();
        }

        // 4. 替换照片显示组件
        if (photoDisplays != null && photoDisplays.Length > 0)
        {
            UpgradePhotoDisplays();
        }

        // 5. 添加VR博物馆控制器
        if (addVRMuseumController)
        {
            AddVRMuseumController();
        }

        // 6. 创建UI界面
        if (createInfoCanvas)
        {
            CreateInfoCanvas();
        }

        // 7. 添加音频源
        if (addAudioSource)
        {
            AddAudioSource();
        }

        // 8. 配置玩家位置
        SetupPlayerPosition();

        Debug.Log("VR博物馆快速设置完成！");
        PrintSetupSummary();
    }

    /// <summary>
    /// 查找博物馆根对象
    /// </summary>
    private bool FindMuseumRoot()
    {
        // 首先尝试查找名称包含"Museum"的对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Museum") || obj.scene.name.Contains("Museum"))
            {
                museumRoot = obj;
                Debug.Log($"VRMuseumQuickSetup: 找到博物馆根对象: {obj.name}");
                break;
            }
        }

        // 如果没找到，使用当前对象
        if (museumRoot == null)
        {
            museumRoot = gameObject;
            Debug.Log($"VRMuseumQuickSetup: 使用当前对象作为博物馆根对象: {gameObject.name}");
        }

        return museumRoot != null;
    }

    /// <summary>
    /// 查找照片显示组件
    /// </summary>
    private void FindPhotoDisplays()
    {
        if (searchInAllObjects)
        {
            // 在所有对象中查找
            PhotoFrameDisplayFade[] legacyDisplays = FindObjectsOfType<PhotoFrameDisplayFade>();
            var convertedList = new System.Collections.Generic.List<VRPhotoDisplay>();

            foreach (var legacyDisplay in legacyDisplays)
            {
                // 将旧的组件替换为新的VR组件
                VRPhotoDisplay vrDisplay = legacyDisplay.gameObject.GetComponent<VRPhotoDisplay>();
                if (vrDisplay == null)
                {
                    vrDisplay = legacyDisplay.gameObject.AddComponent<VRPhotoDisplay>();
                }

                // 复制设置
                vrDisplay.folderName = legacyDisplay.folderName;
                vrDisplay.switchInterval = legacyDisplay.switchInterval;
                vrDisplay.fadeDuration = legacyDisplay.fadeDuration;

                convertedList.Add(vrDisplay);
            }

            photoDisplays = convertedList.ToArray();
        }
        else
        {
            // 在子对象中查找
            if (museumRoot != null)
            {
                photoDisplays = museumRoot.GetComponentsInChildren<VRPhotoDisplay>();
            }

            // 如果还是没找到，查找旧组件并转换
            if (photoDisplays == null || photoDisplays.Length == 0)
            {
                PhotoFrameDisplayFade[] legacyDisplays = museumRoot.GetComponentsInChildren<PhotoFrameDisplayFade>();
                var convertedList = new System.Collections.Generic.List<VRPhotoDisplay>();

                foreach (var legacyDisplay in legacyDisplays)
                {
                    VRPhotoDisplay vrDisplay = legacyDisplay.gameObject.GetComponent<VRPhotoDisplay>();
                    if (vrDisplay == null)
                    {
                        vrDisplay = legacyDisplay.gameObject.AddComponent<VRPhotoDisplay>();
                    }

                    vrDisplay.folderName = legacyDisplay.folderName;
                    vrDisplay.switchInterval = legacyDisplay.switchInterval;
                    vrDisplay.fadeDuration = legacyDisplay.fadeDuration;

                    convertedList.Add(vrDisplay);
                }

                photoDisplays = convertedList.ToArray();
            }
        }

        Debug.Log($"VRMuseumQuickSetup: 找到 {photoDisplays.Length} 个照片显示组件");
    }

    /// <summary>
    /// 添加VR移动系统
    /// </summary>
    private void AddVRLocomotion()
    {
        if (museumRoot == null) return;

        VRLocomotionController locomotion = museumRoot.GetComponent<VRLocomotionController>();
        if (locomotion == null)
        {
            locomotion = museumRoot.AddComponent<VRLocomotionController>();
        }

        // 配置移动设置
        locomotion.locomotionType = defaultLocomotionType;
        locomotion.moveSpeed = defaultMoveSpeed;

        // 查找或添加CharacterController
        characterController = museumRoot.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = museumRoot.AddComponent<CharacterController>();
            characterController.radius = 0.3f;
            characterController.height = 1.8f;
            characterController.center = Vector3.up * 0.9f;
        }

        // 查找手柄锚点
        SetupHandAnchors();

        Debug.Log("VRMuseumQuickSetup: 已添加VR移动系统");
    }

    /// <summary>
    /// 设置手柄锚点
    /// </summary>
    private void SetupHandAnchors()
    {
        if (locomotionController == null) return;

        // 自动查找手柄锚点
        GameObject centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            Transform parent = centerEye.transform.parent;

            if (parent != null)
            {
                Transform leftHand = parent.Find("LeftHandAnchor");
                Transform rightHand = parent.Find("RightHandAnchor");

                if (leftHand != null)
                {
                    locomotionController.leftHandAnchor = leftHand;
                }

                if (rightHand != null)
                {
                    locomotionController.rightHandAnchor = rightHand;
                }
            }
        }

        Debug.Log($"VRMuseumQuickSetup: 手柄锚点设置完成");
    }

    /// <summary>
    /// 升级照片显示组件
    /// </summary>
    private void UpgradePhotoDisplays()
    {
        if (photoDisplays == null || photoDisplays.Length == 0) return;

        int upgradedCount = 0;
        foreach (var display in photoDisplays)
        {
            if (display != null)
            {
                // 移除旧组件
                PhotoFrameDisplayFade oldComponent = display.GetComponent<PhotoFrameDisplayFade>();
                if (oldComponent != null)
                {
                    DestroyImmediate(oldComponent);
                }

                // 确保是VRPhotoDisplay
                if (display.GetComponent<VRPhotoDisplay>() == null)
                {
                    display.gameObject.AddComponent<VRPhotoDisplay>();
                }

                // 配置VR设置
                display.enableVRInteraction = enablePhotoInteractions;
                display.showPhotoInfo = true;
                display.enableHoverEffects = enableHoverEffects;
                display.enable3DFrameEffects = enable3DFrameEffects;

                // 添加碰撞器
                Collider collider = display.GetComponent<Collider>();
                if (collider == null)
                {
                    BoxCollider boxCollider = display.gameObject.AddComponent<BoxCollider>();
                    boxCollider.isTrigger = true;
                    boxCollider.size = Vector3.one * 0.1f;
                }

                upgradedCount++;
            }
        }

        Debug.Log($"VRMuseumQuickSetup: 升级了 {upgradedCount} 个照片显示组件");
    }

    /// <summary>
    /// 添加VR博物馆控制器
    /// </summary>
    private void AddVRMuseumController()
    {
        if (museumRoot == null) return;

        VRMuseumController controller = museumRoot.GetComponent<VRMuseumController>();
        if (controller == null)
        {
            controller = museumRoot.AddComponent<VRMuseumController>();
        }

        // 配置照片显示
        controller.photoDisplays = photoDisplays;
        controller.autoConfigurePhotoDisplays = false; // 我们已经配置过了
        controller.enablePhotoInteractions = enablePhotoInteractions;

        // 配置UI设置
        controller.showWelcomeMessage = showWelcomeMessage;
        // controller.showControlsPanel = showControlsPanel; // 属性不存在
        // controller.showStatsPanel = showStatsPanel; // 属性不存在

        // 配置控制设置
        controller.enableLocomotionSwitch = true;
        controller.switchLocomotionKey = KeyCode.L;

        Debug.Log("VRMuseumQuickSetup: 已添加VR博物馆控制器");
    }

    /// </// <summary>
    /// 创建信息Canvas
    /// </summary>
    private void CreateInfoCanvas()
    {
        if (museumRoot == null) return;

        // 查找现有Canvas
        Canvas existingCanvas = museumRoot.GetComponentInChildren<Canvas>();
        if (existingCanvas != null)
        {
            // 使用现有Canvas
            SetupExistingCanvas(existingCanvas);
            return;
        }

        // 创建新的Canvas
        GameObject canvasObj = new GameObject("InfoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.transform.SetParent(museumRoot.transform);
        canvas.transform.localPosition = new Vector3(0, 1.5f, -1);
        canvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvas.transform.localScale = Vector3.one * 0.001f;

        // 添加CanvasGroup
        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        // 创建UI元素
        CreateUIElements(canvasObj);

        Debug.Log("VRMuseumQuickSetup: 已创建信息Canvas");
    }

    /// <summary>
    /// 设置现有Canvas
    /// </summary>
    private void SetupExistingCanvas(Canvas canvas)
    {
        // 确保是World Space模式
        canvas.renderMode = RenderMode.WorldSpace;

        // 调整位置和大小
        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localPosition = new Vector3(0, 1.5f, -1f);
            rect.localScale = Vector3.one * 0.001f;
        }

        // 添加UI元素（如果还没有）
        if (canvas.GetComponentInChildren<TMP_Text>() == null)
        {
            CreateUIElements(canvas.gameObject);
        }
    }

    /// <summary>
    /// 创建UI元素
    /// </summary>
    private void CreateUIElements(GameObject canvasObj)
    {
        // 欢迎面板
        if (showWelcomeMessage)
        {
            CreateWelcomePanel(canvasObj);
        }

        // 控制面板
        if (showControlsPanel)
        {
            CreateControlsPanel(canvasObj);
        }

        // 统计面板
        if (showStatsPanel)
        {
            CreateStatsPanel(canvasObj);
        }

        // 调试面板
        if (enableDebugInfo)
        {
            CreateDebugPanel(canvasObj);
        }
    }

    /// <summary>
    /// 创建欢迎面板
    /// </summary>
    private void CreateWelcomePanel(GameObject canvasObj)
    {
        GameObject panel = CreateUIPanel(canvasObj, "WelcomePanel", new Vector2(0, 100), new Vector2(300, 200));

        TMP_Text text = CreateUIText(panel, "WelcomeText", "欢迎来到虚拟照片展览馆\n使用手柄移动并浏览您的摄影作品");
        text.fontSize = 16;
        text.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// 创建控制面板
    /// </summary>
    private void CreateControlsPanel(GameObject canvasObj)
    {
        GameObject panel = CreateUIPanel(canvasObj, "ControlsPanel", new Vector2(-320, -100), new Vector2(300, 150));

        TMP_Text text = CreateUIText(panel, "ControlsText", "移动控制:\n按L键切换模式");
        text.fontSize = 12;
        text.alignment = TextAlignmentOptions.TopLeft;
    }

    /// </// <summary>
    /// 创建统计面板
    /// </summary>
    private void CreateStatsPanel(GameObject canvasObj)
    {
        GameObject panel = CreateUIPanel(canvasObj, "StatsPanel", new Vector2(320, -100), new Vector2(300, 150));

        TMP_Text photoText = CreateUIText(panel, "PhotoCountText", "照片数量: 0");
        photoText.fontSize = 12;
        photoText.alignment = TextAlignmentOptions.TopRight;

        TMP_Text timeText = CreateUIText(panel, "TimeText", "访问时长: 00:00");
        timeText.fontSize = 12;
        timeText.alignment = TextAlignmentOptions.TopRight;
        timeText.transform.localPosition = new Vector3(0, -30, 0);
    }

    /// <summary>
    /// 创建调试面板
    /// </summary>
    private void CreateDebugPanel(GameObject canvasObj)
    {
        GameObject panel = CreateUIPanel(canvasObj, "DebugPanel", new Vector2(0, -300), new Vector2(400, 200));

        TMP_Text text = CreateUIText(panel, "DebugText", "调试信息:\n按F1切换显示");
        text.fontSize = 10;
        text.alignment = TextAlignmentOptions.TopLeft;
    }

    /// <summary>
    /// 创建UI面板
    /// </summary>
    private GameObject CreateUIPanel(GameObject parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        return panel;
    }

    /// <summary>
    /// 创建UI文本
    /// </summary>
    private TMP_Text CreateUIText(GameObject parent, string name, string initialText)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);

        TMP_Text textComponent = textObj.AddComponent<TMP_Text>();
        textComponent.text = initialText;
        textComponent.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        RectTransform rect = textComponent.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return textComponent;
    }

    /// <summary>
    /// 添加音频源
    /// </summary>
    private void AddAudioSource()
    {
        if (museumRoot == null) return;

        AudioSource audioSource = museumRoot.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = museumRoot.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        Debug.Log("VRMuseumQuickSetup: 已添加音频源");
    }

    /// <summary>
    /// 设置玩家位置
    /// </summary>
    private void SetupPlayerPosition()
    {
        // 获取主相机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 设置玩家在相机位置
        if (mainCamera != null && locomotionController != null)
        {
            locomotionController.transform.position = mainCamera.transform.position;
        }

        Debug.Log("VRMuseumQuickSetup: 玩家位置已设置");
    }

    /// <summary>
    /// 打印设置摘要
    /// </summary>
    private void PrintSetupSummary()
    {
        Debug.Log("=== VR博物馆快速设置摘要 ===");
        Debug.Log($"Museum Root: {(museumRoot != null ? museumRoot.name : "Not Found")}");
        Debug.Log($"Photo Displays: {(photoDisplays != null ? photoDisplays.Length : 0)}");
        Debug.Log($"Locomotion Controller: {(museumRoot.GetComponent<VRLocomotionController>() != null ? "Added" : "Not Added")}");
        Canvas canvasInfo = GetComponent<Canvas>();
        Debug.Log($"Info Canvas: {(canvasInfo != null ? "Found/Updated" : "Created")}");
        Debug.Log($"Welcome Message: {(showWelcomeMessage ? "Enabled" : "Disabled")}");
        Debug.Log($"Controls Panel: {(showControlsPanel ? "Enabled" : "Disabled")}");
        Debug.Log($"Stats Panel: {(showStatsPanel ? "Enabled" : "Disabled")}");
        Debug.Log($"Audio Source: {(museumRoot.GetComponent<AudioSource>() != null ? "Added" : "Not Added")}");
        Debug.Log("=== 设置完成 ===");
    }

    /// <summary>
    /// 手动配置所有照片显示
    /// </summary>
    [ContextMenu("Configure All Photo Displays")]
    public void ConfigureAllPhotoDisplays()
    {
        if (museumRoot != null)
        {
            VRMuseumController controller = museumRoot.GetComponent<VRMuseumController>();
            if (controller != null)
            {
                controller.ConfigureAllPhotoDisplays();
            }
        }
    }

    /// <summary>
    /// 测试VR移动
    /// </summary>
    [ContextMenu("Test VR Movement")]
    public void TestVRMovement()
    {
        VRLocomotionController locomotion = museumRoot?.GetComponent<VRLocomotionController>();
        if (locomotion != null)
        {
            Debug.Log("VR Movement Test:");
            Debug.Log($"- Locomotion Type: {locomotion.GetCurrentLocomotionType()}");
            Debug.Log($"- Move Speed: {locomotion.moveSpeed}");
            Debug.Log($"- Character Controller: {(locomotion.GetComponent<CharacterController>() != null ? "Found" : "Not Found")}");
        }
        else
        {
            Debug.LogError("VRMuseumQuickSetup: 找不到VRLocomotionController");
        }
    }

    /// <summary>
    /// 清理VR组件（用于测试）
    /// </summary>
    [ContextMenu("Remove VR Components")]
    public void RemoveVRComponents()
    {
        if (museumRoot == null) return;

        // 移除VR组件
        DestroyImmediate(museumRoot.GetComponent<VRLocomotionController>());
        DestroyImmediate(museumRoot.GetComponent<VRMuseumController>());

        // 移除VR照片显示组件
        VRPhotoDisplay[] vrDisplays = museumRoot.GetComponentsInChildren<VRPhotoDisplay>();
        foreach (var display in vrDisplays)
        {
            DestroyImmediate(display);
        }

        // 移除VR滑块交互
        VRSliderInteraction[] vrInteractions = museumRoot.GetComponentsInChildren<VRSliderInteraction>();
        foreach (var interaction in vrInteractions)
        {
            DestroyImmediate(interaction);
        }

        Debug.Log("VRMuseumQuickSetup: 已移除所有VR组件");
    }

    /// <summary>
    /// 强制重新加载照片
    /// </summary>
    [ContextMenu("Reload Photos")]
    public void ReloadPhotos()
    {
        if (photoDisplays != null)
        {
            foreach (var display in photoDisplays)
            {
                if (display != null)
                {
                    display.ReloadPhotos();
                }
            }
        }

        if (museumRoot != null)
        {
            VRMuseumController controller = museumRoot.GetComponent<VRMuseumController>();
            if (controller != null)
            {
                controller.ReloadAllPhotos();
            }
        }

        Debug.Log("VRMuseumQuickSetup: 已重新加载所有照片");
    }
}