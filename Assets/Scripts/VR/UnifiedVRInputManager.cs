using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 统一VR输入管理器 - 基于新的Unity Input System
/// 替代分散在多个脚本中的输入处理逻辑，解决输入冲突
/// </summary>
public class UnifiedVRInputManager : MonoBehaviour
{
    [Header("控制器引用")]
    public GameObject xrOrigin; // XR Origin GameObject

    [Header("输入设置")]
    public bool enableDebugLogs = true;

    // 单例模式
    public static UnifiedVRInputManager Instance { get; private set; }

    // 输入动作引用
    [Header("Input Actions")]
    public InputActionReference leftGrabAction;
    public InputActionReference rightGrabAction;
    public InputActionReference leftTriggerAction;
    public InputActionReference rightTriggerAction;
    public InputActionReference aButtonAction;
    public InputActionReference bButtonAction;
    public InputActionReference xButtonAction;
    public InputActionReference yButtonAction;
    public InputActionReference menuButtonAction;
    public InputActionReference leftThumbstickAction;
    public InputActionReference rightThumbstickAction;

    // 输入模式枚举
    public enum InputMode
    {
        Default,        // 默认模式 - 调整焦段
        HoldA,          // 按住A - 调整对焦距离
        HoldB,          // 按住B - 调整ISO
        HoldX,          // 按住X - 调整光圈
        HoldY           // 按住Y - 调整快门速度
    }

    // 事件定义
    public System.Action<InputMode> OnInputModeChanged;
    public System.Action<float, float> OnGrabChanged; // leftValue, rightValue
    public System.Action<Vector2> OnLeftThumbstickMoved;
    public System.Action<Vector2> OnRightThumbstickMoved;
    public System.Action OnTriggerPressed;
    public System.Action OnMenuButtonPressed;
    public System.Action OnMenuButtonReleased;

    // 私有变量
    private InputMode currentMode = InputMode.Default;
    private bool isInitialized = false;
    private float menuButtonHoldTime = 0f;
    private const float MENU_HOLD_DURATION = 3f;

    // 按钮状态
    private bool aButtonPressed = false;
    private bool bButtonPressed = false;
    private bool xButtonPressed = false;
    private bool yButtonPressed = false;
    private bool menuButtonPressed = false;

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
            return;
        }
    }

    void Start()
    {
        InitializeInputSystem();
    }

    /// <summary>
    /// 初始化输入系统
    /// </summary>
    private void InitializeInputSystem()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[UnifiedVRInputManager] 初始化输入系统");
        }

        // 查找XR Origin
        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin == null)
            {
                Debug.LogError("[UnifiedVRInputManager] XR Origin未找到！");
                return;
            }
        }

        // 启用所有输入动作
        EnableInputActions();

        isInitialized = true;

        if (enableDebugLogs)
        {
            Debug.Log("[UnifiedVRInputManager] 输入系统初始化完成");
        }
    }

    /// <summary>
    /// 启用输入动作
    /// </summary>
    private void EnableInputActions()
    {
        // 启用抓取动作
        if (leftGrabAction != null)
        {
            leftGrabAction.action.Enable();
            leftGrabAction.action.performed += OnLeftGrabPerformed;
            leftGrabAction.action.canceled += OnLeftGrabCanceled;
        }

        if (rightGrabAction != null)
        {
            rightGrabAction.action.Enable();
            rightGrabAction.action.performed += OnRightGrabPerformed;
            rightGrabAction.action.canceled += OnRightGrabCanceled;
        }

        // 启用扳机动作
        if (leftTriggerAction != null)
        {
            leftTriggerAction.action.Enable();
            leftTriggerAction.action.performed += OnLeftTriggerPerformed;
        }

        if (rightTriggerAction != null)
        {
            rightTriggerAction.action.Enable();
            rightTriggerAction.action.performed += OnRightTriggerPerformed;
        }

        // 启用按钮动作
        if (aButtonAction != null)
        {
            aButtonAction.action.Enable();
            aButtonAction.action.performed += OnAButtonPressed;
            aButtonAction.action.canceled += OnAButtonReleased;
        }

        if (bButtonAction != null)
        {
            bButtonAction.action.Enable();
            bButtonAction.action.performed += OnBButtonPressed;
            bButtonAction.action.canceled += OnBButtonReleased;
        }

        if (xButtonAction != null)
        {
            xButtonAction.action.Enable();
            xButtonAction.action.performed += OnXButtonPressed;
            xButtonAction.action.canceled += OnXButtonReleased;
        }

        if (yButtonAction != null)
        {
            yButtonAction.action.Enable();
            yButtonAction.action.performed += OnYButtonPressed;
            yButtonAction.action.canceled += OnYButtonReleased;
        }

        if (menuButtonAction != null)
        {
            menuButtonAction.action.Enable();
            menuButtonAction.action.performed += OnMenuButtonPressedAction;
            menuButtonAction.action.canceled += OnMenuButtonReleasedAction;
        }

        // 启用摇杆动作
        if (leftThumbstickAction != null)
        {
            leftThumbstickAction.action.Enable();
            leftThumbstickAction.action.performed += OnLeftThumbstickPerformed;
        }

        if (rightThumbstickAction != null)
        {
            rightThumbstickAction.action.Enable();
            rightThumbstickAction.action.performed += OnRightThumbstickPerformed;
        }
    }

    // 输入动作回调
    private void OnLeftGrabPerformed(InputAction.CallbackContext context) => UpdateGrabState();
    private void OnLeftGrabCanceled(InputAction.CallbackContext context) => UpdateGrabState();
    private void OnRightGrabPerformed(InputAction.CallbackContext context) => UpdateGrabState();
    private void OnRightGrabCanceled(InputAction.CallbackContext context) => UpdateGrabState();

    private void OnLeftTriggerPerformed(InputAction.CallbackContext context)
    {
        // 左手扳机功能可在此添加
        if (enableDebugLogs) Debug.Log("[UnifiedVRInputManager] 左手扳机按下");
    }

    private void OnRightTriggerPerformed(InputAction.CallbackContext context)
    {
        OnTriggerPressed?.Invoke();
        if (enableDebugLogs) Debug.Log("[UnifiedVRInputManager] 右手扳机按下 - 拍照");
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        aButtonPressed = true;
        UpdateInputMode();
        if (enableDebugLogs) Debug.Log("[UnifiedVRInputManager] A键按下");
    }

    private void OnAButtonReleased(InputAction.CallbackContext context)
    {
        aButtonPressed = false;
        UpdateInputMode();
    }

    private void OnBButtonPressed(InputAction.CallbackContext context)
    {
        bButtonPressed = true;
        UpdateInputMode();
        if (enableDebugLogs) Debug.Log("[UnifiedVRInputManager] B键按下");
    }

    private void OnBButtonReleased(InputAction.CallbackContext context)
    {
        bButtonPressed = false;
        UpdateInputMode();
    }

    private void OnXButtonPressed(InputAction.CallbackContext context)
    {
        xButtonPressed = true;
        UpdateInputMode();
        if (enableDebugLogs) Debug.Log("[UnifiedVRInputManager] X键按下");
    }

    private void OnXButtonReleased(InputAction.CallbackContext context)
    {
        xButtonPressed = false;
        UpdateInputMode();
    }

    private void OnYButtonPressed(InputAction.CallbackContext context)
    {
        yButtonPressed = true;
        UpdateInputMode();
        if (enableDebugLogs) Debug.Log("[UnifiedVRInputManager] Y键按下");
    }

    private void OnYButtonReleased(InputAction.CallbackContext context)
    {
        yButtonPressed = false;
        UpdateInputMode();
    }

    private void OnMenuButtonPressedAction(InputAction.CallbackContext context)
    {
        menuButtonPressed = true;
        menuButtonHoldTime = 0f;
        OnMenuButtonPressed?.Invoke();
        if (enableDebugLogs) Debug.Log("[UnifiedVRInputManager] 菜单键按下");
    }

    private void OnMenuButtonReleasedAction(InputAction.CallbackContext context)
    {
        menuButtonPressed = false;
        menuButtonHoldTime = 0f;
        OnMenuButtonReleased?.Invoke();
        if (enableDebugLogs) Debug.Log("[UnifiedVRInputManager] 菜单键释放");
    }

    private void OnLeftThumbstickPerformed(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        OnLeftThumbstickMoved?.Invoke(value);
    }

    private void OnRightThumbstickPerformed(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        OnRightThumbstickMoved?.Invoke(value);
    }

    /// <summary>
    /// 更新输入模式
    /// </summary>
    private void UpdateInputMode()
    {
        InputMode newMode = InputMode.Default;

        if (aButtonPressed)
            newMode = InputMode.HoldA;
        else if (bButtonPressed)
            newMode = InputMode.HoldB;
        else if (xButtonPressed)
            newMode = InputMode.HoldX;
        else if (yButtonPressed)
            newMode = InputMode.HoldY;

        if (newMode != currentMode)
        {
            currentMode = newMode;
            OnInputModeChanged?.Invoke(currentMode);

            if (enableDebugLogs)
            {
                Debug.Log($"[UnifiedVRInputManager] 输入模式切换到: {currentMode}");
            }
        }
    }

    /// <summary>
    /// 更新抓取状态
    /// </summary>
    private void UpdateGrabState()
    {
        if (leftGrabAction != null && rightGrabAction != null)
        {
            float leftValue = leftGrabAction.action.ReadValue<float>();
            float rightValue = rightGrabAction.action.ReadValue<float>();
            OnGrabChanged?.Invoke(leftValue, rightValue);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // 处理菜单键长按
        HandleMenuButtonHold();
    }

    /// <summary>
    /// 处理菜单键长按返回
    /// </summary>
    /// <summary>
    /// 处理菜单键长按返回
    /// </summary>
    /// <summary>
    /// 处理菜单键长按返回
    /// </summary>
    private void HandleMenuButtonHold()
    {
        if (menuButtonPressed)
        {
            menuButtonHoldTime += Time.deltaTime;

            if (menuButtonHoldTime >= MENU_HOLD_DURATION)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[UnifiedVRInputManager] 菜单键长按3秒，返回主菜单");
                }

                // 触发返回主菜单事件
                if (VRSceneManager.Instance != null)
                {
                    VRSceneManager.Instance.LoadMainMenu();
                }
                else
                {
                    // 回退方案：直接加载主菜单
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                }

                menuButtonHoldTime = 0f; // 重置计时器
            }
        }
    }

    /// <summary>
    /// 获取当前输入模式
    /// </summary>
    public InputMode GetCurrentInputMode()
    {
        return currentMode;
    }

    /// <summary>
    /// 获取抓取值
    /// </summary>
    public Vector2 GetGrabValues()
    {
        Vector2 values = Vector2.zero;

        if (leftGrabAction != null)
            values.x = leftGrabAction.action.ReadValue<float>();

        if (rightGrabAction != null)
            values.y = rightGrabAction.action.ReadValue<float>();

        return values;
    }

    /// <summary>
    /// 获取摇杆值
    /// </summary>
    public Vector2 GetLeftThumbstickValue()
    {
        if (leftThumbstickAction != null)
            return leftThumbstickAction.action.ReadValue<Vector2>();
        return Vector2.zero;
    }

    /// <summary>
    /// 检查VR是否激活
    /// </summary>
    public bool IsVRActive()
    {
        return XRSettings.isDeviceActive;
    }

    /// <summary>
    /// 触发触觉反馈
    /// </summary>
    public void TriggerHapticFeedback(float intensity = 0.3f, float duration = 0.1f, bool rightHand = true)
    {
        // 这里可以添加触觉反馈逻辑
        // 使用新的Input System的触觉反馈API
        if (enableDebugLogs)
        {
            Debug.Log($"[UnifiedVRInputManager] 触觉反馈: 强度={intensity}, 持续时间={duration}, 右手={rightHand}");
        }
    }

    void OnDestroy()
    {
        // 禁用所有输入动作
        DisableInputActions();

        // 清理事件
        OnInputModeChanged = null;
        OnGrabChanged = null;
        OnLeftThumbstickMoved = null;
        OnRightThumbstickMoved = null;
        OnTriggerPressed = null;
        OnMenuButtonPressed = null;
        OnMenuButtonReleased = null;

        // 清理单例
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 禁用输入动作
    /// </summary>
    private void DisableInputActions()
    {
        if (leftGrabAction != null) leftGrabAction.action.Disable();
        if (rightGrabAction != null) rightGrabAction.action.Disable();
        if (leftTriggerAction != null) leftTriggerAction.action.Disable();
        if (rightTriggerAction != null) rightTriggerAction.action.Disable();
        if (aButtonAction != null) aButtonAction.action.Disable();
        if (bButtonAction != null) bButtonAction.action.Disable();
        if (xButtonAction != null) xButtonAction.action.Disable();
        if (yButtonAction != null) yButtonAction.action.Disable();
        if (menuButtonAction != null) menuButtonAction.action.Disable();
        if (leftThumbstickAction != null) leftThumbstickAction.action.Disable();
        if (rightThumbstickAction != null) rightThumbstickAction.action.Disable();
    }

    void OnGUI()
    {
        // 显示调试信息（仅在编辑器中）
        #if UNITY_EDITOR
        if (enableDebugLogs && isInitialized)
        {
            GUILayout.BeginArea(new Rect(10, 10, 250, 300));
            GUILayout.Label("统一VR输入管理器状态");
            GUILayout.Label("VR激活: " + (IsVRActive() ? "是" : "否"));
            GUILayout.Label("输入模式: " + currentMode);
            GUILayout.Label("菜单键按下: " + menuButtonPressed);
            GUILayout.Label("菜单键按住时间: " + menuButtonHoldTime.ToString("F1") + "s");

            GUILayout.Space(10);
            GUILayout.Label("按钮状态:");
            GUILayout.Label("A: " + aButtonPressed);
            GUILayout.Label("B: " + bButtonPressed);
            GUILayout.Label("X: " + xButtonPressed);
            GUILayout.Label("Y: " + yButtonPressed);

            Vector2 grabValues = GetGrabValues();
            GUILayout.Label("抓取值: " + grabValues);

            Vector2 leftStick = GetLeftThumbstickValue();
            GUILayout.Label("左摇杆: " + leftStick);

            GUILayout.EndArea();
        }
        #endif
    }
}