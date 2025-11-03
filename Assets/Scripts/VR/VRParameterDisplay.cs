using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// VR参数显示器 - 在PhotoScene中实时显示相机参数
/// 提供VR友好的UI显示和交互反馈
/// </summary>
public class VRParameterDisplay : MonoBehaviour
{
    [Header("UI组件")]
    public Canvas displayCanvas; // 显示Canvas
    public TextMeshProUGUI modeText; // 当前模式文本
    public TextMeshProUGUI focalLengthText; // 焦段文本
    public TextMeshProUGUI focusDistanceText; // 对焦距离文本
    public TextMeshProUGUI isoText; // ISO文本
    public TextMeshProUGUI apertureText; // 光圈文本
    public TextMeshProUGUI shutterSpeedText; // 快门速度文本

    [Header("显示设置")]
    public float displayDistance = 1.5f; // 显示距离
    public Vector3 displayOffset = Vector3.zero; // 显示偏移
    public bool followCamera = true; // 是否跟随相机
    public float smoothFollowSpeed = 5f; // 平滑跟随速度
    public bool allowPositionControl = false; // 是否允许脚本控制UI位置（关闭时保持Editor设置）

    [Header("样式设置")]
    public Color normalColor = Color.white; // 正常颜色
    public Color activeColor = Color.green; // 激活颜色
    public Color warningColor = Color.yellow; // 警告颜色
    public float textSize = 24f; // 文字大小

    [Header("相机引用")]
    public Camera targetCamera; // 目标相机
    public CameraController cameraController; // 相机控制器

    // 私有变量
    private VRInputManager.InputMode currentMode = VRInputManager.InputMode.Default;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isInitialized = false;

    // 参数缓存
    private float lastFocalLength = 0f;
    private float lastFocusDistance = 0f;
    private float lastISO = 0f;
    private float lastAperture = 0f;
    private float lastShutterSpeed = 0f;

    void Start()
    {
        InitializeParameterDisplay();
    }

    /// <summary>
    /// 初始化参数显示器
    /// </summary>
    private void InitializeParameterDisplay()
    {
        Debug.Log("初始化VR参数显示器...");

        // 查找必要的组件
        FindRequiredComponents();

        // 设置Canvas
        SetupCanvas();

        // 设置文本样式
        SetupTextStyles();

        // 初始化显示位置
        InitializeDisplayPosition();

        // 更新初始显示
        UpdateParameterDisplay();

        isInitialized = true;
        Debug.Log("VR参数显示器初始化完成");
    }

    /// <summary>
    /// 查找必要组件
    /// </summary>
    private void FindRequiredComponents()
    {
        // 查找相机控制器
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }

        // 查找目标相机
        if (targetCamera == null && cameraController != null)
        {
            targetCamera = cameraController.photographyCamera;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (cameraController == null)
        {
            Debug.LogError("CameraController未找到！");
        }

        if (targetCamera == null)
        {
            Debug.LogError("目标相机未找到！");
        }
    }

    /// <summary>
    /// 设置Canvas
    /// </summary>
    private void SetupCanvas()
    {
        if (displayCanvas == null)
        {
            Debug.LogError("显示Canvas未设置！");
            return;
        }

        // 确保Canvas是World Space模式
        displayCanvas.renderMode = RenderMode.WorldSpace;

        // 不强制设置缩放，保持用户在Editor中设置的原始状态
        // 这样用户可以手动调整UI大小到合适的尺寸
    }

    /// <summary>
    /// 设置文本样式
    /// </summary>
    private void SetupTextStyles()
    {
        // 设置所有文本的初始样式
        TextMeshProUGUI[] allTexts = { modeText, focalLengthText, focusDistanceText, isoText, apertureText, shutterSpeedText };

        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text != null)
            {
                text.fontSize = textSize;
                text.color = normalColor;
                text.alignment = TextAlignmentOptions.Left;
                text.fontStyle = FontStyles.Normal;
            }
        }

        // 设置模式文本样式
        if (modeText != null)
        {
            modeText.fontSize = textSize * 1.2f;
            modeText.fontStyle = FontStyles.Bold;
        }
    }

    /// <summary>
    /// 初始化显示位置
    /// </summary>
    private void InitializeDisplayPosition()
    {
        if (targetCamera == null || !allowPositionControl) return;

        // 只有在允许位置控制时才设置初始位置
        UpdateDisplayPosition();
    }

    /// <summary>
    /// 更新显示位置
    /// </summary>
    private void UpdateDisplayPosition()
    {
        if (targetCamera == null || displayCanvas == null) return;

        // 计算目标位置（相机前方偏移）
        targetPosition = targetCamera.transform.position + targetCamera.transform.forward * displayDistance + displayOffset;

        // 设置目标旋转（面向相机）
        targetRotation = Quaternion.LookRotation(targetCamera.transform.forward * -1f, targetCamera.transform.up);
    }

    void Update()
    {
        if (!isInitialized) return;

        // 只有在允许位置控制时才更新显示位置
        if (allowPositionControl && followCamera)
        {
            UpdateDisplayPosition();

            // 平滑移动到目标位置
            if (displayCanvas != null)
            {
                displayCanvas.transform.position = Vector3.Lerp(displayCanvas.transform.position, targetPosition, Time.deltaTime * smoothFollowSpeed);
                displayCanvas.transform.rotation = Quaternion.Slerp(displayCanvas.transform.rotation, targetRotation, Time.deltaTime * smoothFollowSpeed);
            }
        }

        // 定期更新参数显示
        UpdateParameterDisplay();
    }

    /// <summary>
    /// 更新模式显示
    /// </summary>
    public void UpdateModeDisplay(VRInputManager.InputMode mode)
    {
        currentMode = mode;

        if (modeText != null)
        {
            string modeString = GetModeString(mode);
            modeText.text = "模式: " + modeString;

            // 根据模式设置颜色
            switch (mode)
            {
                case VRInputManager.InputMode.Default:
                    modeText.color = normalColor;
                    break;
                case VRInputManager.InputMode.HoldA:
                case VRInputManager.InputMode.HoldB:
                case VRInputManager.InputMode.HoldX:
                case VRInputManager.InputMode.HoldY:
                    modeText.color = activeColor;
                    break;
            }
        }

        // 高亮当前调节的参数
        HighlightCurrentParameter(mode);
    }

    /// <summary>
    /// 获取模式字符串
    /// </summary>
    private string GetModeString(VRInputManager.InputMode mode)
    {
        switch (mode)
        {
            case VRInputManager.InputMode.Default:
                return "焦段调节";
            case VRInputManager.InputMode.HoldA:
                return "对焦距离";
            case VRInputManager.InputMode.HoldB:
                return "ISO感光度";
            case VRInputManager.InputMode.HoldX:
                return "光圈调节";
            case VRInputManager.InputMode.HoldY:
                return "快门速度";
            default:
                return "未知模式";
        }
    }

    /// <summary>
    /// 高亮当前参数
    /// </summary>
    private void HighlightCurrentParameter(VRInputManager.InputMode mode)
    {
        // 重置所有文本颜色
        ResetAllTextColors();

        // 高亮当前调节的参数
        switch (mode)
        {
            case VRInputManager.InputMode.Default:
                if (focalLengthText != null) focalLengthText.color = activeColor;
                break;
            case VRInputManager.InputMode.HoldA:
                if (focusDistanceText != null) focusDistanceText.color = activeColor;
                break;
            case VRInputManager.InputMode.HoldB:
                if (isoText != null) isoText.color = activeColor;
                break;
            case VRInputManager.InputMode.HoldX:
                if (apertureText != null) apertureText.color = activeColor;
                break;
            case VRInputManager.InputMode.HoldY:
                if (shutterSpeedText != null) shutterSpeedText.color = activeColor;
                break;
        }
    }

    /// <summary>
    /// 重置所有文本颜色
    /// </summary>
    private void ResetAllTextColors()
    {
        TextMeshProUGUI[] allTexts = { focalLengthText, focusDistanceText, isoText, apertureText, shutterSpeedText };

        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text != null)
            {
                text.color = normalColor;
            }
        }
    }

    /// <summary>
    /// 更新参数显示
    /// </summary>
    public void UpdateParameterDisplay()
    {
        if (cameraController == null) return;

        // 获取当前参数值
        float focalLength = cameraController.focalLengthSlider.value;
        float focusDistance = cameraController.focusDistanceSlider.value;
        float iso = cameraController.isoSlider.value;
        float aperture = cameraController.apertureSlider.value;
        float shutterSpeed = cameraController.shutterSlider.value;

        // 检查参数是否发生变化
        bool parametersChanged =
            Mathf.Abs(focalLength - lastFocalLength) > 0.01f ||
            Mathf.Abs(focusDistance - lastFocusDistance) > 0.01f ||
            Mathf.Abs(iso - lastISO) > 0.01f ||
            Mathf.Abs(aperture - lastAperture) > 0.01f ||
            Mathf.Abs(shutterSpeed - lastShutterSpeed) > 0.0001f;

        if (parametersChanged)
        {
            // 更新参数显示
            UpdateParameterTexts(focalLength, focusDistance, iso, aperture, shutterSpeed);

            // 更新缓存
            lastFocalLength = focalLength;
            lastFocusDistance = focusDistance;
            lastISO = iso;
            lastAperture = aperture;
            lastShutterSpeed = shutterSpeed;
        }
    }

    /// <summary>
    /// 更新参数文本
    /// </summary>
    private void UpdateParameterTexts(float focalLength, float focusDistance, float iso, float aperture, float shutterSpeed)
    {
        if (focalLengthText != null)
        {
            focalLengthText.text = $"焦段: {focalLength:F0}mm";
        }

        if (focusDistanceText != null)
        {
            focusDistanceText.text = $"对焦: {focusDistance:F2}m";
            // 检查是否在有效对焦范围内
            if (focusDistance < 0.1f || focusDistance > 100f)
            {
                focusDistanceText.color = warningColor;
            }
        }

        if (isoText != null)
        {
            isoText.text = $"ISO: {Mathf.RoundToInt(iso)}";
            // 检查ISO是否过高
            if (iso > 6400f)
            {
                isoText.color = warningColor;
            }
        }

        if (apertureText != null)
        {
            apertureText.text = $"光圈: f/{aperture:F1}";
        }

        if (shutterSpeedText != null)
        {
            shutterSpeedText.text = $"快门: {FormatShutterSpeed(shutterSpeed)}";
            // 检查快门速度是否过慢
            if (shutterSpeed > 0.5f)
            {
                shutterSpeedText.color = warningColor;
            }
        }
    }

    /// <summary>
    /// 格式化快门速度显示
    /// </summary>
    private string FormatShutterSpeed(float shutterSpeed)
    {
        if (shutterSpeed >= 1f)
        {
            return $"{shutterSpeed:F1}s";
        }
        else
        {
            return $"1/{Mathf.RoundToInt(1f / shutterSpeed)}s";
        }
    }

    /// <summary>
    /// 设置显示距离
    /// </summary>
    public void SetDisplayDistance(float distance)
    {
        displayDistance = Mathf.Max(0.5f, distance);
    }

    /// <summary>
    /// 设置文字大小
    /// </summary>
    public void SetTextSize(float size)
    {
        textSize = Mathf.Max(12f, size);
        SetupTextStyles();
    }

    /// <summary>
    /// 切换显示跟随
    /// </summary>
    public void ToggleFollowCamera()
    {
        followCamera = !followCamera;
        Debug.Log("显示跟随相机: " + followCamera);
    }

    /// <summary>
    /// 显示/隐藏UI
    /// </summary>
    public void SetDisplayVisible(bool visible)
    {
        if (displayCanvas != null)
        {
            displayCanvas.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 播放参数变化动画
    /// </summary>
    public void PlayParameterChangeAnimation(TextMeshProUGUI targetText)
    {
        if (targetText != null)
        {
            StartCoroutine(ParameterChangeAnimation(targetText));
        }
    }

    /// <summary>
    /// 参数变化动画协程
    /// </summary>
    private IEnumerator ParameterChangeAnimation(TextMeshProUGUI text)
    {
        Color originalColor = text.color;
        Vector3 originalScale = text.transform.localScale;

        // 缩放动画
        text.transform.localScale = originalScale * 1.2f;

        yield return new WaitForSeconds(0.1f);

        text.transform.localScale = originalScale;
    }

    void OnGUI()
    {
        if (!isInitialized) return;

        // 显示调试信息（仅在编辑器中）
        #if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 250, 200));
        GUILayout.Label("VR参数显示器状态");
        GUILayout.Label("目标相机: " + (targetCamera != null ? "已找到" : "未找到"));
        GUILayout.Label("相机控制器: " + (cameraController != null ? "已找到" : "未找到"));
        GUILayout.Label("跟随相机: " + (followCamera ? "开启" : "关闭"));
        GUILayout.Label("显示距离: " + displayDistance);

        if (GUILayout.Button("切换跟随模式"))
        {
            ToggleFollowCamera();
        }

        GUILayout.EndArea();
        #endif
    }
}