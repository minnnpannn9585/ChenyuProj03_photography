using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

/// <summary>
/// VR主菜单控制器 - 管理MainMenu场景的整体逻辑
/// 处理菜单物体的创建、管理和交互
/// </summary>
public class VRMenuController : MonoBehaviour
{
    [Header("菜单设置")]
    public GameObject photoSceneObject; // 摄影场景菜单物体
    public GameObject museumSceneObject; // 博物馆场景菜单物体

    [Header("UI设置")]
    public Canvas mainMenuCanvas; // 主菜单Canvas
    public TextMeshProUGUI titleText; // 标题文本
    public TextMeshProUGUI instructionText; // 操作说明文本

    [Header("环境设置")]
    public Material floorMaterial; // 地面材质
    public Vector3 roomSize = new Vector3(10f, 3f, 10f); // 房间大小
    public Color ambientColor = new Color(0.5f, 0.5f, 0.6f, 1f); // 环境光颜色

    [Header("音频设置")]
    public AudioClip backgroundMusic; // 背景音乐
    public float musicVolume = 0.3f; // 音乐音量

    // 私有变量
    private AudioSource audioSource;
    private VRGrabbableObject[] menuObjects;
    // private bool isInitialized = false; // 暂时注释避免警告

    void Start()
    {
        InitializeMenu();
    }

    /// <summary>
    /// 初始化主菜单
    /// </summary>
    private void InitializeMenu()
    {
        Debug.Log("初始化VR主菜单...");

        // 初始化音频系统
        InitializeAudio();

        // 设置环境
        SetupEnvironment();

        // 设置UI
        SetupUI();

        // 初始化菜单物体
        InitializeMenuObjects();

        // 验证VR系统
        VerifyVRSystem();

        // isInitialized = true; // 暂时注释避免警告
        Debug.Log("VR主菜单初始化完成");
    }

    /// <summary>
    /// 初始化音频系统
    /// </summary>
    private void InitializeAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = musicVolume;

        if (backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.Play();
        }
    }

    /// <summary>
    /// 设置环境
    /// </summary>
    private void SetupEnvironment()
    {
        // 设置环境光
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        // 创建地面（如果不存在）
        CreateFloor();

        // 设置房间边界（可选）
        SetupRoomBoundaries();
    }

    /// <summary>
    /// 创建地面
    /// </summary>
    private void CreateFloor()
    {
        // 检查是否已存在地面
        GameObject existingFloor = GameObject.Find("Floor");
        if (existingFloor != null)
        {
            return;
        }

        // 创建地面物体
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(roomSize.x / 10f, 1f, roomSize.z / 10f);
        floor.transform.position = Vector3.zero;

        // 设置地面材质
        if (floorMaterial != null)
        {
            floor.GetComponent<Renderer>().material = floorMaterial;
        }
        else
        {
            // 创建默认材质
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            floor.GetComponent<Renderer>().material = defaultMaterial;
        }

        // 添加碰撞体
        if (floor.GetComponent<Collider>() == null)
        {
            floor.AddComponent<BoxCollider>();
        }
    }

    /// <summary>
    /// 设置房间边界
    /// </summary>
    private void SetupRoomBoundaries()
    {
        // 这里可以添加房间边界设置
        // 比如使用 invisible walls 或 Unity的XR Origin边界系统
    }

    /// <summary>
    /// 设置UI
    /// </summary>
    private void SetupUI()
    {
        // 设置主菜单Canvas
        if (mainMenuCanvas != null)
        {
            // 确保Canvas是World Space模式
            mainMenuCanvas.renderMode = RenderMode.WorldSpace;
            mainMenuCanvas.worldCamera = Camera.main;

            // 设置Canvas位置（在玩家前方）
            mainMenuCanvas.transform.position = new Vector3(0f, 1.6f, 3f);
            mainMenuCanvas.transform.rotation = Quaternion.identity;
        }

        // 设置标题文本
        if (titleText != null)
        {
            titleText.text = "VR 摄影体验";
            titleText.fontSize = 48;
            titleText.color = Color.white;
        }

        // 设置操作说明文本
        if (instructionText != null)
        {
            instructionText.text = "抓取物体查看预览\n按住A键进入场景";
            instructionText.fontSize = 24;
            instructionText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        }
    }

    /// <summary>
    /// 初始化菜单物体
    /// </summary>
    private void InitializeMenuObjects()
    {
        menuObjects = new VRGrabbableObject[2];

        // 初始化摄影场景物体
        if (photoSceneObject != null)
        {
            SetupMenuObject(photoSceneObject, "摄影场景", "体验专业VR摄影\n调整相机参数\n拍摄完美照片", VRGrabbableObject.SceneType.PhotoScene);
        }
        else
        {
            Debug.LogWarning("摄影场景物体未设置！");
        }

        // 初始化博物馆物体
        if (museumSceneObject != null)
        {
            SetupMenuObject(museumSceneObject, "博物馆", "探索虚拟博物馆\n欣赏精美展品\n沉浸式体验", VRGrabbableObject.SceneType.Museum);
        }
        else
        {
            Debug.LogWarning("博物馆物体未设置！");
        }
    }

    /// <summary>
    /// 设置单个菜单物体
    /// </summary>
    private void SetupMenuObject(GameObject obj, string name, string description, VRGrabbableObject.SceneType sceneType)
    {
        // 设置物体名称
        obj.name = name;

        // 设置位置（左右排列）
        int index = System.Array.IndexOf(menuObjects, null);
        float xOffset = (index - 0.5f) * 2f; // 左右间隔2单位
        obj.transform.position = new Vector3(xOffset, 1.2f, 2f);

        // 确保有VRGrabbableObject组件
        VRGrabbableObject grabbableObj = obj.GetComponent<VRGrabbableObject>();
        if (grabbableObj == null)
        {
            grabbableObj = obj.AddComponent<VRGrabbableObject>();
        }

        // 设置场景信息
        grabbableObj.targetScene = sceneType;
        grabbableObj.sceneDescription = description;

        // 确保有必要的物理组件
        EnsurePhysicsComponents(obj);

        // 添加到数组
        menuObjects[index] = grabbableObj;

        Debug.Log($"设置菜单物体: {name} -> {sceneType}");
    }

    /// <summary>
    /// 确保物体有必要的物理组件
    /// </summary>
    private void EnsurePhysicsComponents(GameObject obj)
    {
        // 确保有Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }

        // 设置Rigidbody属性
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 1f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;

        // 确保有Collider
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            // 如果物体是基本几何体，添加相应的碰撞体
            if (obj.GetComponent<MeshFilter>() != null)
            {
                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.convex = true; // 允许物理交互
            }
            else
            {
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                boxCollider.size = Vector3.one;
            }
        }
    }

    /// <summary>
    /// 验证VR系统
    /// </summary>
    private void VerifyVRSystem()
    {
        // 检查XR Origin是否存在
        GameObject xrOriginObj = GameObject.Find("XR Origin");
        if (xrOriginObj == null)
        {
            Debug.LogWarning("未找到XR Origin！请确保场景中有XR Origin (Action-based)");
        }
        else
        {
            Debug.Log("XR Origin已找到");
        }

        // 检查控制器
        XRController[] controllers = FindObjectsOfType<XRController>();
        Debug.Log($"找到 {controllers.Length} 个XR控制器");

        // 检查VR设备状态
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            Debug.Log($"VR设备已激活: {UnityEngine.XR.XRSettings.loadedDeviceName}");
        }
        else
        {
            Debug.LogWarning("VR设备未激活！");
        }
    }

    /// <summary>
    /// 重置菜单物体位置
    /// </summary>
    public void ResetMenuObjects()
    {
        if (menuObjects == null) return;

        for (int i = 0; i < menuObjects.Length; i++)
        {
            if (menuObjects[i] != null)
            {
                // 重置到默认位置
                float xOffset = (i - 0.5f) * 2f;
                menuObjects[i].transform.position = new Vector3(xOffset, 1.2f, 2f);
                menuObjects[i].transform.rotation = Quaternion.identity;

                // 重置速度
                Rigidbody rb = menuObjects[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    /// <summary>
    /// 更新操作说明文本
    /// </summary>
    public void UpdateInstructionText(string text)
    {
        if (instructionText != null)
        {
            instructionText.text = text;
        }
    }

    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    void Update()
    {
        // 可以在这里添加更新逻辑
        // 比如检测特定按键重置菜单等

        // 按R键重置菜单物体位置（调试用）
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetMenuObjects();
        }

        // 按ESC键退出应用（调试用）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("退出应用请求");
            Application.Quit();
        }
    }

    void OnDestroy()
    {
        // 清理资源
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
        else if (!pauseStatus && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}