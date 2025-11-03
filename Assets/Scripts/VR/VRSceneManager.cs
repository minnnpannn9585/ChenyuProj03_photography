using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR场景管理器 - 处理场景切换和菜单键返回功能
/// 管理三个场景：MainMenu, PhotoScene, Museum
/// </summary>
public class VRSceneManager : MonoBehaviour
{
    [Header("场景设置")]
    public string mainMenuSceneName = "MainMenu";
    public string photoSceneName = "PhotoScene";
    public string museumSceneName = "Museum";

    [Header("返回设置")]
    public float menuHoldDuration = 3f; // 菜单键长按时间
    public UnityEngine.UI.Image returnProgressImage; // 返回进度指示器（可选）

    [Header("VR设置")]
    public GameObject vrRigPrefab; // VR装备预制体

    // 私有变量
    private bool isMenuButtonPressed = false;
    private float menuButtonHoldTime = 0f;
    private bool isTransitioning = false;

    // 单例模式
    public static VRSceneManager Instance { get; private set; }

    void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 确保VR系统正确初始化
        InitializeVRSystem();
    }

    void Update()
    {
        // 检测菜单键长按返回
        HandleMenuButtonReturn();
    }

    /// <summary>
    /// 初始化VR系统
    /// </summary>
    private void InitializeVRSystem()
    {
        // 检查VR设备状态
        if (XRSettings.isDeviceActive)
        {
            Debug.Log("VR设备已激活: " + XRSettings.loadedDeviceName);
        }
        else
        {
            Debug.Log("等待VR设备连接...");
        }

        // 隐藏鼠标光标在VR环境中
        if (XRSettings.isDeviceActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 隐藏返回进度指示器
        if (returnProgressImage != null)
        {
            returnProgressImage.fillAmount = 0f;
            returnProgressImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 处理菜单键长按返回逻辑
    /// </summary>
    private void HandleMenuButtonReturn()
    {
        // 只在非MainMenu场景处理返回逻辑
        if (SceneManager.GetActiveScene().name == mainMenuSceneName || isTransitioning)
            return;

        // 检测菜单键状态（使用通用输入系统）
        bool menuButtonCurrentState = Input.GetKeyDown(KeyCode.JoystickButton7) || // 菜单键通用按键
                                     OVRInput.GetDown(OVRInput.Button.Start); // Meta Quest特定菜单键

        bool menuButtonHeldState = Input.GetKey(KeyCode.JoystickButton7) ||
                                   OVRInput.Get(OVRInput.Button.Start);

        if (menuButtonCurrentState && !isMenuButtonPressed)
        {
            // 菜单键刚刚被按下
            isMenuButtonPressed = true;
            menuButtonHoldTime = 0f;

            // 显示返回进度指示器
            if (returnProgressImage != null)
            {
                returnProgressImage.gameObject.SetActive(true);
                returnProgressImage.fillAmount = 0f;
            }

            Debug.Log("开始菜单键长按计时...");
        }
        else if (menuButtonHeldState && isMenuButtonPressed)
        {
            // 菜单键正在被按住
            menuButtonHoldTime += Time.deltaTime;

            // 更新进度指示器
            if (returnProgressImage != null)
            {
                returnProgressImage.fillAmount = menuButtonHoldTime / menuHoldDuration;
            }

            // 检查是否达到长按时间
            if (menuButtonHoldTime >= menuHoldDuration)
            {
                // 触发返回主菜单
                StartCoroutine(ReturnToMainMenu());
                ResetMenuButtonState();
            }
        }
        else if (!menuButtonHeldState && isMenuButtonPressed)
        {
            // 菜单键被释放（未达到长按时间）
            ResetMenuButtonState();
        }
    }

    /// <summary>
    /// 重置菜单按钮状态
    /// </summary>
    private void ResetMenuButtonState()
    {
        isMenuButtonPressed = false;
        menuButtonHoldTime = 0f;

        // 隐藏返回进度指示器
        if (returnProgressImage != null)
        {
            returnProgressImage.gameObject.SetActive(false);
            returnProgressImage.fillAmount = 0f;
        }
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public IEnumerator ReturnToMainMenu()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("场景切换中，忽略返回请求");
            yield break;
        }

        isTransitioning = true;
        Debug.Log("返回主菜单...");

        // 可以在这里添加过渡效果
        yield return new WaitForSeconds(0.1f);

        // 加载主菜单场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuSceneName);

        // 等待场景加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isTransitioning = false;
        Debug.Log("已返回主菜单场景");
    }

    /// <summary>
    /// 加载摄影场景
    /// </summary>
    public void LoadPhotoScene()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("场景切换中，忽略加载请求");
            return;
        }

        Debug.Log("加载摄影场景...");
        StartCoroutine(LoadScene(photoSceneName));
    }

    /// <summary>
    /// 加载博物馆场景
    /// </summary>
    public void LoadMainMenu()
    {
        Debug.Log($"[VRSceneManager] 加载主菜单场景: {mainMenuSceneName}");
        
        if (isTransitioning)
        {
            Debug.LogWarning("[VRSceneManager] 正在场景切换中，忽略请求");
            return;
        }
        
        StartCoroutine(LoadSceneAsync(mainMenuSceneName));
    }

    /// <summary>
    /// 异步加载场景的协程
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (isTransitioning)
            yield break;

        isTransitioning = true;

        // 可以在这里添加淡出效果
        yield return new WaitForSeconds(0.1f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isTransitioning = false;
        Debug.Log("场景加载完成: " + sceneName);
    }

    public void LoadMuseumScene()
    {
        Debug.Log($"[VRSceneManager] 加载博物馆场景: {museumSceneName}");

        if (isTransitioning)
        {
            Debug.LogWarning("[VRSceneManager] 正在场景切换中，忽略请求");
            return;
        }

        Debug.Log("加载博物馆场景...");
        StartCoroutine(LoadScene(museumSceneName));
    }

    /// <summary>
    /// 异步加载场景的协程
    /// </summary>
    private IEnumerator LoadScene(string sceneName)
    {
        if (isTransitioning)
            yield break;

        isTransitioning = true;

        // 可以在这里添加淡出效果
        yield return new WaitForSeconds(0.1f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isTransitioning = false;
        Debug.Log("场景加载完成: " + sceneName);
    }

    /// <summary>
    /// 获取当前场景名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// 检查是否正在切换场景
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    /// <summary>
    /// 清理资源（可选）
    /// </summary>
    void OnDestroy()
    {
        // 清理事件监听器等
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 应用暂停时重置菜单状态
            ResetMenuButtonState();
        }
    }
}