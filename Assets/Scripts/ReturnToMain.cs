using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ReturnToMain : MonoBehaviour
{
    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Input Settings")]
    public InputActionAsset vrInputActions; // VR输入动作资源
    public float holdDuration = 3.0f; // 长按持续时间（秒）

    [Header("Feedback Settings")]
    public bool showVisualFeedback = true;
    public UnityEngine.UI.Image progressBar; // 进度条UI组件（可选）
    public Color progressColor = Color.white;

    [Header("Debug")]
    public KeyCode debugKey = KeyCode.M; // 调试按键，用于测试功能

    private InputAction menuButtonAction;
    private float currentHoldTime = 0.0f;
    private bool isMenuButtonPressed = false;
    private bool isReturningToMain = false;

    private Coroutine returnCoroutine;

    void Start()
    {
        SetupInputActions();
    }

    void OnDestroy()
    {
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

        // 从Quest Action Map中获取Menu按钮
        InputActionMap questActionMap = vrInputActions.FindActionMap("Quest");
        if (questActionMap != null)
        {
            menuButtonAction = questActionMap.FindAction("Menu");
            if (menuButtonAction != null)
            {
                menuButtonAction.started += OnMenuButtonPressed;
                menuButtonAction.canceled += OnMenuButtonReleased;
                menuButtonAction.Enable();
                Debug.Log("成功绑定VR Menu按钮");
            }
            else
            {
                Debug.LogError("在VRInputActions中未找到Menu按钮动作！");
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
        if (menuButtonAction != null)
        {
            menuButtonAction.started -= OnMenuButtonPressed;
            menuButtonAction.canceled -= OnMenuButtonReleased;
            menuButtonAction.Disable();
        }

        if (vrInputActions != null)
        {
            vrInputActions.Disable();
        }

        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }
    }

    // Menu按钮按下事件
    private void OnMenuButtonPressed(InputAction.CallbackContext context)
    {
        if (!isReturningToMain)
        {
            isMenuButtonPressed = true;
            currentHoldTime = 0.0f;
            returnCoroutine = StartCoroutine(HandleMenuButtonHold());
            Debug.Log("Menu按钮按下，开始计时...");
        }
    }

    // Menu按钮释放事件
    private void OnMenuButtonReleased(InputAction.CallbackContext context)
    {
        if (!isReturningToMain)
        {
            isMenuButtonPressed = false;
            currentHoldTime = 0.0f;

            if (returnCoroutine != null)
            {
                StopCoroutine(returnCoroutine);
                returnCoroutine = null;
            }

            // 重置进度条
            UpdateProgressBar(0.0f);
            Debug.Log("Menu按钮释放，计时取消");
        }
    }

    // 处理Menu按钮长按的协程
    private IEnumerator HandleMenuButtonHold()
    {
        while (isMenuButtonPressed && currentHoldTime < holdDuration)
        {
            currentHoldTime += Time.deltaTime;
            UpdateProgressBar(currentHoldTime / holdDuration);

            // 每秒输出一次进度
            if (currentHoldTime % 1.0f < Time.deltaTime)
            {
                Debug.Log($"Menu按钮按住中... {currentHoldTime:F1}秒 / {holdDuration}秒");
            }

            yield return null;
        }

        // 如果按满了指定时间且没有被取消
        if (isMenuButtonPressed && currentHoldTime >= holdDuration)
        {
            ReturnToMainMenu();
        }
    }

    // 更新进度条（如果有的话）
    private void UpdateProgressBar(float progress)
    {
        if (showVisualFeedback && progressBar != null)
        {
            progressBar.fillAmount = progress;
            progressBar.color = progressColor;
        }
    }

    // 回到主菜单
    private void ReturnToMainMenu()
    {
        if (isReturningToMain) return; // 防止重复执行

        isReturningToMain = true;
        Debug.Log($"Menu按钮长按{holdDuration}秒，正在返回主菜单...");

        // 检查场景是否存在
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            StartCoroutine(LoadMainMenuAsync());
        }
        else
        {
            Debug.LogError($"场景 '{mainMenuSceneName}' 不存在或未添加到Build Settings中！");
            isReturningToMain = false;
        }
    }

    // 异步加载主菜单
    private IEnumerator LoadMainMenuAsync()
    {
        Debug.Log($"开始加载主菜单场景: {mainMenuSceneName}");

        // 可以添加场景切换效果，比如淡出等
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuSceneName);

        // 等待场景加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log($"主菜单场景加载完成: {mainMenuSceneName}");
    }

    void Update()
    {
        // 调试功能：使用键盘M键模拟长按Menu按钮
        if (Input.GetKeyDown(debugKey))
        {
            Debug.Log($"调试：模拟Menu按钮按下（长按{holdDuration}秒）");
            OnMenuButtonPressed(new InputAction.CallbackContext());
        }

        if (Input.GetKeyUp(debugKey))
        {
            Debug.Log("调试：模拟Menu按钮释放");
            OnMenuButtonReleased(new InputAction.CallbackContext());
        }

        ShowDebugInfo();
    }

    // 调试信息显示
    void ShowDebugInfo()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"ReturnToMain状态 - Menu按钮按下: {isMenuButtonPressed}, 按住时间: {currentHoldTime:F1}秒, 目标时间: {holdDuration}秒, 返回中: {isReturningToMain}");
        }
    }
}
