# VR摄影项目场景配置详细指南

## 📋 项目概述

本指南详细说明Unity VR摄影项目三个主要场景的完整配置步骤，包括所需的组件、素材和具体配置参数。

**项目结构**:
- `MainMenu.unity` - 主菜单场景
- `PhotoScene.unity` - 拍照场景
- `Museum.unity` - 照片浏览场景

---

## 🎮 1. MainMenu场景配置指南

### 1.1 场景基础设置

#### 步骤1：创建基础场景结构
```
MainMenu (空GameObject)
├── VRMenuController (脚本组件)
├── SceneItems (空GameObject)
│   ├── PhotoSceneItem (相机模型)
│   └── MuseumSceneItem (相框模型)
├── UI_Canvas (Canvas)
│   ├── WelcomePanel
│   ├── SceneInfoPanel
│   └── MenuFeedbackPanel
└── Environment (灯光和环境)
```

#### 步骤2：添加OVRCameraRig
1. 在Hierarchy中右键 → XR → XR Origin (VR)
2. 重命名为 "OVRCameraRig"
3. 确保包含以下子对象：
   - Camera Offset
   - LeftHandAnchor
   - RightHandAnchor
   - CenterEyeAnchor

### 1.2 VRMenuController配置

#### 组件配置参数
```csharp
// 在VRMenuController组件中设置：
sceneItems: [PhotoSceneItem, MuseumSceneItem]
grabDistance: 2.0f
grabAnimationDuration: 0.3f
returnAnimationDuration: 0.5f
menuHoldDuration: 3.0f
infoDisplayDistance: 0.5f
previewScale: 0.3f
```

#### 具体配置步骤
1. 创建空GameObject，命名为 "VRMenuManager"
2. 添加 `VRMenuController.cs` 脚本
3. 在Inspector中配置参数：
   - **Scene Items数组**: 拖入两个场景选择物品
   - **Left Hand Anchor**: 拖入OVRCameraRig/LeftHandAnchor
   - **Right Hand Anchor**: 拖入OVRCameraRig/RightHandAnchor
   - **Head Transform**: 拖入OVRCameraRig/Camera Offset/CenterEyeAnchor

### 1.3 场景选择物品配置

#### PhotoSceneItem配置
1. 创建Cube，重命名为 "PhotoSceneItem"
2. 添加以下组件：
   ```csharp
   // Transform
   Position: (-1.5, 1.2, 2)
   Rotation: (0, 0, 0)
   Scale: (0.2, 0.15, 0.05)

   // Collider
   Box Collider (Is Trigger = true)

   // Rigidbody
   Rigidbody (Use Gravity = false, Is Kinematic = true)

   // VRGrabbable脚本
   VRGrabbable.cs
   ```

3. VRGrabbable组件参数：
   ```csharp
   hoverScale: 1.1f
   hoverDuration: 0.2f
   returnToOriginal: true
   grabSound: [拖入抓取音效]
   releaseSound: [拖入释放音效]
   ```

4. 添加MainMenuConfig组件：
   ```csharp
   displayName: "摄影场景"
   sceneName: "PhotoScene"
   previewImage: [拖入预览图片]
   isUnlocked: true
   description: "进入VR摄影场景"
   ```

#### MuseumSceneItem配置
1. 创建Cube，重命名为 "MuseumSceneItem"
2. 设置位置和组件（同PhotoSceneItem，位置为(1.5, 1.2, 2)）
3. MainMenuConfig组件：
   ```csharp
   displayName: "照片展览"
   sceneName: "Museum"
   previewImage: [拖入预览图片]
   isUnlocked: true
   description: "浏览拍摄的照片"
   ```

### 1.4 UI Canvas配置

#### 创建InfoCanvas
1. 创建Canvas，命名为 "InfoCanvas"
2. Canvas设置：
   ```csharp
   Render Mode: Screen Space - Camera
   Render Camera: [拖入CenterEyeAnchor]
   Plane Distance: 1.0
   ```

3. 添加Canvas Scaler：
   ```csharp
   UI Scale Mode: Scale With Screen Size
   Reference Resolution: 1920 x 1080
   Screen Match Mode: Match Width Or Height
   Match: 0.5
   ```

#### UI面板配置
**WelcomePanel**:
```csharp
// RectTransform
Anchor: Center
Position: (0, 0, 0)
Size: (800, 450)

// 组件
Canvas Group (用于淡入淡出)
Image (背景图片)
TextMeshPro - Text (欢迎文本)
```

**SceneInfoPanel**:
```csharp
// RectTransform
Anchor: Center
Position: (0, 0.3, 0)
Size: (600, 200)

// 组件
Image (半透明背景)
TextMeshPro - Text (场景信息)
Image (预览图片)
```

### 1.5 所需素材清单

#### 3D模型
- [ ] **相机模型** (FBX格式，用于PhotoSceneItem)
- [ ] **相框模型** (FBX格式，用于MuseumSceneItem)

#### UI纹理
- [ ] **场景预览图** (512x512像素，PNG格式)
  - PhotoScene_Preview.png (相机图标)
  - Museum_Preview.png (画廊图标)
- [ ] **背景纹理** (2048x2048像素，渐变背景)
- [ ] **UI图标** (256x256像素，PNG格式)
  - Grab_Icon.png (抓取图标)
  - Select_Icon.png (选择图标)

#### 音效文件
- [ ] **Grab_Sound.wav** (抓取音效，0.2秒)
- [ ] **Release_Sound.wav** (释放音效，0.2秒)
- [ ] **Select_Sound.wav** (选择音效，0.3秒)
- [ ] **Menu_Back_Sound.wav** (菜单返回音效，0.5秒)
- [ ] **Welcome_Music.mp3** (背景音乐，循环播放)

---

## 📸 2. PhotoScene场景配置指南

### 2.1 场景基础设置

#### 步骤1：导入CameraModule预制件
1. 从Assets/Prefabs/拖入CameraModule.prefab
2. 确保预制件包含以下结构：
   ```
   CameraModule
   ├── WorldSpaceCanvas
   │   ├── ParameterSliders (5个滑块)
   │   ├── PreviewPanel
   │   └── ControlButtons
   ├── PhotoCamera (物理相机)
   ├── Volume (后处理)
   └── Lighting
   ```

#### 步骤2：添加OVRCameraRig和VR系统
1. 添加XR Origin (VR)
2. 确保手柄锚点正确设置

### 2.2 VRCameraController配置

#### 添加组件到CameraModule
1. 选择CameraModule GameObject
2. 添加以下组件：
   ```csharp
   VRCameraController.cs
   VRCustomExposureController.cs
   VRCameraAdapter.cs
   VRCameraModel.cs
   ```

#### VRCameraController参数配置
```csharp
// 相机设置
photographyCamera: [拖入PhotoCamera]
previewUI: [拖入PreviewPanel中的RawImage]
captureWidth: 1920
captureHeight: 1080
folderName: "CapturedPhotos"

// 手柄设置
rightHandAnchor: [拖入RightHandAnchor]
leftHandAnchor: [拖入LeftHandAnchor]

// 控制设置
parameterChangeSpeed: 50f
focusControlSensitivity: 0.1f
zoomControlSensitivity: 5f

// 音效设置
captureSound: [拖入快门音效]
focusSound: [拖入对焦音效]
```

#### VRCameraModel配置
```csharp
// 相机组件
cameraModel: [拖入相机3D模型]
lensTransform: [拖入镜头Transform]
focusRingTransform: [拖入对焦环Transform]
apertureRingTransform: [拖入光圈环Transform]
shutterButtonTransform: [拖入快门按钮Transform]

// 物理设置
weight: 0.8f
holdDistance: 0.3f
holdRotation: (0, 180, 0)
```

### 2.3 相机3D模型配置

#### 创建或导入相机模型
1. **选项A：使用简单几何体**
   ```csharp
   // 创建相机主体
   Cube (Scale: 0.15, 0.1, 0.08)
   // 创建镜头部分
   Cylinder (Scale: 0.08, 0.03, 0.08)
   // 创建取景器
   Cube (Scale: 0.12, 0.02, 0.1)
   ```

2. **选项B：导入专业相机模型**
   - 下载或创建高质量相机FBX模型
   - 确保模型包含可交互部件
   - 设置正确的材质和纹理

#### 相机模型材质设置
```csharp
// 主体材质
Material: Standard
Albedo: 深灰色 (0.2, 0.2, 0.2)
Metallic: 0.8
Smoothness: 0.7

// 镜头材质
Material: Standard
Albedo: 深蓝色 (0.1, 0.1, 0.3)
Metallic: 0.9
Smoothness: 0.9
```

### 2.4 UI Canvas配置

#### WorldSpaceCanvas设置
```csharp
// Canvas设置
Render Mode: World Space
Canvas Renderer: [自动创建]

// RectTransform设置
Position: (0, 0.1, 0.2)
Rotation: (0, 0, 0)
Scale: (0.001, 0.001, 0.001)

// 添加组件
Canvas Group (用于透明度控制)
```

#### 参数滑块配置
为每个参数创建滑块：
```csharp
// ISO滑块
Slider ISO_Slider
├── Fill Area (ISO填充指示)
├── Handle (ISO滑块手柄)
├── Label (ISO文本标签)
└── VRSliderInteraction (VR交互组件)

// 参数设置
ISO_Slider.minValue: 100
ISO_Slider.maxValue: 3200
ISO_Slider.value: 400

// 其他滑块类似设置
Aperture_Slider: f/1.4 - f/16
Shutter_Slider: 1/1000 - 1/30
FocalLength_Slider: 24mm - 200mm
FocusDistance_Slider: 0.5m - 10m
```

### 2.5 物理相机和后处理配置

#### Camera设置
```csharp
// PhotoCamera组件设置
Render Mode: Base Camera
Rendering Path: Forward
Clear Flags: Skybox
Field of View: 60

// Physical Camera设置
Use Physical Properties: true
Sensor Size: (36, 24)  // 全画幅
Gate Fit: Horizontal
Focal Length: 50
Focus Distance: 10
Aperture: 5.6
Shutter Speed: 60
ISO: 400
```

#### Volume后处理配置
```csharp
// Volume组件设置
Profile: [创建或使用现有Volume Profile]
Mode: Global
Blend Distance: 0

// Volume Profile中添加的特效
1. Color Adjustments (曝光补偿)
2. Depth of Field (景深效果)
3. Motion Blur (动态模糊)
4. Vignette (暗角效果)
```

### 2.6 所需素材清单

#### 3D模型
- [ ] **专业相机模型** (FBX格式，包含镜头、快门、模式转盘等可交互部件)
- [ ] **环境道具** (可选，用于拍摄场景装饰)

#### UI纹理
- [ ] **参数图标** (64x64像素，PNG格式)
  - ISO_Icon.png
  - Aperture_Icon.png
  - Shutter_Icon.png
  - Focus_Icon.png
  - Zoom_Icon.png
- [ ] **UI背景** (1024x1024像素，半透明黑色)
- [ ] **滑块纹理** (128x16像素，渐变效果)

#### 音效文件
- [ ] **Shutter_Click.wav** (快门声，0.5秒)
- [ ] **Focus_Beep.wav** (对焦提示音，0.1秒)
- [ ] **Parameter_Change.wav** (参数调节音效，0.2秒)
- [ ] **Mode_Switch.wav** (模式切换音效，0.3秒)

#### 环境素材
- [ ] **Skybox纹理** (2048x2048像素，HDR格式)
- [ ] **环境反射贴图** (立方体贴图)
- [ ] **光照贴图** (可选，用于静态场景)

---

## 🖼️ 3. Museum场景配置指南

### 3.1 场景基础设置

#### 步骤1：导入艺术画廊环境
1. 从Assets/AK Studio Art/导入Gallery预制件
2. 确保包含：
   ```csharp
   Gallery_Building (画廊建筑)
   PictureFrames (相框集合)
   Lighting_Setup (灯光系统)
   Decorations (装饰品)
   ```

#### 步骤2：添加VR系统
1. 添加XR Origin (VR)
2. 设置玩家起始位置：
   ```csharp
   Position: (0, 0, 5)
   Rotation: (0, 180, 0)  // 面向画廊
   ```

### 3.2 VRMuseumController配置

#### 创建MuseumManager
1. 创建空GameObject，命名为 "MuseumManager"
2. 添加VRMuseumController.cs脚本
3. 配置参数：
   ```csharp
   // 核心组件
   locomotionController: [将在VRLocomotionController步骤设置]
   mainCamera: [拖入CenterEyeAnchor]
   playerTransform: [拖入XR Origin]

   // 照片显示
   photoDisplays: [自动查找或手动拖入15个相框]
   autoConfigurePhotoDisplays: true
   enablePhotoInteractions: true

   // UI界面
   infoCanvas: [拖入或创建InfoCanvas]
   welcomePanel: [在InfoCanvas下创建]
   statsPanel: [在InfoCanvas下创建]

   // 控制设置
   enableLocomotionSwitch: true
   switchLocomotionKey: L
   showWelcomeMessage: true
   welcomeDuration: 3f

   // 场景设置
   autoRotate: false
   rotationSpeed: 10f
   spawnPosition: (0, 0, 5f)
   ```

### 3.3 VRLocomotionController配置

#### 添加到XR Origin
1. 选择XR Origin GameObject
2. 添加VRLocomotionController.cs脚本
3. 配置参数：
   ```csharp
   // 移动设置
   locomotionType: Hybrid
   moveSpeed: 3f
   rotationSpeed: 60f
   enableStrafe: true

   // 平滑移动
   acceleration: 10f
   friction: 8f
   gravity: -20f
   groundLayer: Default

   // 瞬移设置
   teleportMaxDistance: 10f
   teleportMinDistance: 1f
   teleportArcHeight: 2f
   showTeleportArc: true

   // 手柄设置
   leftHandAnchor: [拖入LeftHandAnchor]
   rightHandAnchor: [拖入RightHandAnchor]
   headTransform: [拖入CenterEyeAnchor]

   // 菜单返回
   menuHoldDuration: 3f
   menuFeedbackPanel: [创建UI面板]
   ```

### 3.4 相框和VRPhotoDisplay配置

#### 为每个相框添加VRPhotoDisplay
1. 选择所有PictureFrame子对象
2. 添加VRPhotoDisplay.cs脚本
3. 配置每个相框：
   ```csharp
   // 基本设置
   folderName: "CapturedPhotos"
   switchInterval: 8f
   fadeDuration: 1f
   enableRandomOrder: true

   // VR增强设置
   enableVRInteraction: true
   showPhotoInfo: true
   enableHoverEffects: true
   enable3DFrameEffects: true

   // 交互设置
   hoverScale: 1.05f
   hoverDuration: 0.3f
   hoverColor: Yellow
   normalColor: White

   // 灯光效果
   frameLight: [添加或查找Point Light]
   lightIntensity: 2f
   lightColor: White
   enableLightPulse: true
   ```

#### 相框位置和布局
```csharp
// 推荐的相框布局
Frame_01: Position(-4, 1.6, 2), Rotation(0, 180, 0), Scale(1.2, 0.8, 0.1)
Frame_02: Position(-2, 1.6, 2), Rotation(0, 180, 0), Scale(1.2, 0.8, 0.1)
Frame_03: Position(0, 1.6, 2), Rotation(0, 180, 0), Scale(1.2, 0.8, 0.1)
// ... 继续布局其他12个相框

// 建议布局方式
- 3行 x 5列的网格布局
- 行间距: 1.2m
- 列间距: 2m
- 高度: 1.6m (眼睛高度)
```

### 3.5 UI界面配置

#### 创建InfoCanvas
1. 创建Canvas，设置为World Space
2. 配置Canvas：
   ```csharp
   Render Mode: World Space
   Position: (0, 2, 0)
   Scale: (0.001, 0.001, 0.001)
   ```

#### WelcomePanel配置
```csharp
// GameObject结构
InfoCanvas
├── WelcomePanel
│   ├── Background (Image)
│   ├── TitleText (TextMeshPro)
│   └── ContentText (TextMeshPro)

// WelcomePanel参数
Position: (0, 200, 0)
Size: (600, 300)

// TitleText设置
Text: "欢迎来到虚拟照片展览馆"
Font Size: 32
Alignment: Center
Color: White

// ContentText设置
Text: "使用手柄移动并浏览您的摄影作品"
Font Size: 24
Alignment: Center
Color: Light Gray
```

#### StatsPanel配置
```csharp
// StatsPanel结构
StatsPanel
├── PhotoCountText
├── VisitTimeText
└── MovementDistanceText

// 参数设置
Position: (-300, -150, 0)
Size: (250, 150)

// PhotoCountText
Text: "照片数量: 0"
Font Size: 18
Alignment: Left
Color: White
```

### 3.6 灯光和环境配置

#### 主灯光设置
```csharp
// Directional Light (主光源)
Position: (10, 20, 10)
Rotation: (45, 135, 0)
Intensity: 1.0
Color: White (5500K)
Shadow Type: Soft Shadows

// 环境光设置
Ambient Mode: Flat
Ambient Color: (0.2, 0.2, 0.2, 1.0)
```

#### 相框照明
```csharp
// 为每个相框添加Point Light
Intensity: 2.0f
Range: 3f
Color: Warm White (6000K)
Shadow Type: No Shadows
Culling Mask: Only PictureFrames
```

#### 反射和材质
```csharp
// 地面材质
Material: Standard
Albedo: Dark Gray (0.1, 0.1, 0.1)
Metallic: 0.8
Smoothness: 0.9

// 墙面材质
Material: Standard
Albedo: Light Gray (0.8, 0.8, 0.8)
Metallic: 0.0
Smoothness: 0.3
```

### 3.7 所需素材清单

#### 3D模型
- [ ] **15个相框模型** (FBX格式，不同尺寸和样式)
- [ ] **装饰品** (雕塑、花瓶、展示柜等)
- [ ] **建筑细节** (门窗、柱子、天花板等)

#### 照片内容
- [ ] **示例照片** (至少15张，1920x1080像素，JPG格式)
  - Landscape_01.jpg 到 Landscape_15.jpg
  - 确保照片内容适合画廊展示
- [ ] **照片元数据** (可选，包含拍摄信息)

#### UI纹理
- [ ] **欢迎界面背景** (1024x1024像素，PNG格式)
- [ ] **统计图标** (64x64像素，PNG格式)
  - Photo_Icon.png
  - Time_Icon.png
  - Distance_Icon.png
- [ ] **UI边框和装饰** (512x512像素，PNG格式)

#### 音效文件
- [ ] **Welcome_Music.wav** (欢迎音乐，30秒循环)
- [ ] **Photo_View_Sound.wav** (照片查看音效，0.3秒)
- [ ] **Footstep_Concrete.wav** (脚步声，0.2秒)
- [ ] **Teleport_Sound.wav** (瞬移音效，0.4秒)
- [ ] **Menu_Hold_Sound.wav** (菜单键长按音效，0.1秒循环)

#### 环境素材
- [ ] **Skybox纹理** (2048x2048像素，HDR格式，画廊天空)
- [ ] **环境反射贴图** (立方体贴图，室内反射)
- [ ] **光照贴图** (如果使用静态光照)

---

## 🔧 4. 通用VR配置

### 4.1 XR Plugin Management设置

#### 打开Project Settings
1. Edit → Project Settings → XR Plug-in Management
2. **Provider选项卡**：
   ```csharp
   // 安装的插件
   ✓ Meta XR (All in One) - 78.0.0
   ✓ OpenXR Plugin
   ✓ XR Interaction Toolkit - 2.6.5
   ```

3. **Provider for Android**：
   ```csharp
   ✓ Meta XR
   ✓ OpenXR
   ```

### 4.2 OpenXR设置

#### 配置OpenXR
1. Project Settings → XR Plug-in Management → OpenXR
2. **交互配置**：
   ```csharp
   // Controller Model
   ✓ Meta Quest Touch Pro Controller

   // Runtime Features
   ✓ Hand Tracking
   ✓ Passthrough
   ✓ Controller Haptics
   ```

### 4.3 Quality Settings优化

#### 针对Quest3的质量设置
1. Edit → Project Settings → Quality
2. **Android质量级别**：
   ```csharp
   // 当前选择：Medium
   Pixel Light Count: 0
   Shadows: Hard Only
   Shadow Resolution: Medium
   Shadow Distance: 20
   Texture Quality: Medium
   Anti-Aliasing: 2x
   ```

### 4.4 Player Settings配置

#### Android平台设置
1. Edit → Project Settings → Player
2. **Company和Product**：
   ```csharp
   Company Name: chenyu
   Product Name: ChenyuProf03
   Package Name: com.chenyu.ChenyuProf03
   Version: 1.0
   Bundle Version Code: 1
   ```

3. **Resolution和Presentation**：
   ```csharp
   // Default Orientation
   Landscape Left: ✓
   Landscape Right: ✓
   Auto Rotation: ✓
   ```

4. **Other Settings**：
   ```csharp
   // Rendering
   Graphics APIs: OpenGLES3, Vulkan
   Color Space: Linear
   Metal API Only: false

   // Configuration
   Scripting Backend: IL2CPP
   API Compatibility Level: .NET Standard 2.1

   // Identification
   Minimum API Level: Android 12L (API 32)
   Target API Level: Android 12L (API 32)
   ```

---

## 🎯 5. 测试检查清单

### 5.1 MainMenu场景测试清单

- [ ] **VR交互测试**
  - [ ] 可以用VR手柄抓取场景选择物品
  - [ ] 抓取时显示正确的场景预览信息
  - [ ] 松开手后物品自动返回原位
  - [ ] 抓取时播放音效和动画效果

- [ ] **场景切换测试**
  - [ ] 按A键可以进入选中的场景
  - [ ] 场景切换时播放过渡音效
  - [ ] 长按菜单键3秒返回功能正常

- [ ] **UI显示测试**
  - [ ] 欢迎信息正确显示
  - [ ] 场景信息面板内容正确
  - [ ] 菜单返回进度条正确显示

### 5.2 PhotoScene场景测试清单

- [ ] **VR相机控制测试**
  - [ ] 右手正确持有相机模型
  - [ ] UI画布正确附加到相机前方
  - [ ] 左右手柄的5种控制模式正常工作
  - [ ] 参数调节时实时预览正常

- [ ] **摄影功能测试**
  - [ ] 快门键拍照功能正常
  - [ ] 照片正确保存到CapturedPhotos文件夹
  - [ ] 曝光控制和景深效果正常
  - [ ] 所有5个参数的调节范围合理

- [ ] **手柄控制测试**
  - [ ] 常态：左右Grab键控制焦段
  - [ ] X键模式：控制对焦距离
  - [ ] Y键模式：控制光圈
  - [ ] A键模式：控制快门
  - [ ] B键模式：控制ISO

### 5.3 Museum场景测试清单

- [ ] **VR移动测试**
  - [ ] 左手摇杆移动功能正常
  - [ ] 右手摇杆转向功能正常
  - [ ] 瞬移系统瞄准和执行正常
  - [ ] 移动模式切换功能正常

- [ ] **照片显示测试**
  - [ ] 15个相框正确加载照片
  - [ ] 照片自动轮播功能正常
  - [ ] 悬停时显示照片详细信息
  - [ ] 照片切换动画流畅

- [ ] **UI和统计测试**
  - [ ] 欢迎信息正确显示
  - [ ] 控制说明面板内容正确
  - [ ] 统计信息实时更新
  - [ ] 调试信息面板功能正常

---

## ⚠️ 6. 常见问题解决

### 6.1 VR交互问题

**问题：手柄无法抓取物品**
解决方法：
1. 检查Collider是否正确设置
2. 确认Rigidbody组件存在且IsKinematic=true
3. 验证VRGrabbable组件参数正确
4. 检查手柄锚点引用是否正确

**问题：UI在VR中显示不清晰**
解决方法：
1. 确认Canvas设置为正确的Render Mode
2. 调整Canvas的Scale参数
3. 检查TextMeshPro字体资源
4. 优化UI元素的尺寸和间距

### 6.2 性能问题

**问题：Quest3运行卡顿**
解决方法：
1. 降低Quality Settings中的阴影质量
2. 减少同时显示的照片数量
3. 优化模型的多边形数量
4. 使用纹理压缩格式

**问题：照片加载缓慢**
解决方法：
1. 减小照片分辨率
2. 使用异步加载
3. 实现照片预缓存系统
4. 压缩JPG质量

### 6.3 功能问题

**问题：拍照保存失败**
解决方法：
1. 检查文件写入权限
2. 确认CapturedPhotos文件夹存在
3. 验证文件名格式正确
4. 检查磁盘空间

**问题：场景切换失败**
解决方法：
1. 确认场景名称正确
2. 检查Build Settings中的场景列表
3. 验证场景路径正确
4. 添加错误处理和日志

---

## 📦 7. 打包和部署

### 7.1 Build Settings配置

1. File → Build Settings
2. **Scenes In Build**：
   ```csharp
   0. MainMenu.unity (Enabled: true)
   1. Museum.unity (Enabled: true)
   2. PhotoScene.unity (Enabled: true)
   ```

3. **Platform**：Android
4. **Texture Compression**：ASTC (6x6 block)
5. **Split APKs by target architecture**：ARM64

### 7.2 构建选项

#### Development Build
- ✅ Development Build
- ✅ Script Debugging
- ✅ Autoconnect Profiler

#### Release Build
- ❌ Development Build
- ✅ Compression Method: LZ4HC
- ✅ Export Project: false

### 7.3 Quest3部署步骤

1. **USB连接**：使用USB-C线连接Quest3
2. **开发者模式**：确保Quest3开启开发者模式
3. **安装应用**：
   ```bash
   adb install -r ChenyuProf03.apk
   ```
4. **启动测试**：
   ```bash
   adb shell am start -n com.chenyu.ChenyuProf03/com.unity3d.player.UnityPlayerActivity
   ```

---

## 📊 8. 项目维护和优化

### 8.1 性能监控

#### 使用Profiler
1. Window → Analysis → Profiler
2. 监控关键指标：
   - CPU使用率 < 70%
   - GPU使用率 < 80%
   - 内存使用 < 2GB
   - 帧率 > 60fps

#### 优化建议
- 使用对象池管理动态内容
- 实现LOD系统
- 优化Draw Calls
- 启用GPU Instancing

### 8.2 内容更新

#### 照片内容管理
1. 定期更新CapturedPhotos文件夹
2. 删除过大的照片文件
3. 维护照片元数据
4. 实现照片分类系统

#### 场景内容扩展
1. 添加新的拍照场景
2. 扩展博物馆展区
3. 增加新的相机功能
4. 实现用户自定义内容

### 8.3 版本控制

#### Git配置
1. 忽略大文件：
   ```gitignore
   # Large files
   *.psd
   *.fbx
   *.wav
   *.mp3

   # Unity specific
   Library/
   Temp/
   Logs/
   ```

2. 分支管理：
   - main: 稳定版本
   - develop: 开发版本
   - feature/*: 功能分支
   - hotfix/*: 紧急修复

---

## 🎉 总结

本配置指南涵盖了VR摄影项目的完整设置流程，包括：

- ✅ **三个场景的详细配置步骤**
- ✅ **所有必需的组件和参数设置**
- ✅ **完整的素材清单和要求**
- ✅ **测试检查清单和问题解决方案**
- ✅ **打包部署和维护指南**

按照本指南进行配置，可以确保项目在Quest3平台上的完美运行，提供专业级的VR摄影体验！