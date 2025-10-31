# VR主菜单系统使用说明

## 概述
这个VR主菜单系统为Quest3平台设计，支持手柄抓取物品进行场景切换，集成了Meta All-in-One SDK Building Blocks和DOTween动画。

## 🎯 主要功能

### ✅ 已实现功能
- **手柄抓取交互**: 使用Quest3手柄远程抓取物品
- **场景信息显示**: 抓取物品时显示场景名称和预览图
- **A键场景切换**: 抓取物品后按A键进入对应场景
- **长按菜单返回**: 按住菜单键3秒返回主菜单
- **物品自动返回**: 松开抓取键后物品自动平滑返回初始位置
- **DOTween动画**: 流畅的抓取、返回和UI动画
- **悬浮效果**: 物品自动悬浮和旋转动画
- **视觉反馈**: 抓取时的高亮和缩放效果

## 📁 文件结构

```
Assets/Scripts/VR/
├── VRMenuController.cs      # 主菜单控制器
├── VRGrabbable.cs           # 可抓取物品组件
├── MainMenuConfig.cs        # 配置文件
└── VRMenuTester.cs          # 测试工具

Assets/Scripts/
└── MainMenu.cs              # 主菜单场景控制器
```

## 🛠️ 设置步骤

### 1. 主菜单场景设置

在MainMenu.unity场景中：

1. **创建主菜单控制器**
   - 创建空GameObject，命名为"MainMenuController"
   - 添加`MainMenu.cs`脚本
   - 添加`VRMenuController.cs`脚本

2. **设置VRMenuController组件**
   ```csharp
   // 在VRMenuController中配置以下字段：
   - Scene Items: 场景物品数组
   - Scene Info Panel: 场景信息面板 (Canvas)
   - Scene Name Text: 场景名称文本 (TMP_Text)
   - Preview Image: 预览图显示 (Image)
   - Background Image: 背景图 (Image)
   - Right Hand Anchor: 右手锚点 (Transform)
   - Left Hand Anchor: 左手锚点 (Transform)
   - Grab Animation Duration: 抓取动画时长 (默认0.3秒)
   - Preview Display Delay: 预览显示延迟 (默认0.5秒)
   - Preview Offset: 预览图偏移位置
   - Preview Scale: 预览时缩放比例
   - Return To Origin Duration: 物品返回动画时长 (默认0.8秒)
   - Return Curve: 返回动画曲线
   - Enable Floating On Return: 返回后是否启用悬浮效果
   ```

3. **设置MainMenu组件**
   ```csharp
   // 在MainMenu中配置以下字段：
   - Scene Items: 场景物品配置数组
   - VR Menu Controller: 关联VRMenuController
   - Right Hand Anchor: 右手锚点
   - Left Hand Anchor: 左手锚点
   ```

### 2. 场景物品配置

#### 方式一：在Inspector中配置
```csharp
// 配置Scene Item Config
- Scene Name: "PhotoScene"           // 场景文件名
- Display Name: "摄影场景"           // 显示名称
- Description: "场景描述信息"
- Preview Sprite: [拖入16:9预览图]    // TODO: 准备预览图
- Item Prefab: [拖入物品预制件]       // TODO: 准备物品模型
- Custom Spawn Point: 自定义生成点 (可选)
- Is Unlocked: true                  // 是否解锁
- Required Level: 0                  // 所需等级
```

#### 方式二：使用配置文件
```csharp
// 在MainMenuConfig.cs中修改
public static MainMenuConfig GetDefaultConfig()
{
    // 在这里配置你的场景
    config.defaultScenes = new SceneItemConfig[]
    {
        new SceneItemConfig
        {
            sceneName = "PhotoScene",
            displayName = "摄影场景",
            previewSprite = yourPreviewSprite,  // 替换为你的预览图
            itemPrefab = yourItemPrefab,        // 替换为你的物品预制件
            // ... 其他配置
        }
    };
}
```

### 3. UI面板设置

创建场景信息面板Canvas：

1. **创建Canvas**
   - GameObject > UI > Canvas
   - Render Mode: World Space
   - 位置: 设置在玩家前方合适位置

2. **添加UI组件**
   ```
   Canvas
   ├── Panel (背景)
   │   ├── Image (Background Image)
   │   └── Image (Preview Image)
   └── TextMeshPro - Text (Scene Name Text)
   ```

3. **设置VRMenuController引用**
   - 将面板拖到VRMenuController的Scene Info Panel字段
   - 将预览图拖到Preview Image字段
   - 将文本拖到Scene Name Text字段

## 🎨 预览图配置

### 16:9预览图规格
- **分辨率**: 建议1920x1080或1280x720
- **格式**: PNG或JPG
- **命名**: 建议使用场景名称，如"PhotoScene_Preview.png"

### 替换预览图的方法

#### 方法一：直接在Inspector中替换
1. 选择MainMenuController GameObject
2. 在MainMenu组件中找到Scene Items数组
3. 将新的Sprite拖入Preview Sprite字段

#### 方法二：通过代码替换
```csharp
// 获取MainMenu组件
MainMenu mainMenu = FindObjectOfType<MainMenu>();

// 加载新的预览图
Sprite newPreview = Resources.Load<Sprite>("Previews/PhotoScene_Preview");

// 更新预览图
mainMenu.UpdatePreviewSprite("PhotoScene", newPreview);
```

#### 方法三：修改配置文件
```csharp
// 在MainMenuConfig.cs中
var config = MainMenuConfigManager.GetConfig();

// 加载预览图
Sprite photoPreview = Resources.Load<Sprite>("Previews/PhotoScene_Preview");
config.defaultScenes[0].previewSprite = photoPreview;
```

## 🎮 手柄输入配置

### Quest3手柄按键映射
```csharp
右手手柄:
- Grab键 → 抓取物品到手上 (Meta Building Blocks自动处理)
- 松开Grab键 → 物品自动平滑返回初始位置
- A键 → 进入场景 (VRMenuController.HandleSceneSelection)
- 菜单键 → 长按3秒返回主菜单 (VRMenuController.HandleMenuReturn)

左手手柄:
- 菜单键 → 长按3秒返回主菜单
```

### 物品返回动画
- **自动触发**: 松开抓取键后自动执行
- **平滑动画**: 使用DOTween创建流畅的返回效果
- **可配置时长**: 默认0.8秒，可在VRMenuController中调整
- **自定义曲线**: 支持自定义AnimationCurve控制动画节奏
- **悬浮恢复**: 返回完成后自动重新启动悬浮效果

### 自定义按键映射
如果需要修改按键，在`VRMenuController.cs`的`HandleSceneSelection()`方法中：

```csharp
private void HandleSceneSelection()
{
    // 修改这里来使用不同的按键
    if (currentGrabbedItem != null && currentSceneItem != null)
    {
        // 替换A键为其他按键
        if (OVRInput.GetDown(OVRInput.RawButton.B) ||  // B键
            OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // 扳机
        {
            LoadScene(currentSceneItem);
        }
    }
}
```

## 🧪 测试功能

### 使用VRMenuTester进行测试

1. **启用测试模式**
   ```csharp
   // 在VRMenuTester组件中
   - Enable Test Mode: true
   - 添加Debug UI显示
   ```

2. **键盘测试按键**
   ```
   WASD/QE - 移动模拟手柄
   G - 抓取物品
   R - 松开物品
   Space - 选择场景
   Esc - 返回主菜单
   ```

3. **创建测试物品**
   - 右键VRMenuTester组件 > "Create Test Scene Items"
   - 自动创建简单的测试物品

## 🎯 与Building Blocks集成

### 使用Meta Building Blocks抓取系统

项目已集成Meta All-in-One SDK的Building Blocks，可以使用以下预制件：

1. **抓取预制件**
   - `[BB] Grabbable Cube.prefab` - 基础可抓取物品
   - `GrabInteractable.prefab` - 抓取交互器

2. **UI预制件**
   - `Button/PrimaryButton_IconAndLabel.prefab` - 按钮
   - `Slider/SmallSlider.prefab` - 滑块

### 集成方式
`VRGrabbable.cs`组件与Building Blocks兼容，可以：
- 与现有的PortalGrab脚本配合使用
- 替换为Building Blocks的抓取系统
- 混合使用多种交互方式

## 🔧 常见问题解决

### 1. 物品无法抓取
- 检查物品是否有Collider组件
- 确认物品在"Grabbable"层级
- 检查VRMenuController是否正确初始化

### 2. 场景切换失败
- 确认场景名称正确
- 检查场景是否在Build Settings中
- 查看Console错误信息

### 3. UI面板不显示
- 检查Canvas设置是否为World Space
- 确认UI组件引用正确
- 检查面板是否被激活

### 4. 手柄输入无响应
- 确认Meta XR SDK正确配置
- 检查OVRInput是否初始化
- 查看手柄连接状态

## 📝 开发注意事项

1. **性能优化**
   - 使用对象池管理频繁创建的UI元素
   - 避免在Update中进行复杂的计算
   - 合理设置DOTween动画的缓动函数

2. **VR舒适度**
   - 抓取距离不要太远
   - UI面板大小适中
   - 动画速度不要太快

3. **扩展性**
   - 使用配置文件管理场景物品
   - 支持动态添加/删除场景
   - 预留自定义交互接口

## 🎨 视觉效果配置

### 抓取动画参数
```csharp
// 在VRMenuController中调整
- Grab Animation Duration: 0.3f (抓取动画时长)
- Preview Display Delay: 0.5f (预览显示延迟)
- Preview Offset: (0, 0.1, 0.2) (预览偏移)
- Preview Scale: 0.8f (预览缩放)
```

### 返回动画参数
```csharp
// 在VRMenuController中调整
- Return To Origin Duration: 0.8f (物品返回动画时长)
- Return Curve: AnimationCurve (返回动画曲线，默认EaseInOut)
- Enable Floating On Return: true (返回后是否重新启动悬浮效果)
```

### 返回菜单参数
```csharp
// 在VRMenuController中调整
- Menu Hold Duration: 3f (菜单按键持续时间)
```

## 🚀 部署到Quest3

1. **构建设置**
   - Target Platform: Android
   - Texture Compression: ASTC
   - Scripting Backend: IL2CPP

2. **Quest3特定设置**
   - 在ProjectSettings中启用Quest3支持
   - 配置XR Plug-in Management
   - 设置适当的图形质量

3. **测试**
   - 使用Quest Link进行无线测试
   - 验证手柄输入和UI显示
   - 检查性能和舒适度

---

## 📞 支持

如有问题或需要帮助，请：
1. 检查Unity Console的错误信息
2. 确认所有必需的组件都已正确配置
3. 使用VRMenuTester进行调试

这个主菜单系统已经为你提供了完整的基础框架，只需要准备预览图和物品模型即可使用！