using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 主菜单场景控制器
/// 配合VRMenuController使用
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("场景物品配置")]
    public SceneItemConfig[] sceneItems;

    [Header("UI组件")]
    public VRMenuController vrMenuController;
    public Transform rightHandAnchor;
    public Transform leftHandAnchor;

    [Header("默认设置")]
    public string defaultSceneName = "PhotoScene";
    public Vector3 defaultSpawnArea = new Vector3(0, 1, 2);
    public float spawnSpacing = 1.5f;

    [System.Serializable]
    public class SceneItemConfig
    {
        public string sceneName;
        public string displayName;
        public string description;
        public Sprite previewSprite; // 16:9预览图，后续可替换
        public GameObject itemPrefab;
        public Transform customSpawnPoint;
        public bool isUnlocked = true;
        public int requiredLevel = 0;
    }

    void Start()
    {
        InitializeMainMenu();
    }

    /// <summary>
    /// 初始化主菜单
    /// </summary>
    private void InitializeMainMenu()
    {
        // 等待VR系统初始化
        StartCoroutine(WaitForVRAndInitialize());
    }

    /// <summary>
    /// 等待VR系统初始化后设置菜单
    /// </summary>
    private IEnumerator WaitForVRAndInitialize()
    {
        // 等待几帧确保VR系统初始化完成
        yield return new WaitForSeconds(1f);

        // 设置手柄锚点
        SetupHandAnchors();

        // 配置VRMenuController
        SetupVRMenuController();

        Debug.Log("Main Menu initialized successfully");
    }

    /// <summary>
    /// 设置手柄锚点
    /// </summary>
    private void SetupHandAnchors()
    {
        // 如果没有设置手柄锚点，尝试自动找到
        if (rightHandAnchor == null || leftHandAnchor == null)
        {
            GameObject centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
            if (centerEyeAnchor != null)
            {
                Transform parent = centerEyeAnchor.transform.parent;

                if (rightHandAnchor == null)
                {
                    Transform rightHand = parent?.Find("RightHandAnchor");
                    if (rightHand != null) rightHandAnchor = rightHand;
                }

                if (leftHandAnchor == null)
                {
                    Transform leftHand = parent?.Find("LeftHandAnchor");
                    if (leftHand != null) leftHandAnchor = leftHand;
                }
            }
        }

        // 设置VRMenuController的手柄锚点
        if (vrMenuController != null)
        {
            vrMenuController.SetHandAnchors(rightHandAnchor, leftHandAnchor);
        }
    }

    /// <summary>
    /// 设置VRMenuController
    /// </summary>
    private void SetupVRMenuController()
    {
        if (vrMenuController == null)
        {
            vrMenuController = FindObjectOfType<VRMenuController>();
        }

        if (vrMenuController != null)
        {
            // 转换SceneItemConfig到VRMenuController.SceneItem
            List<VRMenuController.SceneItem> menuItems = new List<VRMenuController.SceneItem>();

            for (int i = 0; i < sceneItems.Length; i++)
            {
                var config = sceneItems[i];
                if (!config.isUnlocked) continue;

                VRMenuController.SceneItem item = new VRMenuController.SceneItem
                {
                    sceneName = config.sceneName,
                    displayName = config.displayName,
                    previewSprite = config.previewSprite,
                    itemPrefab = config.itemPrefab,
                    spawnPoint = config.customSpawnPoint
                };

                // 如果没有自定义生成点，使用默认排列
                if (item.spawnPoint == null)
                {
                    GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
                    spawnPoint.transform.SetParent(transform);

                    // 圆形排列
                    float angle = (float)i / sceneItems.Length * 2f * Mathf.PI;
                    Vector3 position = new Vector3(
                        Mathf.Cos(angle) * spawnSpacing,
                        defaultSpawnArea.y,
                        Mathf.Sin(angle) * spawnSpacing + defaultSpawnArea.z
                    );

                    spawnPoint.transform.position = position;
                    spawnPoint.transform.LookAt(Vector3.zero);
                    item.spawnPoint = spawnPoint.transform;
                }

                menuItems.Add(item);
            }

            // 设置场景物品
            vrMenuController.sceneItems = menuItems.ToArray();

            Debug.Log($"VRMenuController configured with {menuItems.Count} scene items");
        }
        else
        {
            Debug.LogError("VRMenuController not found!");
        }
    }

    /// <summary>
    /// 更新场景物品配置
    /// </summary>
    public void UpdateSceneItems()
    {
        if (vrMenuController != null)
        {
            SetupVRMenuController();
        }
    }

    /// <summary>
    /// 添加新场景物品
    /// </summary>
    public void AddSceneItem(SceneItemConfig newItem)
    {
        var newList = new List<SceneItemConfig>(sceneItems);
        newList.Add(newItem);
        sceneItems = newList.ToArray();

        UpdateSceneItems();
    }

    /// <summary>
    /// 移除场景物品
    /// </summary>
    public void RemoveSceneItem(string sceneName)
    {
        var newList = new List<SceneItemConfig>();
        foreach (var item in sceneItems)
        {
            if (item.sceneName != sceneName)
            {
                newList.Add(item);
            }
        }
        sceneItems = newList.ToArray();

        UpdateSceneItems();
    }

    /// <summary>
    /// 获取场景物品配置
    /// </summary>
    public SceneItemConfig GetSceneItemConfig(string sceneName)
    {
        foreach (var item in sceneItems)
        {
            if (item.sceneName == sceneName)
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// 解锁场景
    /// </summary>
    public void UnlockScene(string sceneName)
    {
        for (int i = 0; i < sceneItems.Length; i++)
        {
            if (sceneItems[i].sceneName == sceneName)
            {
                sceneItems[i].isUnlocked = true;
                UpdateSceneItems();
                break;
            }
        }
    }

    /// <summary>
    /// 更新预览图
    /// </summary>
    public void UpdatePreviewSprite(string sceneName, Sprite newSprite)
    {
        for (int i = 0; i < sceneItems.Length; i++)
        {
            if (sceneItems[i].sceneName == sceneName)
            {
                sceneItems[i].previewSprite = newSprite;
                UpdateSceneItems();
                break;
            }
        }
    }

    void OnValidate()
    {
        // 确保场景名称不为空
        foreach (var item in sceneItems)
        {
            if (string.IsNullOrEmpty(item.sceneName))
            {
                item.sceneName = defaultSceneName;
            }

            if (string.IsNullOrEmpty(item.displayName))
            {
                item.displayName = item.sceneName;
            }
        }
    }

    void OnDrawGizmos()
    {
        // 在Scene视图中绘制生成点
        if (sceneItems != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < sceneItems.Length; i++)
            {
                var item = sceneItems[i];
                Vector3 position;

                if (item.customSpawnPoint != null)
                {
                    position = item.customSpawnPoint.position;
                }
                else
                {
                    // 默认圆形排列
                    float angle = (float)i / sceneItems.Length * 2f * Mathf.PI;
                    position = new Vector3(
                        Mathf.Cos(angle) * spawnSpacing,
                        defaultSpawnArea.y,
                        Mathf.Sin(angle) * spawnSpacing + defaultSpawnArea.z
                    );
                }

                Gizmos.DrawWireSphere(position, 0.2f);
                Gizmos.DrawWireCube(position + Vector3.up * 0.5f, Vector3.one * 0.3f);

                #if UNITY_EDITOR
                UnityEditor.Handles.Label(position + Vector3.up * 0.8f, item.displayName);
                #endif
            }
        }
    }
}
