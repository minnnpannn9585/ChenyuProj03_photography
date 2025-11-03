using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

/// <summary>
/// VR相机装备 - 管理PhotoScene中右手固定的相机系统
/// 将现有CameraController集成到VR环境中
/// </summary>
[RequireComponent(typeof(CameraController))]
public class VRCameraRig : MonoBehaviour
{
    [Header("VR控制器设置")]
    public Transform rightHandController; // 右手控制器变换
    public Transform cameraAttachPoint; // 相机附着点
    public Vector3 cameraPositionOffset = new Vector3(0.05f, -0.02f, 0.1f); // 相机位置偏移
    public Vector3 cameraRotationOffset = new Vector3(0f, 0f, 0f); // 相机旋转偏移

    [Header("相机模型")]
    public GameObject cameraModel; // 相机模型（用于视觉展示）
    public bool showCameraModel = true; // 是否显示相机模型

    [Header("物理设置")]
    public float cameraWeight = 0.5f; // 相机重量（影响手部物理效果）
    public bool enableHandPhysics = true; // 启用手部物理

    [Header("音频反馈")]
    public AudioClip shutterSound; // 快门音效
    public AudioClip focusSound; // 对焦音效
    public AudioClip parameterChangeSound; // 参数调节音效

    [Header("UI设置")]
    public bool followCameraModel = true; // UI是否跟随相机模型
    public Transform defaultUIParent; // 默认UI父级（固定位置时使用）
    public bool allowUIControl = false; // 是否允许脚本控制UI位置（完全禁用时保持Editor设置）

    // 私有变量
    private CameraController cameraController;
    private XRController rightController;
    private GameObject spawnedCameraModel;
    private AudioSource audioSource;
    private bool isInitialized = false;

    // UI相关变量
    private Canvas[] uiCanvases; // 相机相关的Canvas组件
    private Transform[] originalUIParents; // UI原始父级

    void Start()
    {
        InitializeVRCamera();
    }

    /// <summary>
    /// 初始化VR相机系统
    /// </summary>
    private void InitializeVRCamera()
    {
        Debug.Log("[VRCameraRig] 初始化VR相机系统...");

        // 获取CameraController组件
        cameraController = GetComponent<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("[VRCameraRig] 需要CameraController组件！");
            return;
        }

        // 初始化音频源
        InitializeAudioSource();

        // 延迟查找控制器，等待XR系统完全加载
        StartCoroutine(DelayedControllerInitialization());
    }

    /// <summary>
    /// 延迟初始化控制器
    /// </summary>
    private System.Collections.IEnumerator DelayedControllerInitialization()
    {
        // 等待几帧让XR系统完全加载
        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }

        // 查找右手控制器
        FindRightHandController();

        // 如果找到控制器，设置其他组件
        if (rightHandController != null)
        {
            // 设置相机附着点
            SetupCameraAttachPoint();

            // 创建相机模型
            CreateCameraModel();

            // 配置相机物理属性
            SetupCameraPhysics();

            // 集成现有相机系统
            IntegrateExistingCameraSystem();

            isInitialized = true;
            Debug.Log("[VRCameraRig] VR相机系统初始化完成");

            // 延迟设置UI跟随，确保相机模型已创建
            StartCoroutine(DelayedUISetup());
        }
        else
        {
            Debug.LogError("[VRCameraRig] 控制器初始化失败，VR相机系统无法正常工作");
        }
    }

    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D音效
        audioSource.volume = 0.8f;
    }

    /// <summary>
    /// 查找右手控制器
    /// </summary>
    /// <summary>
    /// 查找右手控制器
    /// </summary>
    /// <summary>
    /// 查找右手控制器
    /// </summary>
    private void FindRightHandController()
    {
        // 如果已经在Inspector中指定了右手控制器，直接使用
        if (rightHandController != null)
        {
            Debug.Log("[VRCameraRig] 使用指定的右手控制器: " + rightHandController.name);
            return;
        }

        // 首先尝试查找XR Controller
        XRController[] controllers = FindObjectsOfType<XRController>();
        
        foreach (XRController controller in controllers)
        {
            if (controller.name.ToLower().Contains("right") ||
                (controller.transform.parent != null && controller.transform.parent.name.ToLower().Contains("right")) ||
                controller.transform.position.x > 0)
            {
                rightHandController = controller.transform;
                rightController = controller;
                Debug.Log("[VRCameraRig] 找到右手控制器: " + controller.name);
                return;
            }
        }
        
        // 如果没有找到XR Controller，尝试查找ActionBasedController
        var actionBasedControllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
        foreach (var controller in actionBasedControllers)
        {
            if (controller.name.ToLower().Contains("right") ||
                (controller.transform.parent != null && controller.transform.parent.name.ToLower().Contains("right")) ||
                controller.transform.position.x > 0)
            {
                rightHandController = controller.transform;
                Debug.Log("[VRCameraRig] 找到Action-based右手控制器: " + controller.name);
                return;
            }
        }
        
        Debug.LogWarning("[VRCameraRig] 未找到右手控制器！尝试手动指定。");
    }

    /// <summary>
    /// 设置相机附着点
    /// </summary>
    private void SetupCameraAttachPoint()
    {
        if (rightHandController == null)
        {
            Debug.LogError("右手控制器未找到，无法设置相机附着点！");
            return;
        }

        // 如果没有指定附着点，使用右手控制器
        if (cameraAttachPoint == null)
        {
            cameraAttachPoint = rightHandController;
        }

        Debug.Log("相机附着点设置完成");
    }

    /// <summary>
    /// 创建相机模型
    /// </summary>
    private void CreateCameraModel()
    {
        if (!showCameraModel) return;

        if (cameraModel != null)
        {
            // 实例化相机模型
            spawnedCameraModel = Instantiate(cameraModel, cameraAttachPoint);
            spawnedCameraModel.transform.localPosition = cameraPositionOffset;
            spawnedCameraModel.transform.localRotation = Quaternion.Euler(cameraRotationOffset);
        }
        else
        {
            // 创建简单的相机模型（立方体代表）
            CreateSimpleCameraModel();
        }

        Debug.Log("相机模型创建完成");
    }

    /// <summary>
    /// 创建简单的相机模型
    /// </summary>
    private void CreateSimpleCameraModel()
    {
        spawnedCameraModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spawnedCameraModel.name = "SimpleCameraModel";
        spawnedCameraModel.transform.SetParent(cameraAttachPoint);
        spawnedCameraModel.transform.localPosition = cameraPositionOffset;
        spawnedCameraModel.transform.localRotation = Quaternion.Euler(cameraRotationOffset);
        spawnedCameraModel.transform.localScale = new Vector3(0.1f, 0.06f, 0.15f);

        // 设置材质
        Renderer renderer = spawnedCameraModel.GetComponent<Renderer>();
        Material cameraMaterial = new Material(Shader.Find("Standard"));
        cameraMaterial.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        renderer.material = cameraMaterial;

        // 移除碰撞体（不需要物理交互）
        Collider collider = spawnedCameraModel.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    /// <summary>
    /// 设置相机物理属性
    /// </summary>
    private void SetupCameraPhysics()
    {
        if (enableHandPhysics && rightController != null)
        {
            // 可以在这里添加手部物理设置
            // 比如调整控制器的物理参数以反映相机重量
            Rigidbody controllerRb = rightController.GetComponent<Rigidbody>();
            if (controllerRb != null)
            {
                controllerRb.mass = cameraWeight;
            }
        }
    }

    /// <summary>
    /// 集成现有相机系统
    /// </summary>
    private void IntegrateExistingCameraSystem()
    {
        // 确保相机控制器使用正确的相机
        if (cameraController.photographyCamera == null)
        {
            // 如果没有指定摄影相机，使用主相机
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraController.photographyCamera = mainCamera;
                Debug.Log("设置主相机为摄影相机");
            }
        }

        // 将相机附加到右手控制器
        if (cameraController.photographyCamera != null)
        {
            cameraController.photographyCamera.transform.SetParent(cameraAttachPoint);
            cameraController.photographyCamera.transform.localPosition = cameraPositionOffset;
            cameraController.photographyCamera.transform.localRotation = Quaternion.Euler(cameraRotationOffset);
        }

        Debug.Log("现有相机系统集成完成");
    }

    /// <summary>
    /// 设置UI跟随模式
    /// </summary>
    private void SetupUIFollowing()
    {
        // 如果不允许UI控制，直接返回
        if (!allowUIControl)
        {
            Debug.Log("[VRCameraRig] UI控制已禁用，保持Editor中的原始设置");
            return;
        }

        if (cameraController == null) return;

        // 查找相机Controller下的所有Canvas
        uiCanvases = cameraController.GetComponentsInChildren<Canvas>(true);

        if (uiCanvases.Length > 0)
        {
            originalUIParents = new Transform[uiCanvases.Length];

            for (int i = 0; i < uiCanvases.Length; i++)
            {
                // 保存原始父级
                originalUIParents[i] = uiCanvases[i].transform.parent;

                // 只在开启跟随模式时调整UI，关闭时保持原始状态
                if (followCameraModel)
                {
                    UpdateUIFollowing(uiCanvases[i].transform, i);
                }
            }

            Debug.Log($"[VRCameraRig] 找到 {uiCanvases.Length} 个UI Canvas，跟随模式: {(followCameraModel ? "跟随相机模型" : "保持原始位置")}");
        }
        else
        {
            Debug.LogWarning("[VRCameraRig] 未找到任何UI Canvas");
        }
    }

    /// <summary>
    /// 更新单个UI的跟随状态
    /// </summary>
    /// <summary>
    /// 更新单个UI的跟随状态
    /// </summary>
    private void UpdateUIFollowing(Transform uiTransform, int index)
    {
        if (followCameraModel)
        {
            // 跟随相机模型
            if (spawnedCameraModel != null)
            {
                uiTransform.SetParent(spawnedCameraModel.transform);
                Debug.Log($"[VRCameraRig] {uiTransform.name} 跟随相机模型");
            }
        }
        else
        {
            // 关闭跟随模式时保持原始状态，不进行任何调整
            Debug.Log($"[VRCameraRig] {uiTransform.name} 保持原始位置（关闭跟随模式）");
        }
    }

    /// <summary>
    /// 切换UI跟随模式（公共方法）
    /// </summary>
    public void ToggleUIFollowing()
    {
        followCameraModel = !followCameraModel;
        RefreshUIFollowing();
        
        Debug.Log($"[VRCameraRig] UI跟随模式已切换为: {(followCameraModel ? "跟随相机模型" : "固定位置")}");
    }

    /// <summary>
    /// 设置UI跟随模式
    /// </summary>
    /// <summary>
    /// 设置UI跟随模式
    /// </summary>
    public void SetUIFollowing(bool followModel)
    {
        if (followCameraModel != followModel)
        {
            followCameraModel = followModel;
            
            // 只在开启跟随模式时才刷新UI
            if (followCameraModel)
            {
                RefreshUIFollowing();
            }
            
            Debug.Log($"[VRCameraRig] UI跟随模式设置为: {(followCameraModel ? "跟随相机模型" : "保持原始位置")}");
        }
    }

    /// <summary>
    /// 刷新所有UI的跟随状态
    /// </summary>
    private void RefreshUIFollowing()
    {
        // 如果不允许UI控制，直接返回
        if (!allowUIControl) return;

        if (uiCanvases == null || uiCanvases.Length == 0) return;

        // 只在开启跟随模式时才进行调整
        if (followCameraModel)
        {
            for (int i = 0; i < uiCanvases.Length; i++)
            {
                if (uiCanvases[i] != null)
                {
                    UpdateUIFollowing(uiCanvases[i].transform, i);
                }
            }
        }
        // 关闭跟随模式时不做任何操作，保持UI原始状态
    }

    /// <summary>
    /// 获取当前UI跟随模式
    /// </summary>
    public bool GetUIFollowingMode()
    {
        return followCameraModel;
    }

    /// <summary>
    /// 获取UI控制模式
    /// </summary>
    public bool GetUIControlMode()
    {
        return allowUIControl;
    }

    /// <summary>
    /// 切换UI控制模式
    /// </summary>
    public void ToggleUIControl()
    {
        allowUIControl = !allowUIControl;

        // 如果刚启用控制，需要重新设置UI
        if (allowUIControl)
        {
            SetupUIFollowing();
        }

        Debug.Log($"[VRCameraRig] UI控制模式已切换为: {(allowUIControl ? "脚本控制" : "Editor控制")}");
    }

    /// <summary>
    /// 设置UI控制模式
    /// </summary>
    public void SetUIControl(bool allowControl)
    {
        if (allowUIControl != allowControl)
        {
            allowUIControl = allowControl;

            // 如果刚启用控制，需要重新设置UI
            if (allowUIControl)
            {
                SetupUIFollowing();
            }

            Debug.Log($"[VRCameraRig] UI控制模式设置为: {(allowUIControl ? "脚本控制" : "Editor控制")}");
        }
    }

    /// <summary>
    /// 延迟设置UI跟随
    /// </summary>
    private System.Collections.IEnumerator DelayedUISetup()
    {
        // 等待一帧，确保所有组件都已初始化
        yield return null;
        
        // 设置UI跟随
        SetupUIFollowing();
    }

    



    /// <summary>
    /// 播放快门音效
    /// </summary>
    public void PlayShutterSound()
    {
        if (shutterSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shutterSound);
        }
    }

    /// <summary>
    /// 播放对焦音效
    /// </summary>
    public void PlayFocusSound()
    {
        if (focusSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(focusSound);
        }
    }

    /// <summary>
    /// 播放参数调节音效
    /// </summary>
    public void PlayParameterChangeSound()
    {
        if (parameterChangeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(parameterChangeSound);
        }
    }

    /// <summary>
    /// 获取相机当前位置
    /// </summary>
    public Vector3 GetCameraPosition()
    {
        if (cameraController != null && cameraController.photographyCamera != null)
        {
            return cameraController.photographyCamera.transform.position;
        }
        return transform.position;
    }

    /// <summary>
    /// 获取相机当前朝向
    /// </summary>
    public Quaternion GetCameraRotation()
    {
        if (cameraController != null && cameraController.photographyCamera != null)
        {
            return cameraController.photographyCamera.transform.rotation;
        }
        return transform.rotation;
    }

    /// <summary>
    /// 设置相机位置偏移
    /// </summary>
    public void SetCameraPositionOffset(Vector3 offset)
    {
        cameraPositionOffset = offset;

        if (cameraController != null && cameraController.photographyCamera != null)
        {
            cameraController.photographyCamera.transform.localPosition = offset;
        }

        if (spawnedCameraModel != null)
        {
            spawnedCameraModel.transform.localPosition = offset;
        }
    }

    /// <summary>
    /// 设置相机旋转偏移
    /// </summary>
    public void SetCameraRotationOffset(Vector3 offset)
    {
        cameraRotationOffset = offset;

        if (cameraController != null && cameraController.photographyCamera != null)
        {
            cameraController.photographyCamera.transform.localRotation = Quaternion.Euler(offset);
        }

        if (spawnedCameraModel != null)
        {
            spawnedCameraModel.transform.localRotation = Quaternion.Euler(offset);
        }
    }

    /// <summary>
    /// 切换相机模型显示
    /// </summary>
    public void ToggleCameraModel()
    {
        showCameraModel = !showCameraModel;

        if (spawnedCameraModel != null)
        {
            spawnedCameraModel.SetActive(showCameraModel);
        }
    }

    /// <summary>
    /// 获取相机控制器引用
    /// </summary>
    public CameraController GetCameraController()
    {
        return cameraController;
    }

    void Update()
    {
        if (!isInitialized) return;

        // 可以在这里添加更新逻辑
        // 比如检测控制器状态、更新相机位置等

        // 确保相机始终附着在手上
        UpdateCameraAttachment();
    }

    /// <summary>
    /// 更新相机附着
    /// </summary>
    private void UpdateCameraAttachment()
    {
        if (cameraController != null && cameraController.photographyCamera != null)
        {
            // 确保相机跟随右手控制器
            if (cameraAttachPoint != null)
            {
                cameraController.photographyCamera.transform.position = cameraAttachPoint.position;
                cameraController.photographyCamera.transform.rotation = cameraAttachPoint.rotation;
            }
        }
    }

    void OnDestroy()
    {
        // 清理相机模型
        if (spawnedCameraModel != null)
        {
            Destroy(spawnedCameraModel);
        }

        // 恢复相机的父级关系
        if (cameraController != null && cameraController.photographyCamera != null)
        {
            cameraController.photographyCamera.transform.SetParent(null);
        }
    }

    /// <summary>
    /// 调试信息显示
    /// </summary>
    void OnGUI()
    {
        if (!isInitialized) return;

        // 显示调试信息（仅在编辑器中）
        #if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("VR相机系统状态");
        GUILayout.Label("右手控制器: " + (rightController != null ? "已连接" : "未找到"));
        GUILayout.Label("相机控制器: " + (cameraController != null ? "已集成" : "未找到"));
        GUILayout.Label("相机模型: " + (showCameraModel ? "显示" : "隐藏"));

        if (GUILayout.Button("切换相机模型"))
        {
            ToggleCameraModel();
        }

        GUILayout.EndArea();
        #endif
    }
}