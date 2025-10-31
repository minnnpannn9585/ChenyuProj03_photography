using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VR快速设置脚本
/// 一键配置CameraModule预制件支持VR
/// </summary>
public class VRQuickSetup : MonoBehaviour
{
    [Header("自动配置")]
    public bool autoSetupOnStart = true;
    public bool findCameraModule = true;

    [Header("查找设置")]
    public string cameraModuleName = "CameraModule";
    public string canvasName = "WorldSpaceCanvas";

    [Header("VR组件设置")]
    public bool addVRAdapter = true;
    public bool addVRSliderInteraction = true;
    public bool addVRController = true;
    public bool addVRModel = true;

    [Header("相机设置")]
    public bool attachCameraToHand = true;
    public Transform rightHandAnchor;
    public Transform leftHandAnchor;

    // 找到的组件引用
    private GameObject cameraModuleObject;
    private Canvas worldSpaceCanvas;
    private CameraController legacyCameraController;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupVRForCameraModule();
        }
    }

    /// <summary>
    /// 设置CameraModule的VR支持
    /// </summary>
    [ContextMenu("Setup VR for CameraModule")]
    public void SetupVRForCameraModule()
    {
        Debug.Log("开始VR快速设置...");

        // 1. 查找CameraModule
        if (!FindCameraModule())
        {
            Debug.LogError("VRQuickSetup: 未找到CameraModule，请确保场景中存在CameraModule预制件");
            return;
        }

        // 2. 查找现有组件
        FindExistingComponents();

        // 3. 添加VR适配器
        if (addVRAdapter)
        {
            AddVRAdapter();
        }

        // 4. 添加VR滑块交互
        if (addVRSliderInteraction)
        {
            AddVRSliderInteraction();
        }

        // 5. 添加VR相机控制器
        if (addVRController)
        {
            AddVRController();
        }

        // 6. 添加VR相机模型
        if (addVRModel)
        {
            AddVRCameraModel();
        }

        // 7. 配置Canvas
        ConfigureCanvasForVR();

        // 8. 设置手柄锚点
        SetupHandAnchors();

        Debug.Log("VR快速设置完成！");
        PrintSetupSummary();
    }

    /// <summary>
    /// 查找CameraModule
    /// </summary>
    private bool FindCameraModule()
    {
        // 首先尝试通过名称查找
        cameraModuleObject = GameObject.Find(cameraModuleName);

        if (cameraModuleObject == null)
        {
            // 尝试查找包含CameraModule的对象
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Camera") && obj.name.Contains("Module"))
                {
                    cameraModuleObject = obj;
                    break;
                }
            }
        }

        if (cameraModuleObject == null)
        {
            // 尝试查找有CameraController的对象
            CameraController[] controllers = FindObjectsOfType<CameraController>();
            if (controllers.Length > 0)
            {
                cameraModuleObject = controllers[0].gameObject;
                Debug.Log($"VRQuickSetup: 通过CameraController找到CameraModule: {cameraModuleObject.name}");
            }
        }

        return cameraModuleObject != null;
    }

    /// <summary>
    /// 查找现有组件
    /// </summary>
    private void FindExistingComponents()
    {
        // 查找Canvas
        if (cameraModuleObject != null)
        {
            Transform canvasTransform = cameraModuleObject.transform.Find("Module/" + canvasName);
            if (canvasTransform != null)
            {
                worldSpaceCanvas = canvasTransform.GetComponent<Canvas>();
            }
        }

        // 查找CameraController
        if (cameraModuleObject != null)
        {
            legacyCameraController = cameraModuleObject.GetComponentInChildren<CameraController>();
        }

        Debug.Log($"找到组件 - Canvas: {worldSpaceCanvas != null}, CameraController: {legacyCameraController != null}");
    }

    /// <summary>
    /// 添加VR适配器
    /// </summary>
    private void AddVRAdapter()
    {
        if (cameraModuleObject == null) return;

        VRCameraAdapter adapter = cameraModuleObject.GetComponent<VRCameraAdapter>();
        if (adapter == null)
        {
            adapter = cameraModuleObject.AddComponent<VRCameraAdapter>();
        }

        // 配置适配器设置
        if (adapter.cameraModuleAdapter == null)
        {
            adapter.cameraModuleAdapter = new VRCameraAdapter.CameraModuleAdapter();
        }

        adapter.cameraModuleAdapter.worldSpaceCanvas = worldSpaceCanvas;
        adapter.cameraModuleAdapter.adaptCanvasForVR = true;
        adapter.cameraModuleAdapter.enableVRSliderInteraction = true;

        Debug.Log("VRQuickSetup: 已添加VRCameraAdapter");
    }

    /// <summary>
    /// 添加VR滑块交互
    /// </summary>
    private void AddVRSliderInteraction()
    {
        if (worldSpaceCanvas == null) return;

        Slider[] sliders = worldSpaceCanvas.GetComponentsInChildren<Slider>();
        int addedCount = 0;

        foreach (var slider in sliders)
        {
            VRSliderInteraction vrInteraction = slider.GetComponent<VRSliderInteraction>();
            if (vrInteraction == null)
            {
                vrInteraction = slider.gameObject.AddComponent<VRSliderInteraction>();
                vrInteraction.Initialize(slider, 2f);
                addedCount++;
            }
        }

        Debug.Log($"VRQuickSetup: 已为{addedCount}个滑块添加VR交互");
    }

    /// <summary>
    /// 添加VR相机控制器
    /// </summary>
    private void AddVRController()
    {
        if (cameraModuleObject == null) return;

        // 检查是否已有VRCameraController
        VRCameraController vrController = cameraModuleObject.GetComponent<VRCameraController>();
        if (vrController == null)
        {
            vrController = cameraModuleObject.AddComponent<VRCameraController>();
        }

        // 配置VR控制器
        if (legacyCameraController != null)
        {
            // 复制现有设置
            vrController.photographyCamera = legacyCameraController.photographyCamera;
            vrController.previewUI = legacyCameraController.previewUI;
            vrController.isoSlider = legacyCameraController.isoSlider;
            vrController.apertureSlider = legacyCameraController.apertureSlider;
            vrController.shutterSlider = legacyCameraController.shutterSlider;
            vrController.focalLengthSlider = legacyCameraController.focalLengthSlider;
            vrController.focusDistanceSlider = legacyCameraController.focusDistanceSlider;

            Debug.Log("VRQuickSetup: 已配置VRCameraController与现有组件的连接");
        }

        // 添加VR曝光控制器
        VRCustomExposureController exposureController = cameraModuleObject.GetComponent<VRCustomExposureController>();
        if (exposureController == null)
        {
            exposureController = cameraModuleObject.AddComponent<VRCustomExposureController>();
        }

        // 查找Volume组件
        Volume volume = cameraModuleObject.GetComponentInChildren<Volume>();
        if (volume != null)
        {
            exposureController.postVolume = volume;
            Debug.Log("VRQuickSetup: 已配置VRCustomExposureController的Volume");
        }

        Debug.Log("VRQuickSetup: 已添加VRCameraController和VRCustomExposureController");
    }

    /// <summary>
    /// 添加VR相机模型
    /// </summary>
    private void AddVRCameraModel()
    {
        if (cameraModuleObject == null) return;

        VRCameraModel cameraModel = cameraModuleObject.GetComponent<VRCameraModel>();
        if (cameraModel == null)
        {
            cameraModel = cameraModuleObject.AddComponent<VRCameraModel>();
        }

        // 查找现有的3D模型作为相机模型
        Transform[] childTransforms = cameraModuleObject.GetComponentsInChildren<Transform>();
        foreach (var transform in childTransforms)
        {
            if (transform.name.ToLower().Contains("camera") && transform != cameraModuleObject.transform)
            {
                cameraModel.cameraModel = transform.gameObject;
                Debug.Log($"VRQuickSetup: 已找到相机模型: {transform.name}");
                break;
            }
        }

        Debug.Log("VRQuickSetup: 已添加VRCameraModel");
    }

    /// <summary>
    /// 配置Canvas为VR
    /// </summary>
    private void ConfigureCanvasForVR()
    {
        if (worldSpaceCanvas == null) return;

        // 确保Canvas是World Space模式
        worldSpaceCanvas.renderMode = RenderMode.WorldSpace;

        // 调整Canvas的大小和位置以适应VR
        RectTransform canvasRect = worldSpaceCanvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = new Vector2(1920, 1080);
            canvasRect.localScale = Vector3.one * 0.001f; // VR缩放
        }

        Debug.Log("VRQuickSetup: 已配置Canvas为VR模式");
    }

    /// <summary>
    /// 设置手柄锚点
    /// </summary>
    private void SetupHandAnchors()
    {
        // 自动查找手柄锚点
        if (rightHandAnchor == null)
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                Transform parent = centerEye.transform.parent;
                if (parent != null)
                {
                    rightHandAnchor = parent.Find("RightHandAnchor");
                    leftHandAnchor = parent.Find("LeftHandAnchor");
                }
            }
        }

        // 如果还是找不到，创建虚拟锚点
        if (rightHandAnchor == null)
        {
            GameObject rightHand = new GameObject("VRRightHandAnchor");
            rightHand.transform.SetParent(cameraModuleObject.transform);
            rightHand.transform.localPosition = new Vector3(0.3f, 0, 0.5f);
            rightHandAnchor = rightHand.transform;
        }

        if (leftHandAnchor == null)
        {
            GameObject leftHand = new GameObject("VRLeftHandAnchor");
            leftHand.transform.SetParent(cameraModuleObject.transform);
            leftHand.transform.localPosition = new Vector3(-0.3f, 0, 0.5f);
            leftHandAnchor = leftHand.transform;
        }

        // 设置手柄锚点到VR控制器
        VRCameraController vrController = cameraModuleObject.GetComponent<VRCameraController>();
        if (vrController != null)
        {
            vrController.rightHandAnchor = rightHandAnchor;
            vrController.leftHandAnchor = leftHandAnchor;
        }

        Debug.Log($"VRQuickSetup: 已设置手柄锚点 - Right: {(rightHandAnchor != null ? "Found" : "Created")}, Left: {(leftHandAnchor != null ? "Found" : "Created")}");
    }

    /// <summary>
    /// 打印设置摘要
    /// </summary>
    private void PrintSetupSummary()
    {
        Debug.Log("=== VR快速设置摘要 ===");
        Debug.Log($"CameraModule: {(cameraModuleObject != null ? cameraModuleObject.name : "未找到")}");
        Debug.Log($"Canvas: {(worldSpaceCanvas != null ? "已配置" : "未找到")}");
        Debug.Log($"Legacy CameraController: {(legacyCameraController != null ? "已找到" : "未找到")}");
        Debug.Log($"VR Adapter: {(cameraModuleObject.GetComponent<VRCameraAdapter>() != null ? "已添加" : "未添加")}");
        Debug.Log($"VR Controller: {(cameraModuleObject.GetComponent<VRCameraController>() != null ? "已添加" : "未添加")}");
        Debug.Log($"VR Exposure Controller: {(cameraModuleObject.GetComponent<VRCustomExposureController>() != null ? "已添加" : "未添加")}");
        Debug.Log($"VR Camera Model: {(cameraModuleObject.GetComponent<VRCameraModel>() != null ? "已添加" : "未添加")}");
        Debug.Log("=== 设置完成 ===");
    }

    /// <summary>
    /// 手动查找并设置手柄锚点
    /// </summary>
    [ContextMenu("Find and Setup Hand Anchors")]
    public void FindAndSetupHandAnchors()
    {
        SetupHandAnchors();
    }

    /// <summary>
    /// 测试VR控制
    /// </summary>
    [ContextMenu("Test VR Controls")]
    public void TestVRControls()
    {
        VRCameraController vrController = cameraModuleObject?.GetComponent<VRCameraController>();
        if (vrController != null)
        {
            Debug.Log("VR Controller Test:");
            Debug.Log($"- Right Hand Anchor: {(vrController.rightHandAnchor != null ? "Set" : "Not Set")}");
            Debug.Log($"- Left Hand Anchor: {(vrController.leftHandAnchor != null ? "Set" : "Not Set")}");
            Debug.Log($"- Photography Camera: {(vrController.photographyCamera != null ? "Set" : "Not Set")}");
            Debug.Log($"- Preview UI: {(vrController.previewUI != null ? "Set" : "Not Set")}");
        }
        else
        {
            Debug.LogError("VRQuickSetup: 找不到VRCameraController");
        }
    }

    /// <summary>
    /// 清理VR组件（用于测试）
    /// </summary>
    [ContextMenu("Remove VR Components")]
    public void RemoveVRComponents()
    {
        if (cameraModuleObject == null) return;

        // 移除VR组件
        DestroyImmediate(cameraModuleObject.GetComponent<VRCameraAdapter>());
        DestroyImmediate(cameraModuleObject.GetComponent<VRCameraController>());
        DestroyImmediate(cameraModuleObject.GetComponent<VRCustomExposureController>());
        DestroyImmediate(cameraModuleObject.GetComponent<VRCameraModel>());

        // 移除VR滑块交互
        VRSliderInteraction[] vrInteractions = cameraModuleObject.GetComponentsInChildren<VRSliderInteraction>();
        foreach (var interaction in vrInteractions)
        {
            DestroyImmediate(interaction);
        }

        Debug.Log("VRQuickSetup: 已移除所有VR组件");
    }
}