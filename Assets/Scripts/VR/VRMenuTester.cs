using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// VR主菜单测试器
/// 用于在编辑器中快速测试主菜单功能
/// </summary>
public class VRMenuTester : MonoBehaviour
{
    [Header("测试设置")]
    public bool enableTestMode = false;
    public KeyCode testGrabKey = KeyCode.G;
    public KeyCode testReleaseKey = KeyCode.R;
    public KeyCode testMenuKey = KeyCode.Escape;
    public KeyCode testSelectKey = KeyCode.Space;

    [Header("模拟手柄")]
    public Transform simulatedHandTransform;
    public float handMoveSpeed = 2f;

    [Header("测试UI")]
    public TMP_Text debugText;
    public bool showDebugInfo = true;

    private VRMenuController menuController;
    private VRGrabbable currentGrabbable;
    private bool isSimulatingGrab = false;
    private Vector3 handTargetPosition;

    void Start()
    {
        if (enableTestMode)
        {
            InitializeTestMode();
        }
    }

    /// <summary>
    /// 初始化测试模式
    /// </summary>
    private void InitializeTestMode()
    {
        menuController = FindObjectOfType<VRMenuController>();

        if (menuController == null)
        {
            Debug.LogError("VRMenuController not found!");
            enabled = false;
            return;
        }

        // 创建模拟手柄
        if (simulatedHandTransform == null)
        {
            GameObject hand = new GameObject("SimulatedHand");
            hand.transform.position = new Vector3(0, 1.5f, 1);
            simulatedHandTransform = hand.transform;
        }

        handTargetPosition = simulatedHandTransform.position;

        Debug.Log("VR Menu Test Mode initialized");
        UpdateDebugText("Test Mode Ready");
    }

    void Update()
    {
        if (!enableTestMode) return;

        HandleTestInput();
        UpdateSimulatedHand();
        UpdateDebugInfo();
    }

    /// <summary>
    /// 处理测试输入
    /// </summary>
    private void HandleTestInput()
    {
        // 手柄移动
        Vector3 moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveInput += simulatedHandTransform.forward;
        if (Input.GetKey(KeyCode.S)) moveInput -= simulatedHandTransform.forward;
        if (Input.GetKey(KeyCode.A)) moveInput -= simulatedHandTransform.right;
        if (Input.GetKey(KeyCode.D)) moveInput += simulatedHandTransform.right;
        if (Input.GetKey(KeyCode.Q)) moveInput += simulatedHandTransform.up;
        if (Input.GetKey(KeyCode.E)) moveInput -= simulatedHandTransform.up;

        if (moveInput != Vector3.zero)
        {
            handTargetPosition += moveInput * handMoveSpeed * Time.deltaTime;
        }

        // 抓取测试
        if (Input.GetKeyDown(testGrabKey))
        {
            TestGrab();
        }

        // 松开测试
        if (Input.GetKeyDown(testReleaseKey))
        {
            TestRelease();
        }

        // 选择测试
        if (Input.GetKeyDown(testSelectKey))
        {
            TestSelect();
        }

        // 菜单返回测试
        if (Input.GetKeyDown(testMenuKey))
        {
            TestMenuReturn();
        }
    }

    /// <summary>
    /// 更新模拟手柄位置
    /// </summary>
    private void UpdateSimulatedHand()
    {
        if (simulatedHandTransform != null)
        {
            simulatedHandTransform.position = Vector3.Lerp(
                simulatedHandTransform.position,
                handTargetPosition,
                Time.deltaTime * 5f
            );
        }
    }

    /// <summary>
    /// 测试抓取功能
    /// </summary>
    private void TestGrab()
    {
        if (currentGrabbable != null) return;

        // 查找可抓取物品
        Collider[] hitColliders = Physics.OverlapSphere(simulatedHandTransform.position, 0.3f);
        foreach (var collider in hitColliders)
        {
            VRGrabbable grabbable = collider.GetComponent<VRGrabbable>();
            if (grabbable != null && !grabbable.IsGrabbed())
            {
                currentGrabbable = grabbable;
                grabbable.Grab(simulatedHandTransform);
                UpdateDebugText($"Grabbed: {grabbable.GetSceneItem()?.displayName}");
                break;
            }
        }

        if (currentGrabbable == null)
        {
            UpdateDebugText("No grabbable item found");
        }
    }

    /// <summary>
    /// 测试松开功能
    /// </summary>
    private void TestRelease()
    {
        if (currentGrabbable != null)
        {
            currentGrabbable.Release();
            UpdateDebugText($"Released: {currentGrabbable.GetSceneItem()?.displayName}");
            currentGrabbable = null;
        }
        else
        {
            UpdateDebugText("No item to release");
        }
    }

    /// <summary>
    /// 测试选择功能
    /// </summary>
    private void TestSelect()
    {
        if (currentGrabbable != null)
        {
            var sceneItem = currentGrabbable.GetSceneItem();
            if (sceneItem != null)
            {
                UpdateDebugText($"Selected scene: {sceneItem.displayName}");
                // 这里可以触发场景切换
                // Debug.Log($"Would load scene: {sceneItem.sceneName}");
            }
        }
        else
        {
            UpdateDebugText("No item selected");
        }
    }

    /// <summary>
    /// 测试菜单返回
    /// </summary>
    private void TestMenuReturn()
    {
        UpdateDebugText("Menu return triggered");
        // 这里可以触发返回主菜单
    }

    /// <summary>
    /// 更新调试信息
    /// </summary>
    private void UpdateDebugInfo()
    {
        if (!showDebugInfo || debugText == null) return;

        string info = $"VR Menu Tester\n" +
                     $"Position: {simulatedHandTransform.position:F2}\n" +
                     $"Grabbed Item: {(currentGrabbable != null ? currentGrabbable.GetSceneItem()?.displayName : "None")}\n" +
                     $"Keys:\n" +
                     $"WASD/QE - Move hand\n" +
                     $"G - Grab\n" +
                     $"R - Release\n" +
                     $"Space - Select\n" +
                     $"Esc - Menu";

        debugText.text = info;
    }

    /// <summary>
    /// 更新调试文本
    /// </summary>
    private void UpdateDebugText(string message)
    {
        Debug.Log($"[VRMenuTester] {message}");
        if (showDebugInfo && debugText != null)
        {
            debugText.text = $"[VRMenuTester]\n{message}";
        }
    }

    /// <summary>
    /// 创建调试UI
    /// </summary>
    [ContextMenu("Create Debug UI")]
    public void CreateDebugUI()
    {
        if (debugText == null)
        {
            GameObject canvas = new GameObject("DebugCanvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(canvas.transform, false);

            debugText = textObj.AddComponent<TextMeshProUGUI>();
            debugText.fontSize = 14;
            debugText.color = Color.white;
            debugText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform rectTransform = debugText.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(10, 10);
            rectTransform.offsetMax = new Vector2(-10, -10);

            UpdateDebugText("Debug UI Created");
        }
    }

    /// <summary>
    /// 创建测试场景物品
    /// </summary>
    [ContextMenu("Create Test Scene Items")]
    public void CreateTestSceneItems()
    {
        // 创建简单的测试物品
        CreateTestItem("PhotoScene", "PhotoScene", Color.blue);
        CreateTestItem("Museum", "Museum", Color.green);

        UpdateDebugText("Test scene items created");
    }

    /// <summary>
    /// 创建测试物品
    /// </summary>
    private void CreateTestItem(string sceneName, string displayName, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = $"TestItem_{sceneName}";
        item.transform.position = new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(1f, 3f));
        item.transform.localScale = Vector3.one * 0.2f;

        Renderer renderer = item.GetComponent<Renderer>();
        renderer.material.color = color;

        Rigidbody rb = item.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        VRGrabbable grabbable = item.AddComponent<VRGrabbable>();

        // 创建场景配置
        VRMenuController.SceneItem sceneItem = new VRMenuController.SceneItem
        {
            sceneName = sceneName,
            displayName = displayName,
            itemPrefab = item
        };

        grabbable.Initialize(null, sceneItem);

        BoxCollider collider = item.GetComponent<BoxCollider>();
        collider.isTrigger = true;
    }

    void OnDrawGizmos()
    {
        if (enableTestMode && simulatedHandTransform != null)
        {
            // 绘制手柄位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(simulatedHandTransform.position, 0.3f);

            // 绘制目标位置
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(handTargetPosition, Vector3.one * 0.1f);

            // 绘制抓取范围
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(simulatedHandTransform.position, 0.3f);
        }
    }
}