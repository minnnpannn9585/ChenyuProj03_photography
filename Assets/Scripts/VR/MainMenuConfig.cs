using UnityEngine;

/// <summary>
/// 主菜单配置文件
/// 用于存储和管理场景物品配置
/// 可以在这里修改预览图和其他设置
/// </summary>
[System.Serializable]
public class MainMenuConfig
{
    [Header("默认场景配置")]
    public SceneItemConfig[] defaultScenes;

    [Header("UI设置")]
    public float grabAnimationDuration = 0.3f;
    public float previewDisplayDelay = 0.5f;
    public Vector3 previewOffset = new Vector3(0, 0.1f, 0.2f);
    public float previewScale = 0.8f;

    [Header("菜单返回设置")]
    public float menuHoldDuration = 3f;

    [System.Serializable]
    public class SceneItemConfig
    {
        [Header("基础信息")]
        public string sceneName;
        public string displayName;
        public string description;

        [Header("预览设置")]
        public Sprite previewSprite; // 16:9预览图 - 在这里替换你的图片
        public GameObject itemPrefab; // 代表该场景的3D物品

        [Header("位置设置")]
        public Vector3 customPosition = Vector3.zero;
        public Vector3 customRotation = Vector3.zero;
        public bool useCustomPosition = false;

        [Header("解锁设置")]
        public bool isUnlocked = true;
        public int requiredLevel = 0;
    }

    /// <summary>
    /// 获取默认配置
    /// 包含摄影场景和博物馆场景的配置
    /// </summary>
    public static MainMenuConfig GetDefaultConfig()
    {
        MainMenuConfig config = new MainMenuConfig();

        config.defaultScenes = new SceneItemConfig[]
        {
            new SceneItemConfig
            {
                sceneName = "PhotoScene",
                displayName = "摄影场景",
                description = "进入虚拟摄影棚，体验专业相机模拟",
                previewSprite = null, // TODO: 替换为16:9预览图
                itemPrefab = null, // TODO: 替换为相机模型预制件
                useCustomPosition = false,
                isUnlocked = true,
                requiredLevel = 0
            },
            new SceneItemConfig
            {
                sceneName = "Museum",
                displayName = "照片展览馆",
                description = "浏览你拍摄的所有精彩照片",
                previewSprite = null, // TODO: 替换为16:9预览图
                itemPrefab = null, // TODO: 替换为相框模型预制件
                useCustomPosition = false,
                isUnlocked = true,
                requiredLevel = 0
            }
        };

        return config;
    }
}

/// <summary>
/// 主菜单配置管理器
/// 提供便捷的配置修改方法
/// </summary>
public static class MainMenuConfigManager
{
    private static MainMenuConfig config;

    /// <summary>
    /// 获取配置
    /// </summary>
    public static MainMenuConfig GetConfig()
    {
        if (config == null)
        {
            config = MainMenuConfig.GetDefaultConfig();
        }
        return config;
    }

    /// <summary>
    /// 更新场景预览图
    /// </summary>
    public static void UpdateScenePreview(string sceneName, Sprite previewSprite)
    {
        var cfg = GetConfig();
        for (int i = 0; i < cfg.defaultScenes.Length; i++)
        {
            if (cfg.defaultScenes[i].sceneName == sceneName)
            {
                cfg.defaultScenes[i].previewSprite = previewSprite;
                break;
            }
        }
    }

    /// <summary>
    /// 更新场景物品预制件
    /// </summary>
    public static void UpdateScenePrefab(string sceneName, GameObject prefab)
    {
        var cfg = GetConfig();
        for (int i = 0; i < cfg.defaultScenes.Length; i++)
        {
            if (cfg.defaultScenes[i].sceneName == sceneName)
            {
                cfg.defaultScenes[i].itemPrefab = prefab;
                break;
            }
        }
    }

    /// <summary>
    /// 添加新场景
    /// </summary>
    public static void AddScene(MainMenuConfig.SceneItemConfig newScene)
    {
        var cfg = GetConfig();
        var newList = new System.Collections.Generic.List<MainMenuConfig.SceneItemConfig>(cfg.defaultScenes);
        newList.Add(newScene);
        cfg.defaultScenes = newList.ToArray();
    }

    /// <summary>
    /// 解锁场景
    /// </summary>
    public static void UnlockScene(string sceneName)
    {
        var cfg = GetConfig();
        for (int i = 0; i < cfg.defaultScenes.Length; i++)
        {
            if (cfg.defaultScenes[i].sceneName == sceneName)
            {
                cfg.defaultScenes[i].isUnlocked = true;
                break;
            }
        }
    }

    /// <summary>
    /// 设置场景自定义位置
    /// </summary>
    public static void SetScenePosition(string sceneName, Vector3 position, Vector3 rotation)
    {
        var cfg = GetConfig();
        for (int i = 0; i < cfg.defaultScenes.Length; i++)
        {
            if (cfg.defaultScenes[i].sceneName == sceneName)
            {
                cfg.defaultScenes[i].customPosition = position;
                cfg.defaultScenes[i].customRotation = rotation;
                cfg.defaultScenes[i].useCustomPosition = true;
                break;
            }
        }
    }
}