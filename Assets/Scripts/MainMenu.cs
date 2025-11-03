using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

/// <summary>
/// 主菜单脚本 - 支持VR和传统模式
/// 管理主菜单场景的初始化和逻辑
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("VR设置")]
    public bool enableVR = true; // 是否启用VR
    public VRMenuController vrMenuController; // VR菜单控制器

    [Header("传统模式设置")]
    public UnityEngine.UI.Button photoSceneButton; // 摄影场景按钮
    public UnityEngine.UI.Button museumSceneButton; // 博物馆场景按钮
    public UnityEngine.UI.Button quitButton; // 退出按钮

    [Header("场景管理")]
    public string photoSceneName = "PhotoScene";
    public string museumSceneName = "Museum";

    private bool isVRActive = false;

    void Start()
    {
        // 检测VR状态
        isVRActive = enableVR && XRSettings.isDeviceActive;

        Debug.Log("主菜单初始化 - VR模式: " + (isVRActive ? "启用" : "禁用"));

        if (isVRActive)
        {
            InitializeVRMenu();
        }
        else
        {
            InitializeTraditionalMenu();
        }
    }

    /// <summary>
    /// 初始化VR菜单
    /// </summary>
    private void InitializeVRMenu()
    {
        if (vrMenuController == null)
        {
            vrMenuController = FindObjectOfType<VRMenuController>();
        }

        if (vrMenuController != null)
        {
            Debug.Log("VR菜单控制器已初始化");
        }
        else
        {
            Debug.LogWarning("VR菜单控制器未找到！");
        }

        // 隐藏传统UI
        HideTraditionalUI();
    }

    /// <summary>
    /// 初始化传统菜单
    /// </summary>
    private void InitializeTraditionalMenu()
    {
        Debug.Log("初始化传统菜单");

        // 设置按钮事件
        if (photoSceneButton != null)
        {
            photoSceneButton.onClick.AddListener(LoadPhotoScene);
        }

        if (museumSceneButton != null)
        {
            museumSceneButton.onClick.AddListener(LoadMuseumScene);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        // 隐藏VR相关物体
        HideVRObjects();
    }

    /// <summary>
    /// 隐藏传统UI
    /// </summary>
    private void HideTraditionalUI()
    {
        if (photoSceneButton != null && photoSceneButton.gameObject != null)
        {
            photoSceneButton.gameObject.SetActive(false);
        }

        if (museumSceneButton != null && museumSceneButton.gameObject != null)
        {
            museumSceneButton.gameObject.SetActive(false);
        }

        if (quitButton != null && quitButton.gameObject != null)
        {
            quitButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 隐藏VR物体
    /// </summary>
    private void HideVRObjects()
    {
        if (vrMenuController != null && vrMenuController.gameObject != null)
        {
            vrMenuController.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 加载摄影场景
    /// </summary>
    public void LoadPhotoScene()
    {
        Debug.Log("加载摄影场景: " + photoSceneName);

        if (VRSceneManager.Instance != null)
        {
            VRSceneManager.Instance.LoadPhotoScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(photoSceneName);
        }
    }

    /// <summary>
    /// 加载博物馆场景
    /// </summary>
    public void LoadMuseumScene()
    {
        Debug.Log("加载博物馆场景: " + museumSceneName);

        if (VRSceneManager.Instance != null)
        {
            VRSceneManager.Instance.LoadMuseumScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(museumSceneName);
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }

    void Update()
    {
        // 可以在这里添加全局逻辑
        // 比如检测按键等

        // 传统模式下的快捷键
        if (!isVRActive)
        {
            HandleTraditionalInput();
        }
    }

    /// <summary>
    /// 处理传统模式输入
    /// </summary>
    private void HandleTraditionalInput()
    {
        // 使用新Input System处理按键
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            // 按数字键快速进入场景
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                LoadPhotoScene();
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                LoadMuseumScene();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                QuitGame();
            }
        }
    }

    /// <summary>
    /// 切换VR模式（用于调试）
    /// </summary>
    public void ToggleVRMode()
    {
        enableVR = !enableVR;
        Debug.Log("VR模式切换: " + (enableVR ? "启用" : "禁用"));

        // 重新初始化
        Start();
    }

    void OnDestroy()
    {
        // 清理按钮事件
        if (photoSceneButton != null)
        {
            photoSceneButton.onClick.RemoveListener(LoadPhotoScene);
        }

        if (museumSceneButton != null)
        {
            museumSceneButton.onClick.RemoveListener(LoadMuseumScene);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }

    void OnGUI()
    {
        // 显示调试信息（仅在编辑器中）
        #if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.Label("主菜单状态");
        GUILayout.Label("VR模式: " + (isVRActive ? "启用" : "禁用"));
        GUILayout.Label("VR设备: " + (XRSettings.isDeviceActive ? XRSettings.loadedDeviceName : "未激活"));

        if (GUILayout.Button("切换VR模式"))
        {
            ToggleVRMode();
        }

        GUILayout.Space(10);
        GUILayout.Label("快捷键:");
        GUILayout.Label("1 - 摄影场景");
        GUILayout.Label("2 - 博物馆场景");
        GUILayout.Label("ESC - 退出");

        GUILayout.EndArea();
        #endif
    }
}
