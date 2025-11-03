using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

/// <summary>
/// VR输入管理器 - 处理PhotoScene中复杂的控制器输入组合
/// 管理所有相机参数调节的输入逻辑
/// </summary>
public class VRInputManager : MonoBehaviour
{
    [Header("相机系统引用")]
    public VRCameraRig vrCameraRig; // VR相机装备
    public CameraController cameraController; // 相机控制器

    [Header("输入设置")]
    public float parameterSensitivity = 0.1f; // 参数调节灵敏度
    public float fineModeMultiplier = 0.1f; // 精细模式倍数
    public bool invertFocusDirection = false; // 是否反转对焦方向

    [Header("触觉反馈")]
    public float hapticIntensity = 0.3f; // 触觉反馈强度
    public float hapticDuration = 0.1f; // 触觉反馈持续时间

    [Header("UI引用")]
    public VRParameterDisplay parameterDisplay; // 参数显示UI

    // 输入模式枚举
    public enum InputMode
    {
        Default,        // 默认模式 - 调整焦段
        HoldA,          // 按住A - 调整对焦距离
        HoldB,          // 按住B - 调整ISO
        HoldX,          // 按住X - 调整光圈
        HoldY           // 按住Y - 调整快门速度
    }

    // 私有变量
    private InputMode currentMode = InputMode.Default;
    private bool isInitialized = false;
    private XRController leftController;
    private XRController rightController;

    // 按键状态跟踪
    private bool aButtonPressed = false;
    private bool bButtonPressed = false;
    private bool xButtonPressed = false;
    private bool yButtonPressed = false;
    private bool leftGrabPressed = false;
    private bool rightGrabPressed = false;

    // 参数调节缓存
    private float lastParameterChangeTime = 0f;
    private const float PARAMETER_CHANGE_COOLDOWN = 0.05f; // 参数调节冷却时间

    void Start()
    {
        InitializeInputManager();
    }

    /// <summary>
    /// 初始化输入管理器
    /// </summary>
    private void InitializeInputManager()
    {
        Debug.Log("初始化VR输入管理器...");

        // 查找VR相机装备
        if (vrCameraRig == null)
        {
            vrCameraRig = FindObjectOfType<VRCameraRig>();
        }

        if (vrCameraRig == null)
        {
            Debug.LogError("VRCameraRig未找到！");
            return;
        }

        // 获取相机控制器
        if (cameraController == null && vrCameraRig != null)
        {
            cameraController = vrCameraRig.GetCameraController();
        }

        // 查找XR控制器
        FindXRControllers();

        // 验证所有必要组件
        ValidateComponents();

        isInitialized = true;
        Debug.Log("VR输入管理器初始化完成");
    }

    /// <summary>
    /// 查找XR控制器
    /// </summary>
    private void FindXRControllers()
    {
        // XR Interaction Toolkit 2.6.5+ 新方式
        // 直接查找所有Controller组件
        XRController[] controllers = FindObjectsOfType<XRController>();

        if (controllers.Length == 0)
        {
            Debug.LogWarning("未找到XR控制器！尝试查找Action-based Controller...");
            // 如果没有找到XRController，尝试查找其他控制器类型
            var actionBasedControllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
            if (actionBasedControllers.Length > 0)
            {
                Debug.Log($"找到 {actionBasedControllers.Length} 个Action-based Controller");
                // 由于Action-based Controller不是XRController类型，我们直接返回
                return;
            }
        }

        foreach (XRController controller in controllers)
        {
            // 在新版本中，检查是否是左手控制器
            if (controller.name.ToLower().Contains("left") ||
                (controller.transform.parent != null && controller.transform.parent.name.ToLower().Contains("left")))
            {
                leftController = controller;
                Debug.Log("找到左手控制器: " + controller.name);
            }
            // 检查是否是右手控制器
            else if (controller.name.ToLower().Contains("right") ||
                     (controller.transform.parent != null && controller.transform.parent.name.ToLower().Contains("right")))
            {
                rightController = controller;
                Debug.Log("找到右手控制器: " + controller.name);
            }

            // 如果无法通过名称判断，使用位置判断
            else if (controller.transform.position.x < 0)
            {
                leftController = controller;
                Debug.Log("通过位置找到左手控制器: " + controller.name);
            }
            else
            {
                rightController = controller;
                Debug.Log("通过位置找到右手控制器: " + controller.name);
            }
        }

        if (leftController == null || rightController == null)
        {
            Debug.LogError($"未找到完整的XR控制器！左手: {(leftController != null ? "找到" : "未找到")}, 右手: {(rightController != null ? "找到" : "未找到")}");
        }
    }

    /// <summary>
    /// 验证必要组件
    /// </summary>
    private void ValidateComponents()
    {
        if (cameraController == null)
        {
            Debug.LogError("CameraController未找到！");
        }

        if (parameterDisplay == null)
        {
            parameterDisplay = FindObjectOfType<VRParameterDisplay>();
            if (parameterDisplay == null)
            {
                Debug.LogWarning("VRParameterDisplay未找到，将跳过UI更新");
            }
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // 更新按键状态
        UpdateButtonStates();

        // 更新输入模式
        UpdateInputMode();

        // 处理参数调节
        HandleParameterAdjustment();

        // 处理拍照
        HandlePhotoCapture();

        // 处理移动
        HandleMovement();
    }

    /// <summary>
    /// 更新按键状态
    /// </summary>
    private void UpdateButtonStates()
    {
        // 右手控制器按键
        aButtonPressed = Input.GetKey(KeyCode.JoystickButton0) || OVRInput.Get(OVRInput.Button.One);
        bButtonPressed = Input.GetKey(KeyCode.JoystickButton1) || OVRInput.Get(OVRInput.Button.Two);
        bool rightGrab = Input.GetKey(KeyCode.JoystickButton14) || OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);

        // 左手控制器按键
        xButtonPressed = Input.GetKey(KeyCode.JoystickButton2) || OVRInput.Get(OVRInput.Button.Three);
        yButtonPressed = Input.GetKey(KeyCode.JoystickButton3) || OVRInput.Get(OVRInput.Button.Four);
        leftGrabPressed = Input.GetKey(KeyCode.JoystickButton13) || OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

        // 更新右手Grab状态
        rightGrabPressed = rightGrab;
    }

    /// <summary>
    /// 更新输入模式
    /// </summary>
    private void UpdateInputMode()
    {
        InputMode newMode = InputMode.Default;

        if (aButtonPressed)
        {
            newMode = InputMode.HoldA;
        }
        else if (bButtonPressed)
        {
            newMode = InputMode.HoldB;
        }
        else if (xButtonPressed)
        {
            newMode = InputMode.HoldX;
        }
        else if (yButtonPressed)
        {
            newMode = InputMode.HoldY;
        }

        // 如果模式发生变化，提供触觉反馈
        if (newMode != currentMode)
        {
            currentMode = newMode;
            OnInputModeChanged();
        }
    }

    /// <summary>
    /// 输入模式变化处理
    /// </summary>
    private void OnInputModeChanged()
    {
        Debug.Log("输入模式切换到: " + currentMode);

        // 提供模式切换的触觉反馈
        if (rightController != null)
        {
            OVRInput.SetControllerVibration(1f, 1f, OVRInput.Controller.RTouch);
        }

        // 更新UI显示
        if (parameterDisplay != null)
        {
            parameterDisplay.UpdateModeDisplay(currentMode);
        }
    }

    /// <summary>
    /// 处理参数调节
    /// </summary>
    private void HandleParameterAdjustment()
    {
        if (cameraController == null) return;

        // 检查冷却时间
        if (Time.time - lastParameterChangeTime < PARAMETER_CHANGE_COOLDOWN)
        {
            return;
        }

        bool parameterChanged = false;

        // 根据当前模式调节对应参数
        switch (currentMode)
        {
            case InputMode.Default:
                parameterChanged = AdjustFocalLength();
                break;
            case InputMode.HoldA:
                parameterChanged = AdjustFocusDistance();
                break;
            case InputMode.HoldB:
                parameterChanged = AdjustISO();
                break;
            case InputMode.HoldX:
                parameterChanged = AdjustAperture();
                break;
            case InputMode.HoldY:
                parameterChanged = AdjustShutterSpeed();
                break;
        }

        if (parameterChanged)
        {
            lastParameterChangeTime = Time.time;
            OnParameterChanged();
        }
    }

    /// <summary>
    /// 调整焦段
    /// </summary>
    private bool AdjustFocalLength()
    {
        bool changed = false;

        if (leftGrabPressed)
        {
            cameraController.focalLengthSlider.value -= parameterSensitivity;
            changed = true;
        }

        if (rightGrabPressed)
        {
            cameraController.focalLengthSlider.value += parameterSensitivity;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 调整对焦距离
    /// </summary>
    private bool AdjustFocusDistance()
    {
        bool changed = false;

        if (leftGrabPressed)
        {
            float direction = invertFocusDirection ? 1f : -1f;
            cameraController.focusDistanceSlider.value += direction * parameterSensitivity;
            changed = true;
        }

        if (rightGrabPressed)
        {
            float direction = invertFocusDirection ? -1f : 1f;
            cameraController.focusDistanceSlider.value += direction * parameterSensitivity;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 调整ISO
    /// </summary>
    private bool AdjustISO()
    {
        bool changed = false;

        if (leftGrabPressed)
        {
            cameraController.isoSlider.value -= parameterSensitivity;
            changed = true;
        }

        if (rightGrabPressed)
        {
            cameraController.isoSlider.value += parameterSensitivity;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 调整光圈
    /// </summary>
    private bool AdjustAperture()
    {
        bool changed = false;

        if (leftGrabPressed)
        {
            cameraController.apertureSlider.value += parameterSensitivity;
            changed = true;
        }

        if (rightGrabPressed)
        {
            cameraController.apertureSlider.value -= parameterSensitivity;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 调整快门速度
    /// </summary>
    private bool AdjustShutterSpeed()
    {
        bool changed = false;

        if (leftGrabPressed)
        {
            cameraController.shutterSlider.value -= parameterSensitivity;
            changed = true;
        }

        if (rightGrabPressed)
        {
            cameraController.shutterSlider.value += parameterSensitivity;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 参数变化处理
    /// </summary>
    private void OnParameterChanged()
    {
        // 调用相机控制器的参数更新方法
        if (cameraController != null)
        {
            cameraController.OnParameterChanged();
        }

        // 播放参数调节音效
        if (vrCameraRig != null)
        {
            vrCameraRig.PlayParameterChangeSound();
        }

        // 提供触觉反馈
        if (rightController != null)
        {
            OVRInput.SetControllerVibration(hapticIntensity, hapticIntensity, OVRInput.Controller.RTouch);
        }

        // 更新UI显示
        if (parameterDisplay != null)
        {
            parameterDisplay.UpdateParameterDisplay();
        }
    }

    /// <summary>
    /// 处理拍照
    /// </summary>
    private void HandlePhotoCapture()
    {
        if (cameraController == null) return;

        // 检测右手扳机按下
        bool rightTrigger = Input.GetKeyDown(KeyCode.JoystickButton15) || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);

        if (rightTrigger)
        {
            // 执行拍照
            cameraController.CapturePhoto();

            // 播放快门音效
            if (vrCameraRig != null)
            {
                vrCameraRig.PlayShutterSound();
            }

            // 提供拍照触觉反馈
            if (rightController != null)
            {
                OVRInput.SetControllerVibration(1f, 1f, OVRInput.Controller.RTouch);
            }

            Debug.Log("VR拍照执行");
        }
    }

    /// <summary>
    /// 处理移动
    /// </summary>
    private void HandleMovement()
    {
        // 这里可以添加VR移动逻辑
        // 通常由PlayerController或其他移动系统处理
        Vector2 leftJoystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (Mathf.Abs(leftJoystick.x) > 0.1f || Mathf.Abs(leftJoystick.y) > 0.1f)
        {
            // 可以在这里触发移动事件或更新移动状态
            // 具体移动实现可能在PlayerController中
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
    /// 设置参数灵敏度
    /// </summary>
    public void SetParameterSensitivity(float sensitivity)
    {
        parameterSensitivity = Mathf.Max(0.01f, sensitivity);
    }

    /// <summary>
    /// 设置触觉反馈强度
    /// </summary>
    public void SetHapticIntensity(float intensity)
    {
        hapticIntensity = Mathf.Clamp01(intensity);
    }

    /// <summary>
    /// 切换精细模式
    /// </summary>
    public void ToggleFineMode()
    {
        if (parameterSensitivity > fineModeMultiplier)
        {
            parameterSensitivity = fineModeMultiplier;
        }
        else
        {
            parameterSensitivity = 0.1f; // 恢复默认灵敏度
        }

        Debug.Log("参数灵敏度: " + parameterSensitivity);
    }

    void OnGUI()
    {
        if (!isInitialized) return;

        // 显示调试信息（仅在编辑器中）
        #if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Label("VR输入管理器状态");
        GUILayout.Label("当前模式: " + currentMode);
        GUILayout.Label("左手控制器: " + (leftController != null ? "已连接" : "未找到"));
        GUILayout.Label("右手控制器: " + (rightController != null ? "已连接" : "未找到"));
        GUILayout.Label("A键: " + (aButtonPressed ? "按下" : "释放"));
        GUILayout.Label("B键: " + (bButtonPressed ? "按下" : "释放"));
        GUILayout.Label("X键: " + (xButtonPressed ? "按下" : "释放"));
        GUILayout.Label("Y键: " + (yButtonPressed ? "按下" : "释放"));
        GUILayout.Label("左Grab: " + (leftGrabPressed ? "按下" : "释放"));
        GUILayout.Label("右Grab: " + (rightGrabPressed ? "按下" : "释放"));
        GUILayout.Label("参数灵敏度: " + parameterSensitivity);

        if (GUILayout.Button("切换精细模式"))
        {
            ToggleFineMode();
        }

        GUILayout.EndArea();
        #endif
    }
}