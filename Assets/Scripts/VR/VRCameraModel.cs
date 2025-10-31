using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// VR相机模型控制器
/// 管理相机3D模型的物理交互和动画
/// </summary>
public class VRCameraModel : MonoBehaviour
{
    [Header("相机组件")]
    public VRCameraController cameraController;
    public GameObject cameraModel;
    public Transform lensTransform;
    public Transform focusRingTransform;
    public Transform apertureRingTransform;
    public Transform shutterButtonTransform;
    public Transform modeDialTransform;
    public Light cameraLight;  // 辅助照明灯

    [Header("物理设置")]
    public float weight = 0.8f; // 相机重量（影响手感）
    public float holdDistance = 0.3f; // 手持距离
    public Vector3 holdRotation = new Vector3(0, 180, 0); // 手持旋转

    [Header("交互动画")]
    public float focusRotationSpeed = 50f; // 对焦环旋转速度
    public float apertureRotationSpeed = 30f; // 光圈环旋转速度
    public float shutterPressDistance = 0.02f; // 快门按下的距离
    public float modeDialClickAngle = 30f; // 模式转盘点击角度

    [Header("视觉效果")]
    public Material lensMaterial; // 镜头材质
    public Material bodyMaterial; // 机身材质
    public float lensReflectionIntensity = 0.8f; // 镜头反射强度

    [Header("音效")]
    public AudioClip focusSound; // 对焦音效
    public AudioClip apertureSound; // 光圈音效
    public AudioClip shutterSound; // 快门音效
    public AudioClip modeDialSound; // 模式转盘音效
    public AudioSource audioSource;

    [Header("Haptic反馈")]
    public float focusHapticIntensity = 0.3f; // 对焦震动强度
    public float shutterHapticIntensity = 0.8f; // 快门震动强度
    public float apertureHapticIntensity = 0.4f; // 光圈震动强度

    // 私有变量
    private Transform holdingHand;
    private bool isHolding = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Coroutine focusAnimationCoroutine;
    private Coroutine apertureAnimationCoroutine;

    // 动画状态
    private float currentFocusRotation = 0f;
    private float currentApertureRotation = 0f;
    private bool isShutterPressed = false;
    private int currentModeIndex = 0;

    // 材质属性
    private Color originalBodyColor;
    private float originalLensReflection;

    void Start()
    {
        InitializeCameraModel();
        SetupMaterials();
        InitializeAudio();
    }

    void Update()
    {
        if (isHolding && holdingHand != null)
    {
        UpdateHoldingPosition();
    }

        UpdateVisualEffects();
    }

    /// <summary>
    /// 初始化相机模型
    /// </summary>
    private void InitializeCameraModel()
    {
        // 保存原始位置和旋转
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // 查找相机控制器
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<VRCameraController>();
        }

        // 自动查找组件
        if (lensTransform == null)
        {
            lensTransform = transform.Find("Lens");
        }

        if (focusRingTransform == null)
        {
            focusRingTransform = transform.Find("FocusRing");
        }

        if (apertureRingTransform == null)
        {
            apertureRingTransform = transform.Find("ApertureRing");
        }

        if (shutterButtonTransform == null)
        {
            shutterButtonTransform = transform.Find("ShutterButton");
        }

        if (modeDialTransform == null)
        {
            modeDialTransform = transform.Find("ModeDial");
        }

        Debug.Log("VRCameraModel initialized");
    }

    /// <summary>
    /// 设置材质
    /// </summary>
    private void SetupMaterials()
    {
        // 设置镜头材质
        if (lensMaterial != null)
        {
            originalLensReflection = lensMaterial.GetFloat("_Metallic");
            lensMaterial.SetFloat("_Metallic", lensReflectionIntensity);
        }

        // 设置机身材质
        if (bodyMaterial != null)
        {
            originalBodyColor = bodyMaterial.GetColor("_BaseColor");
        }

        // 自动查找材质
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.name.Contains("Lens"))
            {
                lensMaterial = renderer.material;
            }
            else if (renderer.name.Contains("Body"))
            {
                bodyMaterial = renderer.material;
            }
        }
    }

    /// <summary>
    /// 初始化音效
    /// </summary>
    private void InitializeAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 被手抓住
    /// </summary>
    public void OnGrabbed(Transform handTransform)
    {
        if (isHolding) return;

        isHolding = true;
        holdingHand = handTransform;

        // 设置相机为手的子对象
        transform.SetParent(handTransform);

        // 调整位置和旋转
        transform.localPosition = Vector3.forward * holdDistance;
        transform.localRotation = Quaternion.Euler(holdRotation);

        // 停止所有动画
        StopAllAnimations();

        // 播放抓取音效
        PlayHaptic(handTransform, 0.2f);

        Debug.Log("Camera grabbed by hand");
    }

    /// <summary>
    /// 被松开
    /// </summary>
    public void OnReleased()
    {
        if (!isHolding) return;

        isHolding = false;
        holdingHand = null;

        // 恢复父级
        transform.SetParent(null);

        // 平滑返回原位
        ReturnToOriginalPosition();

        Debug.Log("Camera released");
    }

    /// <summary>
    /// 更新手持位置
    /// </summary>
    private void UpdateHoldingPosition()
    {
        if (holdingHand == null) return;

        // 根据手的姿势微调相机位置
        Vector3 targetPosition = Vector3.forward * holdDistance;
        Quaternion targetRotation = Quaternion.Euler(holdRotation);

        // 平滑移动到目标位置
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 10f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * 10f);
    }

    /// <summary>
    /// 返回原始位置
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        // 使用DOTween创建平滑的返回动画
        transform.DOMove(originalPosition, 0.5f)
            .SetEase(Ease.InOutCubic);

        transform.DORotate(originalRotation.eulerAngles, 0.5f)
            .SetEase(Ease.InOutCubic);

        // 轻微的弹跳效果
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 2, 0.5f);
    }

    /// <summary>
    /// 对焦环旋转
    /// </summary>
    public void RotateFocusRing(float amount)
    {
        if (focusRingTransform == null) return;

        currentFocusRotation += amount * focusRotationSpeed * Time.deltaTime;
        currentFocusRotation = Mathf.Repeat(currentFocusRotation, 360f);

        focusRingTransform.localRotation = Quaternion.Euler(0, currentFocusRotation, 0);

        // 播放对焦音效
        if (focusSound != null && Mathf.Abs(amount) > 0.1f)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(focusSound, 0.3f);
            }
        }

        // 触觉反馈
        if (holdingHand != null && Mathf.Abs(amount) > 0.1f)
        {
            PlayHaptic(holdingHand, focusHapticIntensity * Mathf.Abs(amount));
        }
    }

    /// <summary>
    /// 光圈环旋转
    /// </summary>
    public void RotateApertureRing(float amount)
    {
        if (apertureRingTransform == null) return;

        currentApertureRotation += amount * apertureRotationSpeed * Time.deltaTime;
        currentApertureRotation = Mathf.Repeat(currentApertureRotation, 360f);

        apertureRingTransform.localRotation = Quaternion.Euler(0, -currentApertureRotation, 0);

        // 播放光圈音效
        if (apertureSound != null && Mathf.Abs(amount) > 0.1f)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(apertureSound, 0.3f);
            }
        }

        // 触觉反馈
        if (holdingHand != null && Mathf.Abs(amount) > 0.1f)
        {
            PlayHaptic(holdingHand, apertureHapticIntensity * Mathf.Abs(amount));
        }
    }

    /// <summary>
    /// 按下快门
    /// </summary>
    public void PressShutter()
    {
        if (isShutterPressed || shutterButtonTransform == null) return;

        isShutterPressed = true;

        // 快门按下动画
        shutterButtonTransform.DOLocalMoveZ(shutterButtonTransform.localPosition.z - shutterPressDistance, 0.05f);

        // 播放快门音效
        if (shutterSound != null)
        {
            audioSource.PlayOneShot(shutterSound);
        }

        // 强烈触觉反馈
        if (holdingHand != null)
        {
            PlayHaptic(holdingHand, shutterHapticIntensity);
        }

        // 闪光效果
        StartCoroutine(FlashEffect());

        // 快门释放
        StartCoroutine(ReleaseShutter());
    }

    /// <summary>
    /// 释放快门
    /// </summary>
    private IEnumerator ReleaseShutter()
    {
        yield return new WaitForSeconds(0.1f);

        if (shutterButtonTransform != null)
        {
            shutterButtonTransform.DOLocalMoveZ(shutterButtonTransform.localPosition.z + shutterPressDistance, 0.1f);
        }

        isShutterPressed = false;
    }

    /// <summary>
    /// 切换模式
    /// </summary>
    public void SwitchMode()
    {
        if (modeDialTransform == null) return;

        currentModeIndex = (currentModeIndex + 1) % 5;

        // 模式转盘旋转
        float targetRotation = currentModeIndex * modeDialClickAngle;
        modeDialTransform.DOLocalRotate(Vector3.forward * targetRotation, 0.2f);

        // 播放模式转盘音效
        if (modeDialSound != null)
        {
            audioSource.PlayOneShot(modeDialSound, 0.5f);
        }

        // 轻微触觉反馈
        if (holdingHand != null)
        {
            PlayHaptic(holdingHand, 0.3f);
        }

        Debug.Log($"Camera mode switched to: {currentModeIndex}");
    }

    /// <summary>
    /// 闪光效果
    /// </summary>
    private IEnumerator FlashEffect()
    {
        if (cameraLight != null)
        {
            cameraLight.enabled = true;
            cameraLight.intensity = 2f;

            yield return new WaitForSeconds(0.05f);

            cameraLight.intensity = 0f;
            cameraLight.enabled = false;
        }

        // 镜头闪光材质效果
        if (lensMaterial != null)
        {
            lensMaterial.SetFloat("_Metallic", 1f);
            lensMaterial.SetColor("_EmissionColor", Color.white * 2f);

            yield return new WaitForSeconds(0.1f);

            lensMaterial.SetFloat("_Metallic", lensReflectionIntensity);
            lensMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    /// <summary>
    /// 更新视觉效果
    /// </summary>
    private void UpdateVisualEffects()
    {
        // 镜头反射效果
        if (lensMaterial != null && cameraController != null)
        {
            // 根据当前参数调整镜头反射
            float focusDistance = cameraController.photographyCamera.focusDistance;
            float reflectionValue = Mathf.Lerp(0.5f, 1f, 1f / focusDistance);
            lensMaterial.SetFloat("_Metallic", reflectionValue);
        }

        // 机身材质动态效果
        if (bodyMaterial != null)
        {
            // 根据相机状态调整机身颜色
            Color targetColor = originalBodyColor;

            if (isHolding)
            {
                targetColor = Color.Lerp(originalBodyColor, Color.gray, 0.1f);
            }

            if (isShutterPressed)
            {
                targetColor = Color.Lerp(originalBodyColor, Color.red, 0.2f);
            }

            bodyMaterial.SetColor("_BaseColor", Color.Lerp(bodyMaterial.GetColor("_BaseColor"), targetColor, Time.deltaTime * 5f));
        }
    }

    /// <summary>
    /// 播放触觉反馈
    /// </summary>
    private void PlayHaptic(Transform controller, float intensity)
    {
        if (controller == null) return;

        // 根据手柄类型播放不同的触觉反馈
        if (controller.name.Contains("Right"))
        {
            OVRInput.SetControllerVibration(intensity, intensity, OVRInput.Controller.RTouch);
        }
        else if (controller.name.Contains("Left"))
        {
            OVRInput.SetControllerVibration(intensity, intensity, OVRInput.Controller.LTouch);
        }
    }

    /// <summary>
    /// 停止所有动画
    /// </summary>
    private void StopAllAnimations()
    {
        // 停止DOTween动画
        transform.DOKill();

        if (focusRingTransform != null)
        {
            focusRingTransform.DOKill();
        }

        if (apertureRingTransform != null)
        {
            apertureRingTransform.DOKill();
        }

        if (shutterButtonTransform != null)
        {
            shutterButtonTransform.DOKill();
        }

        if (modeDialTransform != null)
        {
            modeDialTransform.DOKill();
        }

        // 停止协程
        if (focusAnimationCoroutine != null)
        {
            StopCoroutine(focusAnimationCoroutine);
        }

        if (apertureAnimationCoroutine != null)
        {
            StopCoroutine(apertureAnimationCoroutine);
        }
    }

    /// <summary>
    /// 获取当前持有状态
    /// </summary>
    public bool IsHolding()
    {
        return isHolding;
    }

    /// <summary>
    /// 获取当前持有手
    /// </summary>
    public Transform GetHoldingHand()
    {
        return holdingHand;
    }

    /// <summary>
    /// 设置相机重量（影响手感）
    /// </summary>
    public void SetWeight(float newWeight)
    {
        weight = Mathf.Clamp(newWeight, 0.1f, 2f);
        // 可以根据重量调整动画速度和触觉反馈强度
    }

    /// <summary>
    /// 设置手持距离
    /// </summary>
    public void SetHoldDistance(float distance)
    {
        holdDistance = Mathf.Clamp(distance, 0.1f, 1f);
    }

    /// <summary>
    /// 重置相机状态
    /// </summary>
    public void ResetCamera()
    {
        StopAllAnimations();

        // 重置位置和旋转
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = Vector3.one;

        // 重置动画状态
        currentFocusRotation = 0f;
        currentApertureRotation = 0f;
        isShutterPressed = false;
        currentModeIndex = 0;

        // 重置组件旋转
        if (focusRingTransform != null)
        {
            focusRingTransform.localRotation = Quaternion.identity;
        }

        if (apertureRingTransform != null)
        {
            apertureRingTransform.localRotation = Quaternion.identity;
        }

        if (shutterButtonTransform != null)
        {
            shutterButtonTransform.localPosition = Vector3.zero;
        }

        if (modeDialTransform != null)
        {
            modeDialTransform.localRotation = Quaternion.identity;
        }

        // 重置材质
        if (bodyMaterial != null)
        {
            bodyMaterial.SetColor("_BaseColor", originalBodyColor);
        }

        if (lensMaterial != null)
        {
            lensMaterial.SetFloat("_Metallic", originalLensReflection);
        }

        Debug.Log("Camera model reset to original state");
    }

    void OnDestroy()
    {
        // 清理DOTween动画
        transform.DOKill();

        // 停止触觉反馈
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);

        // 停止所有协程
        StopAllCoroutines();
    }
}