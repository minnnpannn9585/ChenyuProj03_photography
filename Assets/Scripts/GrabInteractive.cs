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

    [Header("UI Control")]
    public Canvas infoCanvas; // 信息显示Canvas
    public Vector3 originalPosition; // 初始位置
    public Quaternion originalRotation; // 初始旋转

    [Header("Input Settings")]
    public KeyCode debugKey = KeyCode.Space; // 调试信息显示按键
    public InputActionAsset vrInputActions; // VR输入动作资源

    private InputAction aButtonAction;
    // Start is called before the first frame update
    void Start()
    {
        // 保存初始位置和旋转
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // 获取Grabbable组件
        grabbable = GetComponent<Grabbable>();

        // 初始化Canvas状态（默认隐藏）
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }

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

            // 状态变化时处理Canvas显示和位置重置
            if (isGrabbed)
            {
                OnGrabbed();
            }
            else
            {
                OnReleased();
            }
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
        // if (Input.GetKeyDown(debugKey))
        // {
        //     Debug.Log($"物体 {gameObject.name} 抓取状态: {isGrabbed}");
        // }
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

    // 物体被抓取时的处理
    private void OnGrabbed()
    {
        Debug.Log($"物体 {gameObject.name} 被抓取，显示Canvas");

        // 显示Canvas
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(true);
        }
    }

    // 物体被释放时的处理
    private void OnReleased()
    {
        Debug.Log($"物体 {gameObject.name} 被释放，隐藏Canvas并重置位置");

        // 隐藏Canvas
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }

        // 重置位置和旋转
        ResetPosition();
    }

    // 重置物体到初始位置
    private void ResetPosition()
    {
        // 取消父子关系
        transform.SetParent(null);

        // 重置物理状态
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 重置位置和旋转
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        Debug.Log($"物体 {gameObject.name} 已重置到初始位置: {originalPosition}");
    }
}
