# UI位置控制修复指南

## 问题描述
多个脚本在控制UI位置和缩放，导致UI被强制刷新位置和比例，变得特别大。

## 问题来源
1. **VRParameterDisplay.cs**: 强制设置Canvas缩放为0.001f，并持续更新位置
2. **VRCameraRig.cs**: 在跟随模式下调整UI的父级关系

## 解决方案

### 1. VRParameterDisplay.cs 修复
- 移除了强制缩放设置 `displayCanvas.transform.localScale = Vector3.one * 0.001f`
- 添加了 `allowPositionControl` 选项，默认为false
- 只有在 `allowPositionControl = true` 时才更新UI位置

### 2. VRCameraRig.cs 修复
- 添加了 `allowUIControl` 选项，默认为false
- 只有在 `allowUIControl = true` 时才处理UI跟随逻辑
- 添加了完整的UI控制方法

### 3. UIFollowTester.cs 增强
- 添加了第二个快捷键（I键）来切换UI控制模式
- 显示当前状态（脚本控制 vs Editor控制）

## 使用方法

### 完全禁用脚本控制（推荐）
在PhotoScene中选择VR Camera Rig GameObject，然后在Inspector中：
1. 设置 `Allow UI Control` = false（默认值）
2. 这样UI将保持你在Editor中设置的原始位置、缩放和旋转

### 启用脚本控制
如果你需要脚本控制UI位置：
1. 设置 `Allow UI Control` = true
2. 设置 `Follow Camera Model` = true/false 来控制是否跟随相机模型

### 运行时测试
- 按U键：切换UI跟随模式（需要 `Allow UI Control` = true）
- 按I键：切换UI控制模式（脚本控制 vs Editor控制）

## 配置建议

1. **Editor设置阶段**：
   - 设置 `Allow UI Control` = false
   - 手动调整UI位置、缩放到理想状态
   - UI将作为相机模型的子物体，自然跟随

2. **运行时阶段**：
   - 保持 `Allow UI Control` = false
   - UI将完全保持Editor设置，不会被任何脚本修改

## 技术细节

修复前的问题：
- VRParameterDisplay强制设置scale为0.001f导致UI变得巨大
- VRCameraRig持续更新UI位置，覆盖用户手动设置

修复后的行为：
- 当 `allowPositionControl = false` 且 `allowUIControl = false` 时
- UI完全保持Editor中的原始设置
- 不会被任何脚本修改位置、缩放或旋转

这样确保了UI始终按照你的设计显示，不会被脚本意外修改。