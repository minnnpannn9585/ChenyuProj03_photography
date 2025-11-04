using System;
using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class VRCameraController : MonoBehaviour
{
    [Header("相机设置")]
    public Camera photographyCamera;
    public RawImage previewUI;

    [Header("保存设置")]
    public int captureWidth = 1920;
    public int captureHeight = 1080;
    public string folderName = "CapturedPhotos";

    [Header("VR输入设置")]
    public InputActionAsset vrInputActions;

    [Header("参数调整范围")]
    [Range(1f, 200f)] public float focalLengthMin = 1f;
    [Range(1f, 200f)] public float focalLengthMax = 200f;

    [Range(0.1f, 100f)] public float focusDistanceMin = 0.1f;
    [Range(0.1f, 100f)] public float focusDistanceMax = 100f;

    [Range(50f, 25600f)] public float isoMin = 50f;
    [Range(50f, 25600f)] public float isoMax = 25600f;

    [Range(1f, 64f)] public float apertureMin = 1f;
    [Range(1f, 64f)] public float apertureMax = 64f;

    [Range(0.0001f, 30f)] public float shutterMin = 0.0001f;
    [Range(0.0001f, 30f)] public float shutterMax = 30f;

    [Header("调整速度")]
    public float focalLengthSpeed = 10f; // mm/秒
    public float focusDistanceSpeed = 5f; // m/秒
    public float isoSpeed = 100f; // ISO/秒
    public float apertureSpeed = 1f; // f值/秒
    public float shutterAdjustSpeed = 0.1f; // 秒/秒

    [Header("后处理")]
    public Volume postVolume;

    [Header("UI Display")]
    public Slider focalLengthSlider;
    public Slider focusDistanceSlider;
    public Slider isoSlider;
    public Slider apertureSlider;
    public Slider shutterSpeedSlider;

    public TextMeshProUGUI focalLengthText;
    public TextMeshProUGUI focusDistanceText;
    public TextMeshProUGUI isoText;
    public TextMeshProUGUI apertureText;
    public TextMeshProUGUI shutterSpeedText;

    private RenderTexture previewRT;
    private string saveDirectory;

    // 输入动作
    private InputAction rightTriggerAction;
    private InputAction leftGripAction;
    private InputAction rightGripAction;
    private InputAction aButtonAction;
    private InputAction bButtonAction;
    private InputAction xButtonAction;
    private InputAction yButtonAction;

    // 后处理组件
    private ColorAdjustments colorAdj;
    private DepthOfField dof;
    private MotionBlur motionBlur;
    private float baseEV;

    // 状态变量
    private bool adjustFocalLength = false;
    private bool adjustFocusDistance = false;
    private bool adjustISO = false;
    private bool adjustAperture = false;
    private bool adjustShutter = false;

    // 当前参数值
    private float currentFocalLength;
    private float currentFocusDistance;
    private int currentISO;
    private float currentAperture;
    private float currentShutter;

    void Start()
    {
        InitializeCamera();
        SetupInputActions();
        InitializePostProcessing();
    }

    void OnDestroy()
    {
        CleanupInputActions();
    }

    void InitializeCamera()
    {
        // 创建保存目录
        saveDirectory = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(saveDirectory))
            Directory.CreateDirectory(saveDirectory);

        // 创建预览RenderTexture
        previewRT = new RenderTexture(captureWidth, captureHeight, 16, RenderTextureFormat.ARGB32);
        photographyCamera.targetTexture = previewRT;
        previewUI.texture = previewRT;

        // 启用物理相机属性
        photographyCamera.usePhysicalProperties = true;

        // 初始化参数为相机当前值
        currentFocalLength = photographyCamera.focalLength;
        currentFocusDistance = photographyCamera.focusDistance;
        currentISO = Mathf.RoundToInt(photographyCamera.iso);
        currentAperture = photographyCamera.aperture;
        currentShutter = photographyCamera.shutterSpeed;
    }

    void InitializePostProcessing()
    {
        if (postVolume != null)
        {
            if (postVolume.profile.TryGet(out colorAdj) &&
                postVolume.profile.TryGet(out dof) &&
                postVolume.profile.TryGet(out motionBlur))
            {
                baseEV = CalculateEV(8f, 0.005f, 3200f);
                dof.mode.value = DepthOfFieldMode.Bokeh;
            }
            else
            {
                Debug.LogWarning("VRCameraController: 需要在Volume中添加ColorAdjustments、DepthOfField和MotionBlur覆盖。");
            }
        }
    }

    void SetupInputActions()
    {
        if (vrInputActions == null)
        {
            Debug.LogError("VRInputActions资源未分配！");
            return;
        }

        vrInputActions.Enable();
        InputActionMap questActionMap = vrInputActions.FindActionMap("Quest");
        if (questActionMap == null)
        {
            Debug.LogError("未找到Quest Action Map！");
            return;
        }

        // 右手扳机（拍照）
        rightTriggerAction = questActionMap.FindAction("RightTrigger");
        if (rightTriggerAction != null)
        {
            rightTriggerAction.performed += OnRightTriggerPressed;
            rightTriggerAction.Enable();
        }

        // 左手握把
        leftGripAction = questActionMap.FindAction("LeftGrab");
        if (leftGripAction != null)
        {
            leftGripAction.started += OnGripPressed;
            leftGripAction.canceled += OnGripReleased;
            leftGripAction.Enable();
        }

        // 右手握把
        rightGripAction = questActionMap.FindAction("RightGrab");
        if (rightGripAction != null)
        {
            rightGripAction.started += OnGripPressed;
            rightGripAction.canceled += OnGripReleased;
            rightGripAction.Enable();
        }

        // A键（对焦距离模式）
        aButtonAction = questActionMap.FindAction("A");
        if (aButtonAction != null)
        {
            aButtonAction.started += OnAButtonPressed;
            aButtonAction.canceled += OnAButtonReleased;
            aButtonAction.Enable();
        }

        // B键（ISO模式）
        bButtonAction = questActionMap.FindAction("B");
        if (bButtonAction != null)
        {
            bButtonAction.started += OnBButtonPressed;
            bButtonAction.canceled += OnBButtonReleased;
            bButtonAction.Enable();
        }

        // X键（光圈模式）
        xButtonAction = questActionMap.FindAction("X");
        if (xButtonAction != null)
        {
            xButtonAction.started += OnXButtonPressed;
            xButtonAction.canceled += OnXButtonReleased;
            xButtonAction.Enable();
        }

        // Y键（快门速度模式）
        yButtonAction = questActionMap.FindAction("Y");
        if (yButtonAction != null)
        {
            yButtonAction.started += OnYButtonPressed;
            yButtonAction.canceled += OnYButtonReleased;
            yButtonAction.Enable();
        }
    }

    void CleanupInputActions()
    {
        if (rightTriggerAction != null)
        {
            rightTriggerAction.performed -= OnRightTriggerPressed;
            rightTriggerAction.Disable();
        }

        if (leftGripAction != null)
        {
            leftGripAction.started -= OnGripPressed;
            leftGripAction.canceled -= OnGripReleased;
            leftGripAction.Disable();
        }

        if (rightGripAction != null)
        {
            rightGripAction.started -= OnGripPressed;
            rightGripAction.canceled -= OnGripReleased;
            rightGripAction.Disable();
        }

        if (aButtonAction != null)
        {
            aButtonAction.started -= OnAButtonPressed;
            aButtonAction.canceled -= OnAButtonReleased;
            aButtonAction.Disable();
        }

        if (bButtonAction != null)
        {
            bButtonAction.started -= OnBButtonPressed;
            bButtonAction.canceled -= OnBButtonReleased;
            bButtonAction.Disable();
        }

        if (xButtonAction != null)
        {
            xButtonAction.started -= OnXButtonPressed;
            xButtonAction.canceled -= OnXButtonReleased;
            xButtonAction.Disable();
        }

        if (yButtonAction != null)
        {
            yButtonAction.started -= OnYButtonPressed;
            yButtonAction.canceled -= OnYButtonReleased;
            yButtonAction.Disable();
        }

        if (vrInputActions != null)
        {
            vrInputActions.Disable();
        }
    }

    // 输入事件处理
    private void OnRightTriggerPressed(InputAction.CallbackContext context)
    {
        CapturePhoto();
    }

    private void OnGripPressed(InputAction.CallbackContext context)
    {
        if (!adjustFocusDistance && !adjustISO && !adjustAperture && !adjustShutter)
        {
            adjustFocalLength = true;
            Debug.Log("开始调整焦段");
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        adjustFocalLength = false;
        Debug.Log("停止调整焦段");
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        adjustFocusDistance = true;
        Debug.Log("进入对焦距离调整模式");
    }

    private void OnAButtonReleased(InputAction.CallbackContext context)
    {
        adjustFocusDistance = false;
        Debug.Log("退出对焦距离调整模式");
    }

    private void OnBButtonPressed(InputAction.CallbackContext context)
    {
        adjustISO = true;
        Debug.Log("进入ISO调整模式");
    }

    private void OnBButtonReleased(InputAction.CallbackContext context)
    {
        adjustISO = false;
        Debug.Log("退出ISO调整模式");
    }

    private void OnXButtonPressed(InputAction.CallbackContext context)
    {
        adjustAperture = true;
        Debug.Log("进入光圈调整模式");
    }

    private void OnXButtonReleased(InputAction.CallbackContext context)
    {
        adjustAperture = false;
        Debug.Log("退出光圈调整模式");
    }

    private void OnYButtonPressed(InputAction.CallbackContext context)
    {
        adjustShutter = true;
        Debug.Log("进入快门速度调整模式");
    }

    private void OnYButtonReleased(InputAction.CallbackContext context)
    {
        adjustShutter = false;
        Debug.Log("退出快门速度调整模式");
    }

    void Update()
    {
        HandleParameterAdjustment();
        UpdatePostProcessing();
        UpdateParameterDisplay(); // 实时更新UI显示
    }

    void HandleParameterAdjustment()
    {
        // 获取握把输入值
        float leftGripValue = leftGripAction?.ReadValue<float>() ?? 0f;
        float rightGripValue = rightGripAction?.ReadValue<float>() ?? 0f;
        float gripInput = (leftGripValue + rightGripValue) / 2f;

        // 握把输入范围是0-1，我们将其映射到-1到1以便双向调整
        float adjustment = (gripInput - 0.5f) * 2f;

        if (Mathf.Abs(adjustment) > 0.1f) // 死区
        {
            if (adjustFocalLength)
            {
                // 调整焦段
                currentFocalLength += adjustment * focalLengthSpeed * Time.deltaTime;
                currentFocalLength = Mathf.Clamp(currentFocalLength, focalLengthMin, focalLengthMax);
                photographyCamera.focalLength = currentFocalLength;
            }
            else if (adjustFocusDistance)
            {
                // 调整对焦距离
                currentFocusDistance += adjustment * focusDistanceSpeed * Time.deltaTime;
                currentFocusDistance = Mathf.Clamp(currentFocusDistance, focusDistanceMin, focusDistanceMax);
                photographyCamera.focusDistance = currentFocusDistance;
            }
            else if (adjustISO)
            {
                // 调整ISO（对数调整）
                float isoLog = Mathf.Log10(currentISO);
                isoLog += adjustment * Time.deltaTime * 0.1f;
                currentISO = Mathf.RoundToInt(Mathf.Pow(10, isoLog));
                currentISO = Mathf.Clamp(currentISO, Mathf.RoundToInt(isoMin), Mathf.RoundToInt(isoMax));
                photographyCamera.iso = currentISO;
            }
            else if (adjustAperture)
            {
                // 调整光圈
                currentAperture += adjustment * apertureSpeed * Time.deltaTime;
                currentAperture = Mathf.Clamp(currentAperture, apertureMin, apertureMax);
                photographyCamera.aperture = currentAperture;
            }
            else if (adjustShutter)
            {
                // 调整快门速度（对数调整）
                float shutterLog = Mathf.Log10(currentShutter);
                shutterLog += adjustment * Time.deltaTime * shutterAdjustSpeed * 0.1f;
                currentShutter = Mathf.Pow(10, shutterLog);
                currentShutter = Mathf.Clamp(currentShutter, shutterMin, shutterMax);
                photographyCamera.shutterSpeed = currentShutter;
            }
        }
    }

    void UpdatePostProcessing()
    {
        if (colorAdj == null || dof == null || motionBlur == null) return;

        // 计算当前EV并调整曝光
        float currentEV = CalculateEV(currentAperture, currentShutter, (float)currentISO);
        float evDifference = baseEV - currentEV;
        evDifference *= 0.5f; // 缩放调整
        evDifference = Mathf.Clamp(evDifference, -3f, 3f); // 限制范围
        colorAdj.postExposure.value = evDifference;

        // 更新景深
        dof.aperture.value = currentAperture;
        dof.focalLength.value = currentFocalLength;
        dof.focusDistance.value = Mathf.Max(0.5f, currentFocusDistance);

        // 更新动态模糊
        motionBlur.intensity.value = Mathf.InverseLerp(0.001f, 0.1f, currentShutter);
    }

    float CalculateEV(float aperture, float shutter, float iso)
    {
        return Mathf.Log((aperture * aperture) / shutter * 100f / iso, 2f);
    }

    void CapturePhoto()
    {
        string imageName = "photo_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        StartCoroutine(CaptureAndSave(imageName));
    }

    IEnumerator CaptureAndSave(string imageName)
    {
        Debug.Log("正在拍照...");

        // 使用临时RenderTexture捕捉画面
        RenderTexture tempRT = new RenderTexture(captureWidth, captureHeight, 16, RenderTextureFormat.ARGB32);
        photographyCamera.targetTexture = tempRT;
        photographyCamera.Render();

        RenderTexture.active = tempRT;
        Texture2D image = new Texture2D(captureWidth, captureHeight, TextureFormat.ARGB32, false);
        image.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        image.Apply();

        // 恢复实时预览
        photographyCamera.targetTexture = previewRT;
        RenderTexture.active = null;
        tempRT.Release();

        // 保存图片
        byte[] bytes = image.EncodeToJPG();
        if (bytes != null)
        {
            string savePath = Path.Combine(saveDirectory, imageName + ".jpg");
            File.WriteAllBytes(savePath, bytes);
            Debug.Log("照片已保存到: " + savePath);
        }
        else
        {
            Debug.LogError("图片编码失败！");
        }

        // 释放临时贴图
        Destroy(image);

        yield return null;
    }

    // 更新UI参数显示
    void UpdateParameterDisplay()
    {
        // Update focal length
        if (focalLengthSlider != null)
        {
            focalLengthSlider.value = Mathf.InverseLerp(focalLengthMin, focalLengthMax, currentFocalLength);
        }
        if (focalLengthText != null)
        {
            focalLengthText.text = $"{currentFocalLength:F1}mm";
        }

        // Update focus distance
        if (focusDistanceSlider != null)
        {
            focusDistanceSlider.value = Mathf.InverseLerp(focusDistanceMin, focusDistanceMax, currentFocusDistance);
        }
        if (focusDistanceText != null)
        {
            focusDistanceText.text = $"{currentFocusDistance:F1}m";
        }

        // Update ISO
        if (isoSlider != null)
        {
            isoSlider.value = Mathf.InverseLerp(isoMin, isoMax, currentISO);
        }
        if (isoText != null)
        {
            isoText.text = $"{currentISO}";
        }

        // Update aperture
        if (apertureSlider != null)
        {
            apertureSlider.value = Mathf.InverseLerp(apertureMin, apertureMax, currentAperture);
        }
        if (apertureText != null)
        {
            apertureText.text = $"f/{currentAperture:F1}";
        }

        // Update shutter speed
        if (shutterSpeedSlider != null)
        {
            shutterSpeedSlider.value = Mathf.InverseLerp(Mathf.Log10(shutterMin), Mathf.Log10(shutterMax), Mathf.Log10(currentShutter));
        }
        if (shutterSpeedText != null)
        {
            shutterSpeedText.text = FormatShutterSpeed(currentShutter);
        }
    }

    // 格式化快门速度显示
    string FormatShutterSpeed(float shutter)
    {
        if (shutter >= 1f)
            return $"{shutter:F1}s";
        else if (shutter >= 0.1f)
            return $"{shutter:F2}s";
        else
            return $"{shutter:F4}s";
    }

    // 公共方法用于获取当前参数（可用于UI显示）
    public string GetCurrentParameters()
    {
        return $"Focal Length: {currentFocalLength:F1}mm\n" +
               $"Focus Distance: {currentFocusDistance:F1}m\n" +
               $"ISO: {currentISO:F0}\n" +
               $"Aperture: f/{currentAperture:F1}\n" +
               $"Shutter: {FormatShutterSpeed(currentShutter)}";
    }
}