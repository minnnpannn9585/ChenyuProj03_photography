using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// VR自定义曝光控制器
/// 集成CustomExposureController功能到VR系统
/// 与VRCameraController协同工作
/// </summary>
public class VRCustomExposureController : MonoBehaviour
{
    [Header("相机控制")]
    public VRCameraController vrCameraController;
    public Camera photographyCamera;

    [Header("后处理")]
    public Volume postVolume;
    public bool enableExposureCompensation = true;
    public bool enableDepthOfField = true;
    public bool enableMotionBlur = true;

    [Header("曝光补偿设置")]
    public float exposureScale = 0.5f;        // 曝光调整缩放
    public float exposureMinLimit = -3f;      // 曝光补偿下限
    public float exposureMaxLimit = 3f;       // 曝光补偿上限

    [Header("景深设置")]
    public DepthOfFieldMode dofMode = DepthOfFieldMode.Bokeh;
    public float minFocusDistance = 0.5f;      // 最小焦距
    public float dofApertureScale = 1f;       // 光圈到景深的缩放

    [Header("动态模糊设置")]
    public float minShutterSpeed = 0.001f;    // 最小快门速度
    public float maxShutterSpeed = 0.1f;      // 最大快门速度
    public AnimationCurve motionBlurCurve;    // 动态模糊曲线

    // 后处理组件引用
    private ColorAdjustments colorAdjustments;
    private DepthOfField depthOfField;
    private MotionBlur motionBlur;

    // 基准曝光值
    private float baseEV = 0f;
    private bool isInitialized = false;

    // 事件
    public System.Action<float> OnExposureChanged;
    public System.Action<float> OnDepthOfFieldChanged;
    public System.Action<float> OnMotionBlurChanged;

    void Start()
    {
        StartCoroutine(InitializeController());
    }

    /// <summary>
    /// 初始化控制器
    /// </summary>
    private IEnumerator InitializeController()
    {
        // 等待VRCameraController初始化
        yield return new WaitForSeconds(0.1f);

        // 获取相机引用
        if (photographyCamera == null && vrCameraController != null)
        {
            photographyCamera = vrCameraController.photographyCamera;
        }

        // 初始化后处理组件
        if (!InitializePostProcessing())
        {
            Debug.LogWarning("VRCustomExposureController: 后处理组件初始化失败");
            yield break;
        }

        // 计算基准曝光值
        CalculateBaseExposure();

        isInitialized = true;
        Debug.Log("VRCustomExposureController initialized successfully");
    }

    /// <summary>
    /// 初始化后处理组件
    /// </summary>
    private bool InitializePostProcessing()
    {
        if (postVolume == null)
        {
            Debug.LogWarning("VRCustomExposureController: Post Volume未设置");
            return false;
        }

        // 获取后处理组件
        bool hasColorAdjustments = postVolume.profile.TryGet(out colorAdjustments);
        bool hasDepthOfField = postVolume.profile.TryGet(out depthOfField);
        bool hasMotionBlur = postVolume.profile.TryGet(out motionBlur);

        // 检查必需组件
        if (!hasColorAdjustments)
        {
            Debug.LogWarning("VRCustomExposureController: 缺少ColorAdjustments组件");
        }

        if (!hasDepthOfField && enableDepthOfField)
        {
            Debug.LogWarning("VRCustomExposureController: 缺少DepthOfField组件");
        }

        if (!hasMotionBlur && enableMotionBlur)
        {
            Debug.LogWarning("VRCustomExposureController: 缺少MotionBlur组件");
        }

        // 设置景深模式
        if (depthOfField != null)
        {
            depthOfField.mode.value = dofMode;
        }

        return hasColorAdjustments;
    }

    /// <summary>
    /// 计算基准曝光值
    /// </summary>
    private void CalculateBaseExposure()
    {
        if (photographyCamera == null) return;

        // 使用标准曝光值作为基准
        baseEV = CalculateEV(8f, 0.005f, 3200f);
        Debug.Log($"VRCustomExposureController: Base EV = {baseEV:F2}");
    }

    void Update()
    {
        if (!isInitialized || photographyCamera == null) return;

        // 更新曝光补偿
        if (enableExposureCompensation)
        {
            UpdateExposureCompensation();
        }

        // 更新景深
        if (enableDepthOfField)
        {
            UpdateDepthOfField();
        }

        // 更新动态模糊
        if (enableMotionBlur)
        {
            UpdateMotionBlur();
        }
    }

    /// <summary>
    /// 更新曝光补偿
    /// </summary>
    private void UpdateExposureCompensation()
    {
        if (colorAdjustments == null) return;

        // 计算当前曝光值
        float currentEV = CalculateEV(
            photographyCamera.aperture,
            photographyCamera.shutterSpeed,
            photographyCamera.iso
        );

        // 计算曝光差值
        float evDifference = baseEV - currentEV;

        // 应用缩放和限制
        evDifference *= exposureScale;
        evDifference = Mathf.Clamp(evDifference, exposureMinLimit, exposureMaxLimit);

        // 设置后处理曝光
        colorAdjustments.postExposure.value = evDifference;

        // 触发事件
        OnExposureChanged?.Invoke(evDifference);
    }

    /// <summary>
    /// 更新景深效果
    /// </summary>
    private void UpdateDepthOfField()
    {
        if (depthOfField == null) return;

        // 设置光圈值
        depthOfField.aperture.value = photographyCamera.aperture * dofApertureScale;

        // 设置焦距
        depthOfField.focalLength.value = photographyCamera.focalLength;

        // 设置焦距距离
        float focusDistance = Mathf.Max(minFocusDistance, photographyCamera.focusDistance);
        depthOfField.focusDistance.value = focusDistance;

        // 触发事件
        OnDepthOfFieldChanged?.Invoke(depthOfField.aperture.value);
    }

    /// <summary>
    /// 更新动态模糊
    /// </summary>
    private void UpdateMotionBlur()
    {
        if (motionBlur == null) return;

        // 将快门速度映射到0-1强度范围
        float shutterSpeed = photographyCamera.shutterSpeed;
        float normalizedShutter = Mathf.InverseLerp(minShutterSpeed, maxShutterSpeed, shutterSpeed);

        // 应用曲线调整
        float blurIntensity = motionBlurCurve != null
            ? motionBlurCurve.Evaluate(normalizedShutter)
            : normalizedShutter;

        // 设置动态模糊强度
        motionBlur.intensity.value = blurIntensity;

        // 触发事件
        OnMotionBlurChanged?.Invoke(blurIntensity);
    }

    /// <summary>
    /// 计算曝光值(EV)
    /// </summary>
    public float CalculateEV(float aperture, float shutter, float iso)
    {
        float ev = Mathf.Log((aperture * aperture) / shutter * 100f / iso, 2f);
        return ev;
    }

    /// <summary>
    /// 设置VRCameraController引用
    /// </summary>
    public void SetVRCameraController(VRCameraController controller)
    {
        vrCameraController = controller;

        // 自动获取相机引用
        if (controller != null)
        {
            photographyCamera = controller.photographyCamera;
        }
    }

    /// <summary>
    /// 手动设置基准曝光
    /// </summary>
    public void SetBaseExposure(float ev)
    {
        baseEV = ev;
        Debug.Log($"VRCustomExposureController: Base EV manually set to {ev:F2}");
    }

    /// <summary>
    /// 从当前相机参数设置基准曝光
    /// </summary>
    public void SetBaseExposureFromCurrent()
    {
        if (photographyCamera == null) return;

        baseEV = CalculateEV(
            photographyCamera.aperture,
            photographyCamera.shutterSpeed,
            photographyCamera.iso
        );

        Debug.Log($"VRCustomExposureController: Base EV set from current parameters: {baseEV:F2}");
    }

    /// <summary>
    /// 启用/禁用曝光补偿
    /// </summary>
    public void SetExposureCompensationEnabled(bool enabled)
    {
        enableExposureCompensation = enabled;

        if (!enabled && colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = 0f;
        }
    }

    /// <summary>
    /// 启用/禁用景深
    /// </summary>
    public void SetDepthOfFieldEnabled(bool enabled)
    {
        enableDepthOfField = enabled;

        if (depthOfField != null)
        {
            depthOfField.active = enabled;
        }
    }

    /// <summary>
    /// 启用/禁用动态模糊
    /// </summary>
    public void SetMotionBlurEnabled(bool enabled)
    {
        enableMotionBlur = enabled;

        if (motionBlur != null)
        {
            motionBlur.active = enabled;
        }
    }

    /// <summary>
    /// 获取当前曝光值
    /// </summary>
    public float GetCurrentEV()
    {
        if (photographyCamera == null) return 0f;

        return CalculateEV(
            photographyCamera.aperture,
            photographyCamera.shutterSpeed,
            photographyCamera.iso
        );
    }

    /// <summary>
    /// 获取曝光补偿值
    /// </summary>
    public float GetExposureCompensation()
    {
        return colorAdjustments != null ? colorAdjustments.postExposure.value : 0f;
    }

    /// <summary>
    /// 获取景强度
    /// </summary>
    public float GetMotionBlurIntensity()
    {
        return motionBlur != null ? motionBlur.intensity.value : 0f;
    }

    /// <summary>
    /// 获取景深光圈值
    /// </summary>
    public float GetDepthOfFieldAperture()
    {
        return depthOfField != null ? depthOfField.aperture.value : 0f;
    }

    /// <summary>
    /// 重置所有设置到默认值
    /// </summary>
    public void ResetToDefaults()
    {
        // 重置曝光补偿
        exposureScale = 0.5f;
        exposureMinLimit = -3f;
        exposureMaxLimit = 3f;

        // 重置景深设置
        dofMode = DepthOfFieldMode.Bokeh;
        minFocusDistance = 0.5f;
        dofApertureScale = 1f;

        // 重置动态模糊设置
        minShutterSpeed = 0.001f;
        maxShutterSpeed = 0.1f;

        if (motionBlurCurve == null)
        {
            motionBlurCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        // 重新计算基准曝光
        CalculateBaseExposure();

        Debug.Log("VRCustomExposureController: Reset to default settings");
    }

    void OnDestroy()
    {
        // 清理事件
        OnExposureChanged = null;
        OnDepthOfFieldChanged = null;
        OnMotionBlurChanged = null;
    }

    void OnValidate()
    {
        // 确保参数在合理范围内
        exposureScale = Mathf.Clamp(exposureScale, 0f, 2f);
        exposureMinLimit = Mathf.Clamp(exposureMinLimit, -10f, 0f);
        exposureMaxLimit = Mathf.Clamp(exposureMaxLimit, 0f, 10f);

        minFocusDistance = Mathf.Max(0.1f, minFocusDistance);
        dofApertureScale = Mathf.Max(0.1f, dofApertureScale);

        minShutterSpeed = Mathf.Max(0.0001f, minShutterSpeed);
        maxShutterSpeed = Mathf.Max(minShutterSpeed, maxShutterSpeed);
    }
}