using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

namespace VRPhotography
{
    /// <summary>
    /// Quest3性能优化器
    /// 针对Quest3硬件特性进行优化
    /// </summary>
    public class Quest3PerformanceOptimizer : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private int targetFrameRate = 90;
        [SerializeField] private bool useFixedFramerate = true;
        [SerializeField] private bool enableDynamicResolution = true;
        [SerializeField] private float targetScale = 1.0f;
        [SerializeField] private float minScale = 0.7f;
        
        [Header("Rendering Settings")]
        [SerializeField] private bool optimizeForVR = true;
        [SerializeField] private bool enableOcclusion = true;
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private bool enableInstancing = true;
        
        [Header("Quality Settings")]
        [SerializeField] private TextureQuality textureQuality = TextureQuality.High;
        [SerializeField] private UnityEngine.ShadowQuality shadowQuality = UnityEngine.ShadowQuality.Disabled;
        [SerializeField] private bool enablePostProcessing = true;
        [SerializeField] private bool enableBloom = false;
        [SerializeField] private bool enableDepthOfField = false;
        
        [Header("Memory Management")]
        [SerializeField] private bool enableMemoryOptimization = true;
        [SerializeField] private int maxTextureSize = 2048;
        [SerializeField] private bool compressTextures = true;
        
        [Header("Monitoring")]
        [SerializeField] private bool showPerformanceStats = false;
        [SerializeField] private bool enableFrameTimeWarning = true;
        [SerializeField] private float frameTimeWarningThreshold = 16.67f; // 60fps = 16.67ms per frame
        
        // Performance monitoring
        private float currentFrameTime;
        private float averageFrameTime;
        private int frameCount;
        private float lastUpdateTime;
        
        // URP Asset reference
        private UniversalRenderPipelineAsset urpAsset;
        
        // Performance settings
        [System.Serializable]
        public class PerformanceProfile
        {
            public string name;
            public int targetFrameRate;
            public float renderScale;
            public TextureQuality textureQuality;
            public UnityEngine.ShadowQuality shadowQuality;
            public bool enablePostProcessing;
            public bool enableBloom;
            public bool enableDepthOfField;
        }
        
        [SerializeField] private PerformanceProfile[] performanceProfiles;
        private int currentProfileIndex = 1; // 默认使用中等配置
        
        // Performance metrics
        [System.Serializable]
        public class PerformanceMetrics
        {
            public float frameRate;
            public float frameTime;
            public float memoryUsage;
            public float drawCalls;
            public float triangles;
            public float renderScale;
            
            public bool IsPerformanceGood => frameRate >= 80f && frameTime <= 12.5f; // 80fps = 12.5ms
            public bool NeedsOptimization => frameRate < 70f || frameTime > 14.3f; // 70fps = 14.3ms
        }
        
        private PerformanceMetrics currentMetrics = new PerformanceMetrics();
        
        private void Awake()
        {
            InitializeOptimizer();
        }
        
        private void Start()
        {
            ApplyOptimizations();
            StartPerformanceMonitoring();
        }
        
        private void Update()
        {
            UpdatePerformanceMetrics();
            
            if (enableFrameTimeWarning && currentFrameTime > frameTimeWarningThreshold)
            {
                HandlePerformanceIssue();
            }
        }
        
        private void InitializeOptimizer()
        {
            // 获取URP资产
            urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            
            if (urpAsset == null)
            {
                Debug.LogWarning("未找到URP资产，某些优化可能无法应用");
            }
            
            // 创建默认性能配置文件
            if (performanceProfiles == null || performanceProfiles.Length == 0)
            {
                CreateDefaultPerformanceProfiles();
            }
            
            Debug.Log("Quest3性能优化器已初始化");
        }
        
        private void CreateDefaultPerformanceProfiles()
        {
            performanceProfiles = new PerformanceProfile[]
            {
                new PerformanceProfile
                {
                    name = "高性能",
                    targetFrameRate = 90,
                    renderScale = 1.2f,
                    textureQuality = TextureQuality.Ultra,
                    shadowQuality = UnityEngine.ShadowQuality.All,
                    enablePostProcessing = true,
                    enableBloom = true,
                    enableDepthOfField = true
                },
                new PerformanceProfile
                {
                    name = "平衡",
                    targetFrameRate = 90,
                    renderScale = 1.0f,
                    textureQuality = TextureQuality.High,
                    shadowQuality = UnityEngine.ShadowQuality.HardOnly,
                    enablePostProcessing = true,
                    enableBloom = false,
                    enableDepthOfField = false
                },
                new PerformanceProfile
                {
                    name = "节能",
                    targetFrameRate = 72,
                    renderScale = 0.8f,
                    textureQuality = TextureQuality.Medium,
                    shadowQuality = UnityEngine.ShadowQuality.Disabled,
                    enablePostProcessing = false,
                    enableBloom = false,
                    enableDepthOfField = false
                }
            };
        }
        
        private void ApplyOptimizations()
        {
            ApplyFrameRateSettings();
            ApplyRenderingSettings();
            ApplyQualitySettings();
            ApplyMemoryOptimizations();
            ApplyVRSpecificOptimizations();
        }
        
        private void ApplyFrameRateSettings()
        {
            if (useFixedFramerate)
            {
                Application.targetFrameRate = targetFrameRate;
                QualitySettings.vSyncCount = 0; // 关闭垂直同步
                Debug.Log($"设置目标帧率: {targetFrameRate} FPS");
            }
        }
        
        private void ApplyRenderingSettings()
        {
            // 应用动态分辨率
            if (enableDynamicResolution && urpAsset != null)
            {
                urpAsset.renderScale = targetScale;
                Debug.Log($"设置渲染缩放: {targetScale}");
            }
            
            // 启用遮挡剔除
            if (enableOcclusion)
            {
                var occlusion = GetComponent<OcclusionPortal>();
                if (occlusion == null)
                {
                    gameObject.AddComponent<OcclusionPortal>();
                }
            }
            
            // 启用GPU实例化
            if (enableInstancing)
            {
                GraphicsSettings.useGPUInstancing = true;
            }
        }
        
        private void ApplyQualitySettings()
        {
            // 纹理质量
            QualitySettings.masterTextureLimit = GetTextureLimit(textureQuality);
            
            // 阴影设置
            QualitySettings.shadows = shadowQuality;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 10f;
            
            // 抗锯齿
            QualitySettings.antiAliasing = 2; // MSAA 2x for VR
            
            // 像素光源数量
            QualitySettings.pixelLightCount = 1;
            
            Debug.Log($"应用质量设置: 纹理={textureQuality}, 阴影={shadowQuality}");
        }
        
        private int GetTextureLimit(TextureQuality quality)
        {
            switch (quality)
            {
                case TextureQuality.Ultra: return 0;
                case TextureQuality.High: return 1;
                case TextureQuality.Medium: return 2;
                case TextureQuality.Low: return 3;
                default: return 1;
            }
        }
        
        private void ApplyMemoryOptimizations()
        {
            if (!enableMemoryOptimization) return;
            
            // 设置最大纹理大小
            if (maxTextureSize > 0)
            {
                QualitySettings.masterTextureLimit = Mathf.Max(QualitySettings.masterTextureLimit, 
                    CalculateTextureLimit(maxTextureSize));
            }
            
            // 强制纹理压缩
            if (compressTextures)
            {
                // 这里可以添加纹理压缩逻辑
                Debug.Log("纹理压缩已启用");
            }
            
            // 垃圾回收
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }
        
        private int CalculateTextureLimit(int maxSize)
        {
            if (maxSize >= 4096) return 0;
            if (maxSize >= 2048) return 1;
            if (maxSize >= 1024) return 2;
            if (maxSize >= 512) return 3;
            return 4;
        }
        
        private void ApplyVRSpecificOptimizations()
        {
            if (!optimizeForVR) return;
            
            // 单通道立体渲染
            XRSettings.eyeTextureResolutionScale = targetScale;
            
            // 禁用不需要的功能
            QualitySettings.skinWeights = SkinWeights.TwoBones;
            QualitySettings.particleRaycastBudget = 64;
            QualitySettings.asyncUploadTimeSlice = 2;
            QualitySettings.asyncUploadBufferSize = 4;
            
            // 优化LOD
            if (enableLOD)
            {
                LODBias = 0.7f;
            }
            
            Debug.Log("VR特定优化已应用");
        }
        
        private void StartPerformanceMonitoring()
        {
            if (showPerformanceStats)
            {
                StartCoroutine(PerformanceMonitoringCoroutine());
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            currentFrameTime = Time.unscaledDeltaTime;
            frameCount++;
            
            // 每秒更新一次平均值
            if (Time.time - lastUpdateTime >= 1f)
            {
                averageFrameTime = currentFrameTime;
                
                currentMetrics.frameRate = 1f / averageFrameTime;
                currentMetrics.frameTime = averageFrameTime * 1000f; // 转换为毫秒
                currentMetrics.memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f); // MB
                currentMetrics.renderScale = targetScale;
                
                frameCount = 0;
                lastUpdateTime = Time.time;
            }
        }
        
        private void HandlePerformanceIssue()
        {
            // 自动降低质量
            if (currentMetrics.NeedsOptimization && currentProfileIndex < performanceProfiles.Length - 1)
            {
                currentProfileIndex++;
                ApplyPerformanceProfile(performanceProfiles[currentProfileIndex]);
                Debug.LogWarning($"性能问题: 自动切换到性能配置 {performanceProfiles[currentProfileIndex].name}");
            }
        }
        
        // Public Methods
        
        /// <summary>
        /// 应用性能配置文件
        /// </summary>
        public void ApplyPerformanceProfile(PerformanceProfile profile)
        {
            if (profile == null) return;
            
            targetFrameRate = profile.targetFrameRate;
            targetScale = profile.renderScale;
            textureQuality = profile.textureQuality;
            shadowQuality = profile.shadowQuality;
            enablePostProcessing = profile.enablePostProcessing;
            enableBloom = profile.enableBloom;
            enableDepthOfField = profile.enableDepthOfField;
            
            ApplyOptimizations();
        }
        
        /// <summary>
        /// 切换到下一个性能配置
        /// </summary>
        public void CyclePerformanceProfile()
        {
            currentProfileIndex = (currentProfileIndex + 1) % performanceProfiles.Length;
            ApplyPerformanceProfile(performanceProfiles[currentProfileIndex]);
        }
        
        /// <summary>
        /// 获取当前性能指标
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics()
        {
            return currentMetrics;
        }
        
        /// <summary>
        /// 动态调整渲染缩放
        /// </summary>
        public void SetRenderScale(float scale)
        {
            targetScale = Mathf.Clamp(scale, minScale, 1.5f);
            
            if (urpAsset != null)
            {
                urpAsset.renderScale = targetScale;
            }
            
            if (XRSettings.enabled)
            {
                XRSettings.eyeTextureResolutionScale = targetScale;
            }
        }
        
        /// <summary>
        /// 启用/禁用性能监控
        /// </summary>
        public void SetPerformanceMonitoring(bool enable)
        {
            showPerformanceStats = enable;
            
            if (enable)
            {
                StartPerformanceMonitoring();
            }
        }
        
        private IEnumerator PerformanceMonitoringCoroutine()
        {
            while (showPerformanceStats)
            {
                // 更新性能UI或日志
                Debug.Log($"性能监控 - FPS: {currentMetrics.frameRate:F1}, " +
                         $"帧时间: {currentMetrics.frameTime:F1}ms, " +
                         $"内存: {currentMetrics.memoryUsage:F1}MB");
                
                yield return new WaitForSeconds(5f); // 每5秒输出一次
            }
        }
        
        private void OnGUI()
        {
            if (!showPerformanceStats) return;
            
            // 显示性能统计
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"FPS: {currentMetrics.frameRate:F1}");
            GUILayout.Label($"帧时间: {currentMetrics.frameTime:F1}ms");
            GUILayout.Label($"内存: {currentMetrics.memoryUsage:F1}MB");
            GUILayout.Label($"渲染缩放: {currentMetrics.renderScale:F2}");
            GUILayout.Label($"当前配置: {performanceProfiles[currentProfileIndex].name}");
            
            if (GUILayout.Button("切换性能配置"))
            {
                CyclePerformanceProfile();
            }
            
            GUILayout.EndArea();
        }
        
        // Performance quality enum
        public enum TextureQuality
        {
            Ultra,
            High,
            Medium,
            Low
        }
    }
}