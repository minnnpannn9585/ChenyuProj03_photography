using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// VR相机测试器
/// 用于在编辑器中测试VR相机控制功能
/// </summary>
public class VRCameraTester : MonoBehaviour
{
    [Header("测试设置")]
    public bool enableTestMode = false;
    public bool simulateVRInput = true;
    public float testParameterChangeSpeed = 10f;

    [Header("测试UI")]
    public Canvas testCanvas;
    public TMP_Text statusText;
    public TMP_Text modeText;
    public TMP_Text parameterText;
    public Button[] testButtons;

    [Header("模拟手柄")]
    public Transform simulatedRightHand;
    public Transform simulatedLeftHand;
    public float handMoveSpeed = 2f;

    [Header("测试相机")]
    public VRCameraController testCameraController;
    public VRCameraModel testCameraModel;

    // 测试状态
    private VRCameraController.ControlMode currentTestMode = VRCameraController.ControlMode.Normal;
    private bool isSimulatingGrab = false;
    private bool isSimulatingParameterChange = false;
    private Vector3 rightHandTargetPosition;
    private Vector3 leftHandTargetPosition;

    // 键盘映射
    private readonly KeyCode modeToggleKey = KeyCode.M;
    private readonly KeyCode grabLeftKey = KeyCode.Q;
    private readonly KeyCode grabRightKey = KeyCode.E;
    private readonly KeyCode captureKey = KeyCode.Space;
    private readonly KeyCode holdFocusKey = KeyCode.X;
    private readonly KeyCode holdApertureKey = KeyCode.Y;
    private readonly KeyCode holdShutterKey = KeyCode.A;
    private readonly KeyCode holdISOKey = KeyCode.B;
    private readonly KeyCode menuKey = KeyCode.Escape;

    void Start()
    {
        if (enableTestMode)
        {
            InitializeTestMode();
        }
    }

    void Update()
    {
        if (!enableTestMode) return;

        HandleTestInput();
        UpdateSimulatedHands();
        UpdateTestUI();
    }

    /// <summary>
    /// 初始化测试模式
    /// </summary>
    private void InitializeTestMode()
    {
        // 创建测试Canvas
        if (testCanvas == null)
        {
            CreateTestCanvas();
        }

        // 查找相机组件
        if (testCameraController == null)
        {
            testCameraController = FindObjectOfType<VRCameraController>();
        }

        if (testCameraModel == null)
        {
            testCameraModel = FindObjectOfType<VRCameraModel>();
        }

        // 创建模拟手柄
        if (simulatedRightHand == null || simulatedLeftHand == null)
        {
            CreateSimulatedHands();
        }

        // 设置初始位置
        if (simulatedRightHand != null)
        {
            rightHandTargetPosition = simulatedRightHand.position;
        }

        if (simulatedLeftHand != null)
        {
            leftHandTargetPosition = simulatedLeftHand.position;
        }

        Debug.Log("VRCameraTester initialized in test mode");
        UpdateStatusText("Test Mode Ready");
    }

    /// <summary>
    /// 创建测试Canvas
    /// </summary>
    private void CreateTestCanvas()
    {
        GameObject canvasObj = new GameObject("TestCanvas");
        testCanvas = canvasObj.AddComponent<Canvas>();
        testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        testCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // 创建状态文本
        CreateStatusText();

        // 创建模式文本
        CreateModeText();

        // 创建参数文本
        CreateParameterText();

        // 创建测试按钮
        CreateTestButtons();
    }

    /// <summary>
    /// 创建状态文本
    /// </summary>
    private void CreateStatusText()
    {
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(testCanvas.transform, false);

        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 18;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = statusText.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(20, 20);
        rect.offsetMax = new Vector2(-20, -20);
    }

    /// <summary>
    /// 创建模式文本
    /// </summary>
    private void CreateModeText()
    {
        GameObject modeObj = new GameObject("ModeText");
        modeObj.transform.SetParent(testCanvas.transform, false);

        modeText = modeObj.AddComponent<TextMeshProUGUI>();
        modeText.fontSize = 24;
        modeText.color = Color.yellow;
        modeText.alignment = TextAlignmentOptions.Top;

        RectTransform rect = modeText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.8f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(-200, -100);
        rect.offsetMax = new Vector2(200, 0);
    }

    /// <summary>
    /// 创建参数文本
    /// </summary>
    private void CreateParameterText()
    {
        GameObject paramObj = new GameObject("ParameterText");
        paramObj.transform.SetParent(testCanvas.transform, false);

        parameterText = paramObj.AddComponent<TextMeshProUGUI>();
        parameterText.fontSize = 16;
        parameterText.color = Color.green;
        parameterText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = parameterText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20, 20);
        rect.offsetMax = new Vector2(-20, -20);
    }

    /// <summary>
    /// 创建测试按钮
    /// </summary>
    private void CreateTestButtons()
    {
        string[] buttonNames = { "Reset Camera", "Test Capture", "Toggle Mode", "Test UI" };
        KeyCode[] keys = { KeyCode.R, KeyCode.C, KeyCode.T, KeyCode.U };

        testButtons = new Button[buttonNames.Length];

        for (int i = 0; i < buttonNames.Length; i++)
        {
            GameObject buttonObj = new GameObject($"TestButton_{i}");
            buttonObj.transform.SetParent(testCanvas.transform, false);

            Button button = buttonObj.AddComponent<Button>();
            testButtons[i] = button;

            // 按钮背景
            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // 按钮文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"{buttonNames[i]} ({keys[i]})";
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // 按钮位置
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.1f + i * 0.08f);
            rect.anchorMax = new Vector2(0.3f, 0.15f + i * 0.08f);
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-20, 0);

            // 添加点击事件
            int index = i; // 避免闭包问题
            button.onClick.AddListener(() => OnTestButtonClicked(index));
        }
    }

    /// <summary>
    /// 创建模拟手柄
    /// </summary>
    private void CreateSimulatedHands()
    {
        // 右手
        GameObject rightHand = new GameObject("SimulatedRightHand");
        rightHand.transform.position = new Vector3(0.3f, 1.2f, 0.5f);
        simulatedRightHand = rightHand.transform;

        // 左手
        GameObject leftHand = new GameObject("SimulatedLeftHand");
        leftHand.transform.position = new Vector3(-0.3f, 1.2f, 0.5f);
        simulatedLeftHand = leftHand.transform;

        // 添加可视化
        CreateHandVisual(rightHand);
        CreateHandVisual(leftHand);
    }

    /// <summary>
    /// 创建手部可视化
    /// </summary>
    private void CreateHandVisual(GameObject hand)
    {
        // 创建简单的手部模型
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "HandVisual";
        visual.transform.SetParent(hand.transform);
        visual.transform.localScale = Vector3.one * 0.05f;

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.material.color = hand.name.Contains("Right") ? Color.blue : Color.red;

        // 添加方向指示器
        GameObject direction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        direction.name = "Direction";
        direction.transform.SetParent(hand.transform);
        direction.transform.localPosition = Vector3.forward * 0.1f;
        direction.transform.localScale = new Vector3(0.02f, 0.02f, 0.1f);

        Renderer dirRenderer = direction.GetComponent<Renderer>();
        dirRenderer.material.color = hand.name.Contains("Right") ? Color.cyan : Color.magenta;
    }

    /// <summary>
    /// 处理测试输入
    /// </summary>
    private void HandleTestInput()
    {
        // 模式切换
        if (Input.GetKeyDown(modeToggleKey))
        {
            CycleTestMode();
        }

        // 抓取模拟
        bool leftGrab = Input.GetKey(grabLeftKey);
        bool rightGrab = Input.GetKey(grabRightKey);

        if (leftGrab || rightGrab)
        {
            isSimulatingGrab = true;
            SimulateGrab(leftGrab, rightGrab);
        }
        else
        {
            isSimulatingGrab = false;
        }

        // 模式按键模拟
        bool focusMode = Input.GetKey(holdFocusKey);
        bool apertureMode = Input.GetKey(holdApertureKey);
        bool shutterMode = Input.GetKey(holdShutterKey);
        bool isoMode = Input.GetKey(holdISOKey);

        if (focusMode) currentTestMode = VRCameraController.ControlMode.Focus;
        else if (apertureMode) currentTestMode = VRCameraController.ControlMode.Aperture;
        else if (shutterMode) currentTestMode = VRCameraController.ControlMode.Shutter;
        else if (isoMode) currentTestMode = VRCameraController.ControlMode.ISO;
        else currentTestMode = VRCameraController.ControlMode.Normal;

        // 拍照
        if (Input.GetKeyDown(captureKey))
        {
            TestCapturePhoto();
        }

        // 手柄移动
        HandleHandMovement();

        // 快捷键
        if (Input.GetKeyDown(KeyCode.R)) OnTestButtonClicked(0); // Reset
        if (Input.GetKeyDown(KeyCode.C)) OnTestButtonClicked(1); // Capture
        if (Input.GetKeyDown(KeyCode.T)) OnTestButtonClicked(2); // Toggle Mode
        if (Input.GetKeyDown(KeyCode.U)) OnTestButtonClicked(3); // Test UI
    }

    /// <summary>
    /// 处理手部移动
    /// </summary>
    private void HandleHandMovement()
    {
        Vector3 moveInput = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) moveInput += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) moveInput -= Vector3.forward;
        if (Input.GetKey(KeyCode.A)) moveInput -= Vector3.right;
        if (Input.GetKey(KeyCode.D)) moveInput += Vector3.right;
        if (Input.GetKey(KeyCode.LeftShift)) moveInput += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) moveInput -= Vector3.up;

        if (moveInput != Vector3.zero)
        {
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                // 移动右手
                rightHandTargetPosition += moveInput * handMoveSpeed * Time.deltaTime;
            }
            else
            {
                // 移动两手
                rightHandTargetPosition += moveInput * handMoveSpeed * Time.deltaTime;
                leftHandTargetPosition += moveInput * handMoveSpeed * Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// 更新模拟手柄
    /// </summary>
    private void UpdateSimulatedHands()
    {
        if (simulatedRightHand != null)
        {
            simulatedRightHand.position = Vector3.Lerp(
                simulatedRightHand.position,
                rightHandTargetPosition,
                Time.deltaTime * 5f
            );
        }

        if (simulatedLeftHand != null)
        {
            simulatedLeftHand.position = Vector3.Lerp(
                simulatedLeftHand.position,
                leftHandTargetPosition,
                Time.deltaTime * 5f
            );
        }
    }

    /// <summary>
    /// 模拟抓取
    /// </summary>
    private void SimulateGrab(bool leftGrab, bool rightGrab)
    {
        if (testCameraController == null) return;

        // 根据当前模式调整参数
        float delta = 0f;

        if (rightGrab && !leftGrab) delta = 1f;
        else if (!rightGrab && leftGrab) delta = -1f;
        else if (rightGrab && leftGrab) delta = 0.5f;

        if (Mathf.Abs(delta) > 0f)
        {
            switch (currentTestMode)
            {
                case VRCameraController.ControlMode.Normal:
                    TestAdjustFocalLength(delta);
                    break;
                case VRCameraController.ControlMode.Focus:
                    TestAdjustFocusDistance(delta);
                    break;
                case VRCameraController.ControlMode.Aperture:
                    TestAdjustAperture(delta);
                    break;
                case VRCameraController.ControlMode.Shutter:
                    TestAdjustShutterSpeed(delta);
                    break;
                case VRCameraController.ControlMode.ISO:
                    TestAdjustISO(delta);
                    break;
            }
        }
    }

    /// <summary>
    /// 测试调整参数的方法
    /// </summary>
    private void TestAdjustISO(float delta)
    {
        if (testCameraController?.isoSlider != null)
        {
            float newValue = Mathf.Clamp(
                testCameraController.isoSlider.value + delta * testParameterChangeSpeed * Time.deltaTime,
                testCameraController.isoSlider.minValue,
                testCameraController.isoSlider.maxValue
            );
            testCameraController.isoSlider.value = newValue;
        }
    }

    private void TestAdjustAperture(float delta)
    {
        if (testCameraController?.apertureSlider != null)
        {
            float newValue = Mathf.Clamp(
                testCameraController.apertureSlider.value - delta * testParameterChangeSpeed * Time.deltaTime,
                testCameraController.apertureSlider.minValue,
                testCameraController.apertureSlider.maxValue
            );
            testCameraController.apertureSlider.value = newValue;
        }
    }

    private void TestAdjustShutterSpeed(float delta)
    {
        if (testCameraController?.shutterSlider != null)
        {
            float newValue = Mathf.Clamp(
                testCameraController.shutterSlider.value - delta * testParameterChangeSpeed * Time.deltaTime,
                testCameraController.shutterSlider.minValue,
                testCameraController.shutterSlider.maxValue
            );
            testCameraController.shutterSlider.value = newValue;
        }
    }

    private void TestAdjustFocalLength(float delta)
    {
        if (testCameraController?.focalLengthSlider != null)
        {
            float newValue = Mathf.Clamp(
                testCameraController.focalLengthSlider.value + delta * testParameterChangeSpeed * Time.deltaTime,
                testCameraController.focalLengthSlider.minValue,
                testCameraController.focalLengthSlider.maxValue
            );
            testCameraController.focalLengthSlider.value = newValue;
        }
    }

    private void TestAdjustFocusDistance(float delta)
    {
        if (testCameraController?.focusDistanceSlider != null)
        {
            float newValue = Mathf.Clamp(
                testCameraController.focusDistanceSlider.value + delta * testParameterChangeSpeed * Time.deltaTime,
                testCameraController.focusDistanceSlider.minValue,
                testCameraController.focusDistanceSlider.maxValue
            );
            testCameraController.focusDistanceSlider.value = newValue;
        }
    }

    /// <summary>
    /// 测试拍照
    /// </summary>
    private void TestCapturePhoto()
    {
        if (testCameraController != null)
        {
            testCameraController.CapturePhoto();
            UpdateStatusText("Photo Captured!");
        }
    }

    /// <summary>
    /// 循环测试模式
    /// </summary>
    private void CycleTestMode()
    {
        currentTestMode = (VRCameraController.ControlMode)(((int)currentTestMode + 1) % 5);
        UpdateStatusText($"Test Mode: {currentTestMode}");
    }

    /// <summary>
    /// 测试按钮点击
    /// </summary>
    private void OnTestButtonClicked(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case 0: // Reset Camera
                if (testCameraModel != null)
                {
                    testCameraModel.ResetCamera();
                    UpdateStatusText("Camera Reset");
                }
                break;

            case 1: // Test Capture
                TestCapturePhoto();
                break;

            case 2: // Toggle Mode
                CycleTestMode();
                break;

            case 3: // Test UI
                ToggleTestUI();
                break;
        }
    }

    /// <summary>
    /// 切换测试UI
    /// </summary>
    private void ToggleTestUI()
    {
        if (testCanvas != null)
        {
            bool isActive = testCanvas.gameObject.activeSelf;
            testCanvas.gameObject.SetActive(!isActive);
            UpdateStatusText(isActive ? "Test UI Hidden" : "Test UI Shown");
        }
    }

    /// <summary>
    /// 更新测试UI
    /// </summary>
    private void UpdateTestUI()
    {
        // 更新模式文本
        if (modeText != null)
        {
            modeText.text = $"Mode: {currentTestMode}\n" +
                           $"Grab: {(isSimulatingGrab ? "YES" : "NO")}";
        }

        // 更新参数文本
        if (parameterText != null && testCameraController != null)
        {
            string paramText = "Camera Parameters:\n";

            if (testCameraController.isoSlider != null)
                paramText += $"ISO: {testCameraController.isoSlider.value:F0}\n";

            if (testCameraController.apertureSlider != null)
                paramText += $"Aperture: f/{testCameraController.apertureSlider.value:F1}\n";

            if (testCameraController.shutterSlider != null)
                paramText += $"Shutter: {testCameraController.shutterSlider.value:F4}s\n";

            if (testCameraController.focalLengthSlider != null)
                paramText += $"Focal Length: {testCameraController.focalLengthSlider.value:F0}mm\n";

            if (testCameraController.focusDistanceSlider != null)
                paramText += $"Focus Distance: {testCameraController.focusDistanceSlider.value:F1}m\n";

            paramText += "\nControls:\n" +
                         "WASD: Move hands (Alt = Right only)\n" +
                         "Shift/Ctrl: Up/Down\n" +
                         $"Q/E: Grab Left/Right\n" +
                         $"X/Y/A/B: Hold for modes\n" +
                         $"Space: Capture\n" +
                         $"M: Toggle mode";

            parameterText.text = paramText;
        }
    }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"VR Camera Tester\n" +
                            $"Status: {message}\n" +
                            $"Time: {System.DateTime.Now.ToString("HH:mm:ss")}";
        }

        Debug.Log($"[VRCameraTester] {message}");
    }

    /// <summary>
    /// 创建测试相机组件
    /// </summary>
    [ContextMenu("Create Test Camera")]
    public void CreateTestCamera()
    {
        // 创建相机控制器
        if (testCameraController == null)
        {
            GameObject controllerObj = new GameObject("TestCameraController");
            testCameraController = controllerObj.AddComponent<VRCameraController>();

            // 设置基本参数
            testCameraController.captureWidth = 1280;
            testCameraController.captureHeight = 720;
        }

        // 创建相机模型
        if (testCameraModel == null)
        {
            GameObject modelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            modelObj.name = "TestCameraModel";
            modelObj.transform.localScale = new Vector3(0.1f, 0.06f, 0.15f);
            testCameraModel = modelObj.AddComponent<VRCameraModel>();

            // 关联组件
            testCameraModel.cameraController = testCameraController;
            testCameraController.cameraModel = modelObj;
        }

        UpdateStatusText("Test Camera Created");
    }

    void OnDrawGizmos()
    {
        if (enableTestMode)
        {
            // 绘制手柄位置
            if (simulatedRightHand != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(simulatedRightHand.position, 0.1f);
                Gizmos.DrawRay(simulatedRightHand.position, simulatedRightHand.forward * 0.2f);
            }

            if (simulatedLeftHand != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(simulatedLeftHand.position, 0.1f);
                Gizmos.DrawRay(simulatedLeftHand.position, simulatedLeftHand.forward * 0.2f);
            }

            // 绘制目标位置
            Gizmos.color = Color.yellow;
            if (simulatedRightHand != null)
                Gizmos.DrawWireCube(rightHandTargetPosition, Vector3.one * 0.02f);
            if (simulatedLeftHand != null)
                Gizmos.DrawWireCube(leftHandTargetPosition, Vector3.one * 0.02f);
        }
    }
}