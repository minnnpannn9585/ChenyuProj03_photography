using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR;
using UnityEngine.Events;
using System.Collections;

namespace VRPhotography
{
    /// <summary>
    /// VR取景器UI系统
    /// 显示实时画面和摄影参数
    /// </summary>
    public class ViewfinderUI : MonoBehaviour
    {
        [Header("Display Components")]
        [SerializeField] private RawImage viewfinderDisplay;
        [SerializeField] private Canvas viewfinderCanvas;
        [SerializeField] private Transform viewfinderTransform;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI focalLengthText;
        [SerializeField] private TextMeshProUGUI isoText;
        [SerializeField] private TextMeshProUGUI shutterSpeedText;
        [SerializeField] private TextMeshProUGUI apertureText;
        [SerializeField] private TextMeshProUGUI batteryText;
        [SerializeField] private TextMeshProUGUI photoCountText;
        [SerializeField] private TextMeshProUGUI modeText;
        
        [Header("UI Icons")]
        [SerializeField] private Image focusIndicator;
        [SerializeField] private Image[] focusPoints;
        [SerializeField] private Image exposureIndicator;
        [SerializeField] private Image batteryIcon;
        [SerializeField] private Image flashIndicator;
        
        [Header("UI Settings")]
        [SerializeField] private bool showFocusPoints = true;
        [SerializeField] private bool showExposureInfo = true;
        [SerializeField] private bool showGridLines = false;
        [SerializeField] private float uiScale = 1f;
        [SerializeField] private float updateRate = 30f;
        
        [Header("Camera Reference")]
        [SerializeField] private VRCamera vrCamera;
        [SerializeField] private VRPhotoCapture photoCapture;
        
        // Grid lines
        [SerializeField] private Image[] gridLines;
        
        // Private variables
        private float lastUpdateTime;
        private int lastPhotoCount = -1;
        private float currentBatteryLevel = 100f;
        private bool isCharging = false;
        
        // Events
        public UnityEvent onSettingsRequested;
        public UnityEvent onViewfinderToggle;
        
        private void Awake()
        {
            FindComponents();
            InitializeUI();
        }
        
        private void Start()
        {
            SetupViewfinderDisplay();
            UpdateUIVisibility();
        }
        
        private void Update()
        {
            if (ShouldUpdateUI())
            {
                UpdatePhotographySettings();
                UpdateStatusInfo();
                UpdateFocusIndicators();
                lastUpdateTime = Time.time;
            }
        }
        
        private void FindComponents()
        {
            // 查找VR相机
            if (vrCamera == null)
                vrCamera = GetComponentInParent<VRCamera>();
                
            if (photoCapture == null)
                photoCapture = GetComponentInParent<VRPhotoCapture>();
            
            // 查找取景器显示组件
            if (viewfinderDisplay == null)
                viewfinderDisplay = GetComponentInChildren<RawImage>();
            
            // 查找画布
            if (viewfinderCanvas == null)
                viewfinderCanvas = GetComponentInParent<Canvas>();
        }
        
        private void InitializeUI()
        {
            // 设置UI缩放
            if (viewfinderTransform != null)
            {
                viewfinderTransform.localScale = Vector3.one * uiScale;
            }
            
            // 初始化文本组件
            SetupTextComponents();
            
            // 初始化图标组件
            SetupIconComponents();
            
            // 设置网格线
            SetupGridLines();
        }
        
        private void SetupTextComponents()
        {
            // 查找或创建文本组件
            if (focalLengthText == null)
                focalLengthText = FindChildByName<TextMeshProUGUI>("FocalLengthText");
                
            if (isoText == null)
                isoText = FindChildByName<TextMeshProUGUI>("ISOText");
                
            if (shutterSpeedText == null)
                shutterSpeedText = FindChildByName<TextMeshProUGUI>("ShutterSpeedText");
                
            if (apertureText == null)
                apertureText = FindChildByName<TextMeshProUGUI>("ApertureText");
                
            if (batteryText == null)
                batteryText = FindChildByName<TextMeshProUGUI>("BatteryText");
                
            if (photoCountText == null)
                photoCountText = FindChildByName<TextMeshProUGUI>("PhotoCountText");
                
            if (modeText == null)
                modeText = FindChildByName<TextMeshProUGUI>("ModeText");
        }
        
        private void SetupIconComponents()
        {
            // 查找图标组件
            if (focusIndicator == null)
                focusIndicator = FindChildByName<Image>("FocusIndicator");
                
            if (exposureIndicator == null)
                exposureIndicator = FindChildByName<Image>("ExposureIndicator");
                
            if (batteryIcon == null)
                batteryIcon = FindChildByName<Image>("BatteryIcon");
                
            if (flashIndicator == null)
                flashIndicator = FindChildByName<Image>("FlashIndicator");
            
            // 查找对焦点
            var focusPointsTransform = transform.Find("FocusPoints");
            if (focusPointsTransform != null)
            {
                focusPoints = focusPointsTransform.GetComponentsInChildren<Image>();
            }
        }
        
        private void SetupGridLines()
        {
            var gridTransform = transform.Find("GridLines");
            if (gridTransform != null)
            {
                gridLines = gridTransform.GetComponentsInChildren<Image>();
            }
            
            UpdateGridLinesVisibility();
        }
        
        private T FindChildByName<T>(string name) where T : Component
        {
            Transform child = transform.Find(name);
            return child != null ? child.GetComponent<T>() : null;
        }
        
        private void SetupViewfinderDisplay()
        {
            if (viewfinderDisplay != null && vrCamera != null && vrCamera.CaptureCamera != null)
            {
                // 创建取景器渲染纹理
                RenderTexture viewfinderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                viewfinderTexture.Create();
                
                // 设置相机渲染到取景器
                vrCamera.CaptureCamera.targetTexture = viewfinderTexture;
                viewfinderDisplay.texture = viewfinderTexture;
                
                Debug.Log("取景器显示已初始化");
            }
        }
        
        private void UpdateUIVisibility()
        {
            // 更新焦点显示
            if (focusIndicator != null)
                focusIndicator.gameObject.SetActive(showFocusPoints);
                
            if (focusPoints != null)
            {
                foreach (var point in focusPoints)
                {
                    if (point != null)
                        point.gameObject.SetActive(showFocusPoints);
                }
            }
            
            // 更新曝光信息显示
            if (exposureIndicator != null)
                exposureIndicator.gameObject.SetActive(showExposureInfo);
            
            // 更新网格线显示
            UpdateGridLinesVisibility();
        }
        
        private void UpdateGridLinesVisibility()
        {
            if (gridLines != null)
            {
                foreach (var line in gridLines)
                {
                    if (line != null)
                        line.gameObject.SetActive(showGridLines);
                }
            }
        }
        
        private bool ShouldUpdateUI()
        {
            return Time.time - lastUpdateTime > (1f / updateRate);
        }
        
        private void UpdatePhotographySettings()
        {
            if (vrCamera == null) return;
            
            // 更新焦距显示
            if (focalLengthText != null)
            {
                focalLengthText.text = $"{vrCamera.CurrentFocalLength:F0}mm";
            }
            
            // 更新ISO显示
            if (isoText != null)
            {
                isoText.text = $"ISO {vrCamera.ISO}";
            }
            
            // 更新快门速度显示
            if (shutterSpeedText != null)
            {
                if (vrCamera.ShutterSpeed >= 1f)
                    shutterSpeedText.text = $"{vrCamera.ShutterSpeed:F0}\"";
                else
                    shutterSpeedText.text = $"1/{(1f/vrCamera.ShutterSpeed):F0}";
            }
            
            // 更新光圈显示
            if (apertureText != null)
            {
                apertureText.text = $"f/{vrCamera.Aperture:F1}";
            }
            
            // 更新模式显示
            if (modeText != null)
            {
                modeText.text = vrCamera.IsPhotoMode ? "PHOTO" : "VIDEO";
            }
        }
        
        private void UpdateStatusInfo()
        {
            // 更新照片数量
            if (photoCapture != null && photoCountText != null)
            {
                int currentPhotoCount = photoCapture.GetPhotoCount();
                if (currentPhotoCount != lastPhotoCount)
                {
                    photoCountText.text = $"{currentPhotoCount}";
                    lastPhotoCount = currentPhotoCount;
                }
            }
            
            // 更新电池状态
            UpdateBatteryStatus();
        }
        
        private void UpdateBatteryStatus()
        {
            // 模拟电池状态（实际项目中应该从系统获取）
            if (batteryText != null)
            {
                batteryText.text = $"{currentBatteryLevel:F0}%";
            }
            
            if (batteryIcon != null)
            {
                // 根据电池电量改变图标颜色
                Color batteryColor = Color.green;
                if (currentBatteryLevel < 20f)
                    batteryColor = Color.red;
                else if (currentBatteryLevel < 50f)
                    batteryColor = Color.yellow;
                    
                batteryIcon.color = batteryColor;
            }
        }
        
        private void UpdateFocusIndicators()
        {
            // 简化的对焦指示器
            if (focusIndicator != null)
            {
                // 模拟对焦状态
                bool isInFocus = true; // 实际应该从相机获取对焦状态
                focusIndicator.color = isInFocus ? Color.green : Color.red;
            }
            
            // 更新焦点
            if (focusPoints != null && showFocusPoints)
            {
                foreach (var point in focusPoints)
                {
                    if (point != null)
                    {
                        // 简单的焦点动画
                        float pulse = Mathf.Sin(Time.time * 2f) * 0.1f + 0.9f;
                        point.transform.localScale = Vector3.one * pulse;
                    }
                }
            }
        }
        
        // Public Methods
        
        /// <summary>
        /// 切换网格线显示
        /// </summary>
        public void ToggleGridLines()
        {
            showGridLines = !showGridLines;
            UpdateGridLinesVisibility();
        }
        
        /// <summary>
        /// 切换焦点显示
        /// </summary>
        public void ToggleFocusPoints()
        {
            showFocusPoints = !showFocusPoints;
            UpdateUIVisibility();
        }
        
        /// <summary>
        /// 切换曝光信息显示
        /// </summary>
        public void ToggleExposureInfo()
        {
            showExposureInfo = !showExposureInfo;
            UpdateUIVisibility();
        }
        
        /// <summary>
        /// 设置UI缩放
        /// </summary>
        public void SetUIScale(float scale)
        {
            uiScale = Mathf.Clamp(scale, 0.5f, 2f);
            
            if (viewfinderTransform != null)
            {
                viewfinderTransform.localScale = Vector3.one * uiScale;
            }
        }
        
        /// <summary>
        /// 显示设置菜单
        /// </summary>
        public void ShowSettings()
        {
            onSettingsRequested?.Invoke();
        }
        
        /// <summary>
        /// 播放拍照动画效果
        /// </summary>
        public void PlayCaptureAnimation()
        {
            StartCoroutine(CaptureAnimationCoroutine());
        }
        
        private IEnumerator CaptureAnimationCoroutine()
        {
            // 闪光效果
            if (viewfinderDisplay != null)
            {
                Color originalColor = viewfinderDisplay.color;
                viewfinderDisplay.color = Color.white;
                
                yield return new WaitForSeconds(0.05f);
                
                viewfinderDisplay.color = originalColor;
            }
            
            // 快门震动效果（如果支持）
            if (viewfinderTransform != null)
            {
                Vector3 originalPosition = viewfinderTransform.localPosition;
                viewfinderTransform.localPosition += Vector3.up * 0.01f;
                
                yield return new WaitForSeconds(0.1f);
                
                viewfinderTransform.localPosition = originalPosition;
            }
        }
        
        /// <summary>
        /// 模拟电池电量变化（用于测试）
        /// </summary>
        public void SimulateBatteryLevel(float level)
        {
            currentBatteryLevel = Mathf.Clamp(level, 0f, 100f);
        }
        
        private void OnDestroy()
        {
            // 清理渲染纹理
            if (viewfinderDisplay != null && viewfinderDisplay.texture is RenderTexture rt)
            {
                rt.Release();
            }
        }
    }
}