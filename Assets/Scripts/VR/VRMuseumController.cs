using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using DG.Tweening;

/// <summary>
/// VR博物馆场景控制器
/// 统一管理浏览场景的所有VR功能
/// </summary>
public class VRMuseumController : MonoBehaviour
{
    [Header("核心组件")]
    public VRLocomotionController locomotionController;
    public Camera mainCamera;
    public Transform playerTransform;

    [Header("照片显示")]
    public VRPhotoDisplay[] photoDisplays;
    public bool autoConfigurePhotoDisplays = true;
    public bool enablePhotoInteractions = true;

    [Header("UI界面")]
    public Canvas infoCanvas;
    public GameObject welcomePanel;
    public TMP_Text welcomeText;
    public GameObject controlsPanel;
    public TMP_Text controlsText;
    public GameObject statsPanel;
    public TMP_Text photoCountText;
    public TMP_Text visitTimeText;

    [Header("控制设置")]
    public bool enableLocomotionSwitch = true;
    public KeyCode switchLocomotionKey = KeyCode.L;
    public bool showWelcomeMessage = true;
    public float welcomeDuration = 3f;

    [Header("场景设置")]
    public bool autoRotate = false;
    public float rotationSpeed = 10f;
    public Vector3 spawnPosition = new Vector3(0, 0, 5f);
    public bool useCustomSpawnPosition = false;

    [Header("音效")]
    public AudioClip welcomeSound;
    public AudioClip photoViewSound;
    public AudioClip switchModeSound;
    public AudioSource audioSource;

    [Header("调试设置")]
    public bool enableDebugInfo = true;
    public GameObject debugPanel;
    public TMP_Text debugText;

    // 私有变量
    private bool isInitialized = false;
    private float visitStartTime;
    private int viewedPhotos = 0;
    private VRPhotoDisplay currentHoveredDisplay;
    private bool isShowingWelcome = false;

    // 统计信息
    public struct MuseumStats
    {
        public int totalPhotos;
        public int viewedPhotos;
        public float visitDuration;
        public Vector3 lastPosition;
        public float totalDistance;
    }

    private MuseumStats stats;

    void Start()
    {
        InitializeController();
    }

    void Update()
    {
        if (!isInitialized) return;

        HandleInput();
        UpdateStats();
        HandleDebugInfo();

        // 自动旋转（如果启用）
        if (autoRotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 初始化控制器
    /// </summary>
    private void InitializeController()
    {
        StartCoroutine(InitializeCoroutine());
    }

    /// <summary>
    /// 初始化协程
    /// </summary>
    private IEnumerator InitializeCoroutine()
    {
        // 等待VR系统初始化
        yield return new WaitForSeconds(0.5f);

        // 设置玩家位置
        SetupPlayerPosition();

        // 查找组件
        FindComponents();

        // 自动配置照片显示
        if (autoConfigurePhotoDisplays)
        {
            ConfigurePhotoDisplays();
        }

        // 初始化UI
        InitializeUI();

        // 显示欢迎信息
        if (showWelcomeMessage)
        {
            ShowWelcomeMessage();
        }

        // 开始统计
        StartStatsTracking();

        isInitialized = true;
        Debug.Log("VRMuseumController initialized successfully");
    }

    /// <summary>
    /// 设置玩家位置
    /// </summary>
    private void SetupPlayerPosition()
    {
        if (useCustomSpawnPosition)
        {
            transform.position = spawnPosition;
        }

        // 确保玩家在地面
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 10f, Vector3.down, out hit, 20f))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }

        // 获取玩家Transform
        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        // 获取主相机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    /// <summary>
    /// 查找组件
    /// </summary>
    private void FindComponents()
    {
        // 查找移动控制器
        if (locomotionController == null)
        {
            locomotionController = FindObjectOfType<VRLocomotionController>();
        }

        // 自动查找照片显示组件
        if (photoDisplays == null || photoDisplays.Length == 0)
        {
            VRPhotoDisplay[] allDisplays = FindObjectsOfType<VRPhotoDisplay>();
            if (allDisplays.Length > 0)
            {
                photoDisplays = allDisplays;
                Debug.Log($"VRMuseumController: Found {allDisplays.Length} photo displays");
            }
        }

        // 查找Canvas组件
        if (infoCanvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            infoCanvas = canvases.FirstOrDefault(c => c.name.ToLower().Contains("info") || c.name.ToLower().Contains("ui"));
        }

        // 查找UI组件
        if (infoCanvas != null)
        {
            welcomePanel = infoCanvas.transform.Find("WelcomePanel")?.gameObject;
            controlsPanel = infoCanvas.transform.Find("ControlsPanel")?.gameObject;
            statsPanel = infoCanvas.transform.Find("StatsPanel")?.gameObject;

            if (welcomePanel != null)
                welcomeText = welcomePanel.GetComponentInChildren<TMP_Text>();
            if (controlsPanel != null)
                controlsText = controlsPanel.GetComponentInChildren<TMP_Text>();
            if (statsPanel != null)
            {
                TMP_Text[] texts = statsPanel.GetComponentsInChildren<TMP_Text>();
                photoCountText = texts.FirstOrDefault(t => t.name.ToLower().Contains("photo"));
                visitTimeText = texts.FirstOrDefault(t => t.name.ToLower().Contains("time"));
            }
        }

        Debug.Log($"Components found - Locomotion: {locomotionController != null}, Photo Displays: {photoDisplays.Length}, Info Canvas: {infoCanvas != null}");
    }

    /// <summary>
    /// 配置照片显示
    /// </summary>
    private void ConfigurePhotoDisplays()
    {
        if (photoDisplays == null || photoDisplays.Length == 0) return;

        int configuredCount = 0;
        foreach (var display in photoDisplays)
        {
            if (display != null)
            {
                // 设置事件监听
                display.OnPhotoChanged += OnPhotoChanged;
                display.OnPhotoInfoDisplayed += OnPhotoInfoDisplayed;

                // 启用VR交互
                display.enableVRInteraction = enablePhotoInteractions;
                display.showPhotoInfo = true;
                display.enableHoverEffects = true;

                // 添加碰撞器以便VR交互
                Collider collider = display.GetComponent<Collider>();
                if (collider == null)
                {
                    BoxCollider boxCollider = display.gameObject.AddComponent<BoxCollider>();
                    boxCollider.isTrigger = true;
                    boxCollider.size = Vector3.one * 0.1f;
                }

                configuredCount++;
            }
        }

        Debug.Log($"VRMuseumController: Configured {configuredCount} photo displays for VR interaction");
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 初始化音频源
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 设置初始UI状态
        if (welcomePanel != null)
        {
            welcomePanel.SetActive(false);
        }

        if (controlsPanel != null)
        {
            UpdateControlsDisplay();
        }

        if (statsPanel != null)
        {
            statsPanel.SetActive(true);
        }

        // 隐藏调试面板
        if (debugPanel != null && !enableDebugInfo)
        {
            debugPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示欢迎信息
    /// </summary>
    private void ShowWelcomeMessage()
    {
        if (welcomePanel == null || welcomeText == null) return;

        isShowingWelcome = true;

        // 设置欢迎文本
        welcomeText.text = "欢迎来到虚拟照片展览馆\n\n使用手柄移动并浏览您的摄影作品";

        // 显示面板
        welcomePanel.SetActive(true);

        // 播放欢迎音效
        if (welcomeSound != null)
        {
            audioSource.PlayOneShot(welcomeSound);
        }

        // 动画效果
        CanvasGroup canvasGroup = welcomePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = welcomePanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);

        // 自动隐藏
        StartCoroutine(HideWelcomeAfterDelay());
    }

    /// <summary>
    /// 延迟隐藏欢迎信息
    /// </summary>
    private IEnumerator HideWelcomeAfterDelay()
    {
        yield return new WaitForSeconds(welcomeDuration);

        CanvasGroup canvasGroup = welcomePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, 0.5f).OnComplete(() => {
                welcomePanel.SetActive(false);
                isShowingWelcome = false;
            });
        }
    }

    /// <summary>
    /// 开始统计跟踪
    /// </summary>
    private void StartStatsTracking()
    {
        visitStartTime = Time.time;
        stats = new MuseumStats
        {
            totalPhotos = photoDisplays?.Length ?? 0,
            viewedPhotos = 0,
            visitDuration = 0f,
            lastPosition = transform.position,
            totalDistance = 0f
        };
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        // 切换移动模式
        if (enableLocomotionSwitch && Input.GetKeyDown(switchLocomotionKey))
        {
            SwitchLocomotionType();
        }

        // 手动隐藏欢迎信息
        if (isShowingWelcome && Input.GetKeyDown(KeyCode.Escape))
        {
            HideWelcomePanel();
        }

        // 调试信息
        if (enableDebugInfo && Input.GetKeyDown(KeyCode.F1))
        {
            ToggleDebugInfo();
        }
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStats()
    {
        if (!isInitialized) return;

        // 更新访问时长
        stats.visitDuration = Time.time - visitStartTime;

        // 更新移动距离
        float distance = Vector3.Distance(transform.position, stats.lastPosition);
        stats.totalDistance += distance;
        stats.lastPosition = transform.position;

        // 更新UI显示
        UpdateStatsUI();
    }

    /// <summary>
    /// 更新统计UI
    /// </summary>
    private void UpdateStatsUI()
    {
        if (photoCountText != null)
        {
            photoCountText.text = $"照片数量: {stats.totalPhotos}";
        }

        if (visitTimeText != null)
        {
            int minutes = Mathf.FloorToInt(stats.visitDuration / 60f);
            int seconds = Mathf.FloorToInt(stats.visitDuration % 60f);
            visitTimeText.text = $"访问时长: {minutes:00}:{seconds:D2}";
        }
    }

    /// <summary>
    /// 处理调试信息
    /// </summary>
    private void HandleDebugInfo()
    {
        if (debugText == null) return;

        string debugInfo = $"=== VR博物馆调试信息 ===\n";
        debugInfo += $"访问时长: {stats.visitDuration:F1}s\n";
        debugInfo += $"移动距离: {stats.totalDistance:F2}m\n";
        debugInfo += $"查看照片: {stats.viewedPhotos}/{stats.totalPhotos}\n";
        debugInfo += $"当前位置: {transform.position:F2}\n";
        debugInfo += $"移动模式: {(locomotionController?.GetCurrentLocomotionType() ?? VRLocomotionController.LocomotionType.Smooth)}\n";

        if (currentHoveredDisplay != null)
        {
            debugInfo += $"当前查看: {currentHoveredDisplay.GetCurrentIndex() + 1}/{currentHoveredDisplay.GetPhotoCount()}\n";
        }

        debugInfo += "\n=== 控制说明 ===\n";
        debugInfo += "L键: 切换移动模式\n";
        debugInfo += "ESC: 隐藏欢迎信息\n";
        debugInfo += "F1: 切换调试信息\n";
        debugInfo += "长按菜单键: 返回主菜单";

        debugText.text = debugInfo;
    }

    /// <summary>
    /// 切换移动类型
    /// </summary>
    private void SwitchLocomotionType()
    {
        if (locomotionController != null)
        {
            locomotionController.SwitchLocomotionType();
            UpdateControlsDisplay();

            // 播放切换音效
            if (switchModeSound != null)
            {
                audioSource.PlayOneShot(switchModeSound, 0.5f);
            }

            Debug.Log($"Switched to {locomotionController.GetCurrentLocomotionType()} locomotion");
        }
    }

    /// <summary>
    /// 更新控制显示
    /// </summary>
    private void UpdateControlsDisplay()
    {
        if (controlsText == null || locomotionController == null) return;

        string controls = "=== 移动控制 ===\n\n";
        controls += $"当前模式: {locomotionController.GetCurrentLocomotionType()}\n\n";

        switch (locomotionController.GetCurrentLocomotionType())
        {
            case VRLocomotionController.LocomotionType.Smooth:
                controls += "操作说明:\n";
                controls += "• 左手摇杆: 移动\n";
                controls += "• 右手摇杆: 转向\n";
                controls += "• 手柄按键: 瞬移(如启用)\n";
                break;

            case VRLocomotionController.LocomotionType.Teleport:
                controls += "操作说明:\n";
                controls += "• 左手B键: 瞄准瞬移\n";
                controls += "• 左手A键: 执行瞬移\n";
                controls += "• 右手摇杆: 转向\n";
                break;

            case VRLocomotionController.LocomotionType.Hybrid:
                controls += "操作说明:\n";
                controls += "• 移动时: 平滑移动\n";
                controls += "• 静止时: 可瞬移\n";
                controls += "• 右手摇杆: 转向\n";
                controls += "• L键: 切换模式\n";
                break;
        }

        controls += "\n=== 照片交互 ===\n";
        controls += "• 悬停在相框上查看详情\n";
        controls += "• 手柄靠近相框查看信息\n";
        controls += "• 自动轮播展示照片\n";

        controlsText.text = controls;
    }

    /// <summary>
    /// 隐藏欢迎面板
    /// </summary>
    private void HideWelcomePanel()
    {
        if (welcomePanel != null && isShowingWelcome)
        {
            CanvasGroup canvasGroup = welcomePanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.3f).OnComplete(() => {
                    welcomePanel.SetActive(false);
                    isShowingWelcome = false;
                });
            }
            else
            {
                welcomePanel.SetActive(false);
                isShowingWelcome = false;
            }
        }
    }

    /// <summary>
    /// 切换调试信息
    /// </summary>
    private void ToggleDebugInfo()
    {
        if (debugPanel != null)
        {
            bool isActive = debugPanel.activeSelf;
            debugPanel.SetActive(!isActive);
        }
    }

    /// <summary>
    /// 照片变化事件
    /// </summary>
    private void OnPhotoChanged(Texture2D photo)
    {
        viewedPhotos++;
        stats.viewedPhotos = viewedPhotos;

        // 播放查看音效
        if (photoViewSound != null)
        {
            audioSource.PlayOneShot(photoViewSound, 0.2f);
        }

        Debug.Log($"Viewed photo {viewedPhotos}: {photo?.name ?? "Unknown"}");
    }

    /// <summary>
    /// 照片信息显示事件
    /// </summary>
    private void OnPhotoInfoDisplayed(string photoName)
    {
        Debug.Log($"Photo info displayed: {photoName}");
    }

    /// <summary>
    /// 强制配置所有照片显示
    /// </summary>
    [ContextMenu("Configure All Photo Displays")]
    public void ConfigureAllPhotoDisplays()
    {
        VRPhotoDisplay[] allDisplays = FindObjectsOfType<VRPhotoDisplay>();
        int configuredCount = 0;

        foreach (var display in allDisplays)
        {
            if (display != null)
            {
                // 移除旧的VR交互组件（如果有）
                VRGrabbable oldGrabbable = display.GetComponent<VRGrabbable>();
                if (oldGrabbable != null)
                {
                    DestroyImmediate(oldGrabbable);
                }

                // 配置新设置
                display.enableVRInteraction = true;
                display.showPhotoInfo = true;
                display.enableHoverEffects = true;
                display.enable3DFrameEffects = true;

                // 添加碰撞器
                Collider collider = display.GetComponent<Collider>();
                if (collider == null)
                {
                    BoxCollider boxCollider = display.gameObject.AddComponent<BoxCollider>();
                    boxCollider.isTrigger = true;
                    boxCollider.size = Vector3.one * 0.1f;
                }

                // 设置事件
                display.OnPhotoChanged += OnPhotoChanged;
                display.OnPhotoInfoDisplayed += OnPhotoInfoDisplayed;

                configuredCount++;
            }
        }

        Debug.Log($"VRMuseumController: Configured {configuredCount} photo displays");
    }

    /// <summary>
    /// 重新加载所有照片
    /// </summary>
    [ContextMenu("Reload All Photos")]
    public void ReloadAllPhotos()
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

        // 更新统计
        if (photoDisplays != null)
        {
            stats.totalPhotos = 0;
            foreach (var display in photoDisplays)
            {
                if (display != null)
                {
                    stats.totalPhotos += display.GetPhotoCount();
                }
            }
        }

        Debug.Log("VRMuseumController: Reloaded all photos");
    }

    /// <summary>
    /// 强制传送到指定位置
    /// </summary>
    [ContextMenu("Force Teleport to Spawn")]
    public void ForceTeleportToSpawn()
    {
        if (locomotionController != null)
        {
            locomotionController.ForceTeleport(spawnPosition);
        }
        else
        {
            transform.position = spawnPosition;
        }

        Debug.Log($"VRMuseumController: Force teleported to {spawnPosition}");
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public MuseumStats GetStats()
    {
        return stats;
    }

    /// <summary>
    /// 设置自动旋转
    /// </summary>
    public void SetAutoRotate(bool enabled)
    {
        autoRotate = enabled;
    }

    /// <summary>
    /// 设置旋转速度
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (photoDisplays != null)
        {
            foreach (var display in photoDisplays)
            {
                if (display != null)
                {
                    display.OnPhotoChanged -= OnPhotoChanged;
                    display.OnPhotoInfoDisplayed -= OnPhotoInfoDisplayed;
                }
            }
        }

        // 清理DOTween动画
        transform.DOKill();
    }
}