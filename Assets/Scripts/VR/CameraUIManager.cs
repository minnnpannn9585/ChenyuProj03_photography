using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 相机UI管理器
/// 管理拍照场景的UI显示和交互反馈
/// </summary>
public class CameraUIManager : MonoBehaviour
{
    [Header("UI组件")]
    public Canvas cameraCanvas;
    public Canvas settingsPanel;
    public Canvas infoPanel;

    [Header("参数显示")]
    public TMP_Text isoText;
    public TMP_Text apertureText;
    public TMP_Text shutterText;
    public TMP_Text focalLengthText;
    public TMP_Text focusDistanceText;

    [Header("模式指示器")]
    public Image modeIndicator;
    public TMP_Text modeText;
    public Image modeBackground;

    [Header("控制提示")]
    public GameObject controlHints;
    public TMP_Text grabHintText;
    public TMP_Text buttonHintsText;

    [Header("拍照反馈")]
    public Image captureFlash;
    public Image captureButton;
    public Image captureButtonBorder;
    public TMP_Text captureButtonText;

    [Header("设置面板")]
    public Slider[] settingSliders;
    public TMP_Text[] settingLabels;
    public Image[] sliderBackgrounds;

    [Header("动画设置")]
    public float uiFadeDuration = 0.3f;
    public float pulseDuration = 1f;
    public Color activeColor = Color.yellow;
    public Color inactiveColor = Color.white;
    public Color captureColor = Color.red;

    // 私有变量
    private VRCameraController cameraController;
    private bool isUIVisible = true;
    private Coroutine pulseCoroutine;

    void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        cameraController = FindObjectOfType<VRCameraController>();

        // 设置初始状态
        if (cameraCanvas != null)
        {
            cameraCanvas.gameObject.SetActive(true);
        }

        // 初始化模式指示器
        UpdateModeIndicator(VRCameraController.ControlMode.Normal);

        // 初始化控制提示
        UpdateControlHints(VRCameraController.ControlMode.Normal);

        // 隐藏闪光效果
        if (captureFlash != null)
        {
            Color flashColor = captureFlash.color;
            flashColor.a = 0f;
            captureFlash.color = flashColor;
        }
    }

    /// <summary>
    /// 设置事件监听
    /// </summary>
    private void SetupEventListeners()
    {
        if (cameraController != null)
        {
            cameraController.OnParameterChanged.AddListener(OnParameterChanged);
            cameraController.OnPhotoCaptured.AddListener(OnPhotoCaptured);
        }
    }

    /// <summary>
    /// 参数变化事件
    /// </summary>
    private void OnParameterChanged()
    {
        UpdateParameterDisplays();
    }

    /// <summary>
    /// 拍照完成事件
    /// </summary>
    private void OnPhotoCaptured()
    {
        PlayCaptureFeedback();
    }

    /// <summary>
    /// 更新模式指示器
    /// </summary>
    public void UpdateModeIndicator(VRCameraController.ControlMode mode)
    {
        if (modeText == null || modeBackground == null) return;

        string modeName = "";
        Color modeColor = inactiveColor;

        switch (mode)
        {
            case VRCameraController.ControlMode.Normal:
                modeName = "焦段控制";
                modeColor = Color.green;
                break;
            case VRCameraController.ControlMode.Focus:
                modeName = "对焦控制";
                modeColor = Color.blue;
                break;
            case VRCameraController.ControlMode.Aperture:
                modeName = "光圈控制";
                modeColor = Color.yellow;
                break;
            case VRCameraController.ControlMode.Shutter:
                modeName = "快门控制";
                modeColor = Color.red;
                break;
            case VRCameraController.ControlMode.ISO:
                modeName = "ISO控制";
                modeColor = Color.magenta;
                break;
        }

        // 更新模式文本
        modeText.text = modeName;

        // 更新模式背景颜色
        modeBackground.DOColor(modeColor * 0.8f, uiFadeDuration);

        // 更新模式边框（如果有）
        if (modeIndicator != null)
        {
            modeIndicator.DOColor(modeColor, uiFadeDuration);
        }

        // 添加脉冲效果
        StartPulseEffect(modeBackground, modeColor);
    }

    /// <summary>
    /// 更新控制提示
    /// </summary>
    public void UpdateControlHints(VRCameraController.ControlMode mode)
    {
        if (grabHintText == null || buttonHintsText == null) return;

        string grabHint = "";
        string buttonHint = "";

        switch (mode)
        {
            case VRCameraController.ControlMode.Normal:
                grabHint = "左右手柄Grab键: 调整焦段";
                buttonHint = "按住X: 对焦 | Y: 光圈 | A: 快门 | B: ISO";
                break;
            case VRCameraController.ControlMode.Focus:
                grabHint = "左右手柄Grab键: 调整对焦距离";
                buttonHint = "松开X键: 返回焦段控制";
                break;
            case VRCameraController.ControlMode.Aperture:
                grabHint = "左右手柄Grab键: 调整光圈";
                buttonHint = "松开Y键: 返回焦段控制";
                break;
            case VRCameraController.ControlMode.Shutter:
                grabHint = "左右手柄Grab键: 调整快门";
                buttonHint = "松开A键: 返回焦段控制";
                break;
            case VRCameraController.ControlMode.ISO:
                grabHint = "左右手柄Grab键: 调整ISO";
                buttonHint = "松开B键: 返回焦段控制";
                break;
        }

        // 更新提示文本
        grabHintText.text = grabHint;
        buttonHintsText.text = buttonHint;

        // 添加文本动画
        grabHintText.DOFade(1f, 0.2f);
        buttonHintsText.DOFade(1f, 0.2f);
    }

    /// <summary>
    /// 更新参数显示
    /// </summary>
    public void UpdateParameterDisplays()
    {
        if (cameraController == null) return;

        // 这里需要从VRCameraController获取当前参数值
        // 由于VRCameraController使用滑块，我们可以直接读取滑块值
        UpdateSliderValues();
    }

    /// <summary>
    /// 更新滑块值显示
    /// </summary>
    private void UpdateSliderValues()
    {
        // 这个方法将由VRCameraController直接调用
        // 保持UI与实际参数同步
    }

    /// <summary>
    /// 更新ISO显示
    /// </summary>
    public void UpdateISODisplay(int isoValue)
    {
        if (isoText != null)
        {
            isoText.text = $"ISO: {isoValue}";
            AnimateParameterChange(isoText);
        }
    }

    /// <summary>
    /// 更新光圈显示
    /// </summary>
    public void UpdateApertureDisplay(float apertureValue)
    {
        if (apertureText != null)
        {
            apertureText.text = $"f/{apertureValue:F1}";
            AnimateParameterChange(apertureText);
        }
    }

    /// <summary>
    /// 更新快门显示
    /// </summary>
    public void UpdateShutterDisplay(float shutterValue)
    {
        if (shutterText != null)
        {
            if (shutterValue >= 1f)
            {
                shutterText.text = $"{shutterValue:F1}s";
            }
            else
            {
                shutterText.text = $"1/{Mathf.RoundToInt(1f/shutterValue)}s";
            }
            AnimateParameterChange(shutterText);
        }
    }

    /// <summary>
    /// 更新焦距显示
    /// </summary>
    public void UpdateFocalLengthDisplay(float focalLengthValue)
    {
        if (focalLengthText != null)
        {
            focalLengthText.text = $"{focalLengthValue:F0}mm";
            AnimateParameterChange(focalLengthText);
        }
    }

    /// <summary>
    /// 更新对焦距离显示
    /// </summary>
    public void UpdateFocusDistanceDisplay(float focusDistanceValue)
    {
        if (focusDistanceText != null)
        {
            if (focusDistanceValue >= 1f)
            {
                focusDistanceText.text = $"{focusDistanceValue:F1}m";
            }
            else
            {
                focusDistanceText.text = $"{focusDistanceValue * 100:F0}cm";
            }
            AnimateParameterChange(focusDistanceText);
        }
    }

    /// <summary>
    /// 参数变化动画
    /// </summary>
    private void AnimateParameterChange(TMP_Text textComponent)
    {
        if (textComponent == null) return;

        // 停止之前的动画
        textComponent.DOKill();

        // 创建变化动画
        textComponent.transform.DOScale(1.2f, 0.1f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad);

        textComponent.DOColor(activeColor, 0.1f)
            .SetLoops(2, LoopType.Yoyo);
    }

    /// <summary>
    /// 播放拍照反馈
    /// </summary>
    public void PlayCaptureFeedback()
    {
        // 闪光效果
        if (captureFlash != null)
        {
            captureFlash.DOFade(1f, 0.05f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => {
                    Color flashColor = captureFlash.color;
                    flashColor.a = 0f;
                    captureFlash.color = flashColor;
                });
        }

        // 按钮反馈
        if (captureButton != null)
        {
            captureButton.transform.DOScale(0.9f, 0.1f)
                .SetLoops(2, LoopType.Yoyo);

            if (captureButtonBorder != null)
            {
                captureButtonBorder.DOColor(captureColor, 0.1f)
                    .SetLoops(2, LoopType.Yoyo);
            }
        }

        // 文本反馈
        if (captureButtonText != null)
        {
            captureButtonText.DOColor(Color.white, 0.1f)
                .SetLoops(2, LoopType.Yoyo);
        }
    }

    /// <summary>
    /// 开始脉冲效果
    /// </summary>
    private void StartPulseEffect(Image targetImage, Color pulseColor)
    {
        if (targetImage == null) return;

        // 停止之前的脉冲
        StopPulseEffect();

        // 开始新的脉冲
        pulseCoroutine = StartCoroutine(PulseCoroutine(targetImage, pulseColor));
    }

    /// <summary>
    /// 脉冲协程
    /// </summary>
    private IEnumerator PulseCoroutine(Image targetImage, Color pulseColor)
    {
        while (true)
        {
            // 脉冲到高亮
            targetImage.DOColor(pulseColor * 0.6f, pulseDuration * 0.5f)
                .SetEase(Ease.InOutSine);

            yield return new WaitForSeconds(pulseDuration * 0.5f);

            // 脉冲回正常
            targetImage.DOColor(pulseColor * 0.3f, pulseDuration * 0.5f)
                .SetEase(Ease.InOutSine);

            yield return new WaitForSeconds(pulseDuration * 0.5f);
        }
    }

    /// <summary>
    /// 停止脉冲效果
    /// </summary>
    private void StopPulseEffect()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    /// <summary>
    /// 切换UI可见性
    /// </summary>
    public void ToggleUIVisibility()
    {
        isUIVisible = !isUIVisible;

        if (cameraCanvas != null)
        {
            if (isUIVisible)
            {
                cameraCanvas.gameObject.SetActive(true);
                FadeInUI();
            }
            else
            {
                FadeOutUI();
                StartCoroutine(DisableUICoroutine());
            }
        }
    }

    /// <summary>
    /// 淡入UI
    /// </summary>
    private void FadeInUI()
    {
        // 淡入所有Canvas Group
        CanvasGroup[] canvasGroups = cameraCanvas.GetComponentsInChildren<CanvasGroup>();
        foreach (var group in canvasGroups)
        {
            group.alpha = 0f;
            group.DOFade(1f, uiFadeDuration);
        }

        // 淡入独立Image和Text
        Image[] images = cameraCanvas.GetComponentsInChildren<Image>();
        foreach (var image in images)
        {
            Color imageColor = image.color;
            imageColor.a = 0f;
            image.color = imageColor;
            image.DOFade(1f, uiFadeDuration);
        }

        TMP_Text[] texts = cameraCanvas.GetComponentsInChildren<TMP_Text>();
        foreach (var text in texts)
        {
            Color textColor = text.color;
            textColor.a = 0f;
            text.color = textColor;
            text.DOFade(1f, uiFadeDuration);
        }
    }

    /// <summary>
    /// 淡出UI
    /// </summary>
    private void FadeOutUI()
    {
        // 淡出所有Canvas Group
        CanvasGroup[] canvasGroups = cameraCanvas.GetComponentsInChildren<CanvasGroup>();
        foreach (var group in canvasGroups)
        {
            group.DOFade(0f, uiFadeDuration);
        }

        // 淡出独立Image和Text
        Image[] images = cameraCanvas.GetComponentsInChildren<Image>();
        foreach (var image in images)
        {
            image.DOFade(0f, uiFadeDuration);
        }

        TMP_Text[] texts = cameraCanvas.GetComponentsInChildren<TMP_Text>();
        foreach (var text in texts)
        {
            text.DOFade(0f, uiFadeDuration);
        }
    }

    /// <summary>
    /// 禁用UI协程
    /// </summary>
    private IEnumerator DisableUICoroutine()
    {
        yield return new WaitForSeconds(uiFadeDuration);

        if (cameraCanvas != null)
        {
            cameraCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 高亮滑块
    /// </summary>
    public void HighlightSlider(string parameterName, bool highlight)
    {
        Slider targetSlider = null;

        switch (parameterName)
        {
            case "ISO":
                targetSlider = FindSliderByParameter("ISO");
                break;
            case "Aperture":
                targetSlider = FindSliderByParameter("Aperture");
                break;
            case "Shutter":
                targetSlider = FindSliderByParameter("Shutter");
                break;
            case "FocalLength":
                targetSlider = FindSliderByParameter("FocalLength");
                break;
            case "FocusDistance":
                targetSlider = FindSliderByParameter("FocusDistance");
                break;
        }

        if (targetSlider != null)
        {
            Image fillImage = targetSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                Color targetColor = highlight ? activeColor : inactiveColor;
                fillImage.DOColor(targetColor, 0.2f);
            }
        }
    }

    /// <summary>
    /// 根据参数名查找滑块
    /// </summary>
    private Slider FindSliderByParameter(string parameterName)
    {
        if (settingSliders == null) return null;

        foreach (var slider in settingSliders)
        {
            if (slider != null && slider.name.Contains(parameterName))
            {
                return slider;
            }
        }

        return null;
    }

    /// <summary>
    /// 显示设置面板
    /// </summary>
    public void ShowSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.gameObject.SetActive(true);
            settingsPanel.GetComponent<CanvasGroup>()?.DOFade(1f, uiFadeDuration);
        }
    }

    /// <summary>
    /// 隐藏设置面板
    /// </summary>
    public void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            var canvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, uiFadeDuration)
                    .OnComplete(() => {
                        settingsPanel.gameObject.SetActive(false);
                    });
            }
            else
            {
                settingsPanel.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 显示信息面板
    /// </summary>
    public void ShowInfoPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.gameObject.SetActive(true);
            infoPanel.GetComponent<CanvasGroup>()?.DOFade(1f, uiFadeDuration);
        }
    }

    /// <summary>
    /// 隐藏信息面板
    /// </summary>
    public void HideInfoPanel()
    {
        if (infoPanel != null)
        {
            var canvasGroup = infoPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, uiFadeDuration)
                    .OnComplete(() => {
                        infoPanel.gameObject.SetActive(false);
                    });
            }
            else
            {
                infoPanel.gameObject.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (cameraController != null)
        {
            cameraController.OnParameterChanged.RemoveListener(OnParameterChanged);
            cameraController.OnPhotoCaptured.RemoveListener(OnPhotoCaptured);
        }

        // 停止所有动画
        StopPulseEffect();

        // 清理DOTween
        transform.DOKill();
    }
}