# PortalGrab 迁移指南

## 📋 概述

`PortalGrab.cs` 脚本已被完全移除，其功能已被新的VR系统（`VRMenuController` + `VRGrabbable`）完全替代并超越。

## 🔄 迁移对照表

### PortalGrab 功能 → 新VR系统对应功能

| PortalGrab 功能 | 新VR系统实现 | 改进说明 |
|-----------------|--------------|----------|
| `OnSelected()` | `VRGrabbable.Grab()` | 更完整的状态管理 |
| `OnUnselected()` | `VRGrabbable.Release()` | 更平滑的返回动画 |
| `ReturnToOrigin()` | `SmoothReturnToOrigin()` | 使用DOTween，更流畅 |
| A键场景切换 | `HandleSceneSelection()` | 相同功能，更好集成 |
| UI面板显示 | 场景信息面板 + 预览图 | 更丰富的UI展示 |
| 物理返回 | 智能物理状态管理 | 避免物理抖动 |

## 🛠️ 迁移步骤

### 1. 移除PortalGrab组件
```bash
# 删除PortalGrab脚本文件
rm Assets/Scripts/PortalGrab.cs
```

### 2. 更新场景中的GameObject
如果你有使用PortalGrab的场景物品，需要：

1. **移除PortalGrab组件**
   - 在Inspector中移除PortalGrab组件
   - 保留Rigidbody和Collider组件

2. **添加VRGrabbable组件**
   ```csharp
   // 替换PortalGrab为VRGrabbable
   [RequireComponent(typeof(Rigidbody), typeof(Collider))]
   public class VRGrabbable : MonoBehaviour
   ```

### 3. 更新Unity事件绑定
如果你有使用Unity Events绑定PortalGrab的方法：

```csharp
// 旧的绑定方式
- OnSelected() → PortalGrab.OnSelected()
- OnUnselected() → PortalGrab.OnUnselected()

// 新的绑定方式
- OnSelected() → VRGrabbable.Grab()
- OnUnselected() → VRGrabbable.Release()
```

### 4. 场景配置更新

#### 旧方式（PortalGrab）
```csharp
// 直接在物品上配置
PortalGrab portalGrab = item.GetComponent<PortalGrab>();
portalGrab.sceneName = "PhotoScene";
portalGrab.sceneInfoPanel = infoPanel;
portalGrab.sceneNameText = nameText;
```

#### 新方式（VR系统）
```csharp
// 在VRMenuController中统一配置
var sceneItem = new VRMenuController.SceneItem
{
    sceneName = "PhotoScene",
    displayName = "摄影场景",
    previewSprite = previewImage,
    itemPrefab = itemPrefab
};
```

## 🎯 代码迁移示例

### 旧代码（PortalGrab）
```csharp
public class OldSceneItem : MonoBehaviour
{
    public string sceneName;
    public GameObject infoPanel;
    public TMP_Text nameText;

    private PortalGrab portalGrab;

    void Start()
    {
        portalGrab = GetComponent<PortalGrab>();
        portalGrab.sceneName = sceneName;
        portalGrab.sceneInfoPanel = infoPanel;
        portalGrab.sceneNameText = nameText;
    }
}
```

### 新代码（VR系统）
```csharp
public class NewSceneSetup : MonoBehaviour
{
    public VRMenuController menuController;
    public MainMenu.SceneItemConfig[] sceneConfigs;

    void Start()
    {
        // 配置场景物品
        menuController.sceneItems = ConvertToSceneItems(sceneConfigs);
    }

    private VRMenuController.SceneItem[] ConvertToSceneItems(MainMenu.SceneItemConfig[] configs)
    {
        // 转换配置格式
        // VRMenuController会自动处理所有逻辑
    }
}
```

## ⚙️ 配置参数迁移

### PortalGrab 参数 → VRMenuController 参数

| PortalGrab 参数 | VRMenuController 参数 | 默认值 |
|-----------------|----------------------|--------|
| `returnTime` | `returnToOriginDuration` | 0.8秒 |
| `returnCurve` | `returnCurve` | AnimationCurve.EaseInOut |
| `sceneName` | `SceneItem.sceneName` | - |
| `sceneInfoPanel` | `sceneInfoPanel` | - |
| `sceneNameText` | `sceneNameText` | - |

## 🎨 新增功能

新VR系统提供了PortalGrab没有的功能：

1. **悬浮动画** - 物品自动悬浮和旋转
2. **抓取高亮** - 抓取时视觉反馈
3. **预览图支持** - 16:9预览图显示
4. **DOTween动画** - 更流畅的动画效果
5. **音效支持** - 抓取、选择、返回音效
6. **可配置返回曲线** - 自定义动画节奏
7. **智能物理管理** - 避免物理抖动

## 🚀 性能改进

1. **更好的动画性能** - DOTween优化
2. **智能状态管理** - 避免重复计算
3. **内存优化** - 更好的对象生命周期管理
4. **帧率稳定** - 优化的Update循环

## 🔧 常见问题

### Q: 为什么要移除PortalGrab？
A: PortalGrab的功能完全被新VR系统覆盖和超越，保留它会造成：
- 代码重复和维护困难
- 两套抓取系统可能冲突
- 功能不一致的用户体验

### Q: 新系统兼容Meta Building Blocks吗？
A: 完全兼容！新VR系统专为Meta All-in-One SDK设计，可以：
- 使用Building Blocks的抓取预制件
- 与现有的交互系统协同工作
- 支持所有Quest3手柄功能

### Q: 如何处理已有的PortalGrab组件？
A: 有几种选择：
1. **自动迁移** - 使用VRMenuController的`SpawnSceneItems()`方法
2. **手动替换** - 移除PortalGrab，添加VRGrabbable
3. **保留作为备份** - 可以暂时保留，但建议迁移

### Q: 新系统支持哪些额外功能？
A: 新系统支持：
- 自定义返回动画曲线
- 音效集成
- 更丰富的UI反馈
- 可配置的悬浮效果
- 智能的物理状态管理

## 📝 迁移检查清单

- [ ] 删除PortalGrab.cs文件
- [ ] 移除所有GameObject上的PortalGrab组件
- [ ] 添加VRMenuController到主菜单场景
- [ ] 添加MainMenu组件并配置场景物品
- [ ] 更新Unity Events绑定（如果使用）
- [ ] 测试抓取和返回功能
- [ ] 验证场景切换正常工作
- [ ] 检查音效和视觉效果
- [ ] 在Quest3设备上测试

## 🎯 迁移收益

迁移到新VR系统后，你将获得：

1. **更好的用户体验** - 流畅的动画和丰富的反馈
2. **更强的功能性** - 预览图、音效、自定义动画
3. **更好的维护性** - 统一的代码架构
4. **更强的扩展性** - 易于添加新功能
5. **更好的性能** - 优化的动画和状态管理

---

## 🆘 需要帮助？

如果在迁移过程中遇到问题：

1. 检查`VR主菜单使用说明.md`文档
2. 使用`VRMenuTester`进行调试
3. 查看Unity Console的错误信息
4. 确认所有必需的组件都已正确配置

新的VR系统为你提供了更强大、更灵活的交互体验！🚀