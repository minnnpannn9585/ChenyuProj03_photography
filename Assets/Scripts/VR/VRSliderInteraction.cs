using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

/// <summary>
/// VR滑块交互组件
/// 为传统UI滑块添加VR手柄交互支持
/// </summary>
public class VRSliderInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("交互设置")]
    public bool enableVRInteraction = true;
    public float vrSensitivity = 2f;
    public float smoothTime = 0.1f;

    [Header("视觉反馈")]
    public bool enableHighlight = true;
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;
    public float highlightScale = 1.2f;
    public float animationDuration = 0.2f;

    [Header("手柄控制")]
    public bool allowHandControl = true;
    public KeyCode increaseKey = KeyCode.RightArrow;
    public KeyCode decreaseKey = KeyCode.LeftArrow;
    public float keyControlSpeed = 1f;

    // 组件引用
    private Slider targetSlider;
    private Image handleImage;
    private Image backgroundImage;
    private Image fillImage;

    // 状态管理
    private bool isHovered = false;
    private bool isPressed = false;
    private bool isControlledByHand = false;
    private Vector3 originalHandleScale;
    private Color originalHandleColor;
    private Color originalBackgroundColor;
    private Color originalFillColor;

    // 手柄控制
    private float currentControlValue = 0f;
    private Coroutine smoothControlCoroutine;

    void Start()
    {
        InitializeComponents();
        SetupVisualElements();
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        targetSlider = GetComponent<Slider>();
        if (targetSlider == null)
        {
            targetSlider = GetComponentInParent<Slider>();
        }

        if (targetSlider == null)
        {
            Debug.LogError("VRSliderInteraction: No Slider component found!");
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// 设置视觉元素
    /// </summary>
    private void SetupVisualElements()
    {
        // 获取滑块的视觉组件
        if (targetSlider.handleRect != null)
        {
            handleImage = targetSlider.handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                originalHandleColor = handleImage.color;
                originalHandleScale = handleImage.transform.localScale;
            }
        }

        if (targetSlider.fillRect != null)
        {
            fillImage = targetSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                originalFillColor = fillImage.color;
            }
        }

        Transform backgroundTransform = targetSlider.transform.Find("Background");
        if (backgroundTransform != null)
        {
            backgroundImage = backgroundTransform.GetComponent<Image>();
            if (backgroundImage != null)
            {
                originalBackgroundColor = backgroundImage.color;
            }
        }
    }

    /// <summary>
    /// 初始化VR滑块交互
    /// </summary>
    public void Initialize(Slider slider, float sensitivity = 2f)
    {
        targetSlider = slider;
        vrSensitivity = sensitivity;

        // 重新设置视觉元素
        SetupVisualElements();

        Debug.Log($"VRSliderInteraction initialized for {slider.name}");
    }

    void Update()
    {
        if (!enableVRInteraction || targetSlider == null) return;

        HandleKeyboardControl();
        HandleVRHandControl();
    }

    /// <summary>
    /// 处理键盘控制
    /// </summary>
    private void HandleKeyboardControl()
    {
        if (!allowHandControl) return;

        float delta = 0f;

        if (Input.GetKey(increaseKey))
        {
            delta = keyControlSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(decreaseKey))
        {
            delta = -keyControlSpeed * Time.deltaTime;
        }

        if (Mathf.Abs(delta) > 0f)
        {
            currentControlValue = Mathf.Clamp(currentControlValue + delta, -1f, 1f);
            ApplyControlValue();
        }
        else
        {
            // 逐渐回到中心
            currentControlValue = Mathf.MoveTowards(currentControlValue, 0f, Time.deltaTime * 2f);
        }
    }

    /// <summary>
    /// 处理VR手柄控制
    /// </summary>
    private void HandleVRHandControl()
    {
        if (!isControlledByHand) return;

        // 检测VR手柄输入
        float handInput = GetVRHandInput();

        if (Mathf.Abs(handInput) > 0.01f)
        {
            currentControlValue = Mathf.Clamp(currentControlValue + handInput * vrSensitivity * Time.deltaTime, -1f, 1f);
            ApplyControlValue();
        }
        else
        {
            // 逐渐回到中心
            currentControlValue = Mathf.MoveTowards(currentControlValue, 0f, Time.deltaTime * 2f);
        }
    }

    /// <summary>
    /// 获取VR手柄输入
    /// </summary>
    private float GetVRHandInput()
    {
        // 这里可以集成OVRInput或其他VR输入系统
        // 暂时使用键盘模拟
        float input = 0f;

        // 模拟手柄输入 (可以替换为实际的VR输入)
        if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Equals))
        {
            input = 1f;
        }
        else if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus))
        {
            input = -1f;
        }

        return input;
    }

    /// <summary>
    /// 应用控制值到滑块
    /// </summary>
    private void ApplyControlValue()
    {
        if (targetSlider == null) return;

        // 将控制值映射到滑块值
        float normalizedValue = (currentControlValue + 1f) * 0.5f;
        float newValue = Mathf.Lerp(targetSlider.minValue, targetSlider.maxValue, normalizedValue);

        targetSlider.value = newValue;

        // 添加视觉反馈
        if (enableHighlight)
        {
            UpdateVisualFeedback(true);
        }
    }

    /// <summary>
    /// 更新视觉反馈
    /// </summary>
    private void UpdateVisualFeedback(bool active)
    {
        if (!enableHighlight) return;

        if (active)
        {
            // 高亮效果
            if (handleImage != null)
            {
                handleImage.DOColor(highlightColor, animationDuration);
                handleImage.transform.DOScale(originalHandleScale * highlightScale, animationDuration);
            }

            if (fillImage != null)
            {
                fillImage.DOColor(Color.Lerp(originalFillColor, highlightColor, 0.5f), animationDuration);
            }
        }
        else
        {
            // 恢复正常状态
            if (handleImage != null)
            {
                handleImage.DOColor(originalHandleColor, animationDuration);
                handleImage.transform.DOScale(originalHandleScale, animationDuration);
            }

            if (fillImage != null)
            {
                fillImage.DOColor(originalFillColor, animationDuration);
            }
        }
    }

    #region Unity Event Handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if (enableHighlight)
        {
            UpdateVisualFeedback(true);
        }

        Debug.Log($"VRSlider: Enter {targetSlider.name}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (enableHighlight && !isPressed)
        {
            UpdateVisualFeedback(false);
        }

        Debug.Log($"VRSlider: Exit {targetSlider.name}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        isControlledByHand = true;

        // 开始平滑控制
        if (smoothControlCoroutine != null)
        {
            StopCoroutine(smoothControlCoroutine);
        }
        smoothControlCoroutine = StartCoroutine(SmoothControlCoroutine());

        Debug.Log($"VRSlider: Down {targetSlider.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        isControlledByHand = false;
        currentControlValue = 0f;

        // 停止平滑控制
        if (smoothControlCoroutine != null)
        {
            StopCoroutine(smoothControlCoroutine);
            smoothControlCoroutine = null;
        }

        if (enableHighlight && !isHovered)
        {
            UpdateVisualFeedback(false);
        }

        Debug.Log($"VRSlider: Up {targetSlider.name}");
    }

    #endregion

    /// <summary>
    /// 平滑控制协程
    /// </summary>
    private IEnumerator SmoothControlCoroutine()
    {
        while (isControlledByHand)
        {
            // 这里可以添加更复杂的平滑控制逻辑
            yield return null;
        }
    }

    /// <summary>
    /// 设置高亮颜色
    /// </summary>
    public void SetHighlightColor(Color color)
    {
        highlightColor = color;
    }

    /// <summary>
    /// 设置灵敏度
    /// </summary>
    public void SetSensitivity(float sensitivity)
    {
        vrSensitivity = Mathf.Max(0.1f, sensitivity);
    }

    /// <summary>
    /// 启用/禁用VR交互
    /// </summary>
    public void SetVRInteractionEnabled(bool enabled)
    {
        enableVRInteraction = enabled;

        if (!enabled)
        {
            isControlledByHand = false;
            currentControlValue = 0f;
        }
    }

    /// <summary>
    /// 获取当前滑块值
    /// </summary>
    public float GetValue()
    {
        return targetSlider != null ? targetSlider.value : 0f;
    }

    /// <summary>
    /// 设置滑块值
    /// </summary>
    public void SetValue(float value)
    {
        if (targetSlider != null)
        {
            targetSlider.value = Mathf.Clamp(value, targetSlider.minValue, targetSlider.maxValue);
        }
    }

    /// <summary>
    /// 获取是否被控制
    /// </summary>
    public bool IsBeingControlled()
    {
        return isControlledByHand;
    }

    void OnDestroy()
    {
        // 清理DOTween动画
        if (handleImage != null)
        {
            handleImage.DOKill();
        }

        if (fillImage != null)
        {
            fillImage.DOKill();
        }

        if (backgroundImage != null)
        {
            backgroundImage.DOKill();
        }

        // 停止协程
        if (smoothControlCoroutine != null)
        {
            StopCoroutine(smoothControlCoroutine);
        }
    }
}