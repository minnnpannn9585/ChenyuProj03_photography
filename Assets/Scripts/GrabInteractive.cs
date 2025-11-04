using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using Oculus.Interaction;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Grabbable))]
public class GrabInteractive : MonoBehaviour
{
    private Grabbable grabbable;
    public string targetSceneName = "Museum"; // 目标场景名称

    [Header("Debug")]
    public bool isGrabbed = false;

    [Header("Input Settings")]
    public KeyCode debugKey = KeyCode.Space; // 调试信息显示按键
    public InputActionAsset vrInputActions; // VR输入动作资源

    private InputAction aButtonAction;
    // Start is called before the first frame update
    void Start()
    {
        // 获取Grabbable组件
        grabbable = GetComponent<Grabbable>();

        // 初始化Input Action
        SetupInputActions();
    }

    void OnDestroy()
    {
        // 清理Input Action
        CleanupInputActions();
    }

    // 设置Input Actions
    private void SetupInputActions()
    {
        // 检查是否分配了VRInputActions资源
        if (vrInputActions == null)
        {
            Debug.LogError("VRInputActions资源未分配！请在Inspector中分配VRInputActions资源。");
            return;
        }

        // 启用VRInputActions
        vrInputActions.Enable();

        // 从Quest Action Map中获取A按钮
        InputActionMap questActionMap = vrInputActions.FindActionMap("Quest");
        if (questActionMap != null)
        {
            aButtonAction = questActionMap.FindAction("A");
            if (aButtonAction != null)
            {
                aButtonAction.performed += OnAButtonPressed;
                aButtonAction.Enable();
                Debug.Log("成功绑定VR A按钮");
            }
            else
            {
                Debug.LogError("在VRInputActions中未找到A按钮动作！");
            }
        }
        else
        {
            Debug.LogError("在VRInputActions中未找到Quest Action Map！");
        }
    }

    // 清理Input Actions
    private void CleanupInputActions()
    {
        if (aButtonAction != null)
        {
            aButtonAction.performed -= OnAButtonPressed;
            aButtonAction.Disable();
        }

        if (vrInputActions != null)
        {
            vrInputActions.Disable();
        }
    }

    // A键按下时的回调函数
    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        if (isGrabbed)
        {
            Debug.Log("VR A键按下（使用VRInputActions），切换到场景: " + targetSceneName);
            LoadTargetScene();
        }
    }

    // 更新抓取状态
    private void UpdateGrabStatus()
    {
        bool currentlyGrabbed = false;

        if (grabbable != null)
        {
            // 方法1: 检查物体的父级是否变化（被抓取时通常会作为手部的子物体）
            if (transform.parent != null && (transform.parent.name.Contains("Hand") || transform.parent.name.Contains("Controller")))
            {
                currentlyGrabbed = true;
            }

            // 方法2: 检查rigidbody的isKinematic状态（被抓取时通常设为kinematic）
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic)
            {
                currentlyGrabbed = true;
            }
        }

        // 如果状态发生变化，更新状态
        if (currentlyGrabbed != isGrabbed)
        {
            isGrabbed = currentlyGrabbed;
            Debug.Log($"物体 {gameObject.name} {(isGrabbed ? "被抓取" : "被释放")}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGrabStatus();
        HandleSceneTransition();
        ShowDebugInfo();
    }

    void HandleSceneTransition()
    {
        // 现在A键检测由Input Action回调函数处理
        // 这个方法可以用于其他场景切换逻辑，比如延迟检测等
    }

    // 调试信息显示
    void ShowDebugInfo()
    {
        if (Input.GetKeyDown(debugKey))
        {
            Debug.Log($"物体 {gameObject.name} 抓取状态: {isGrabbed}");
        }
    }

    void LoadTargetScene()
    {
        // 检查场景是否存在
        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            StartCoroutine(LoadSceneAsync());
        }
        else
        {
            Debug.LogError("场景 '" + targetSceneName + "' 不存在或未添加到Build Settings中！");
        }
    }

    IEnumerator LoadSceneAsync()
    {
        Debug.Log("开始加载场景: " + targetSceneName);

        // 可以添加场景切换效果，比如淡出等
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);

        // 等待场景加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("场景加载完成: " + targetSceneName);
    }
}
