using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

namespace VRPhotography
{
    /// <summary>
    /// VR相机设置UI系统
    /// 提供摄影参数调节界面
    /// </summary>
    public class CameraSettingsUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject quickSettingsPanel;
        [SerializeField] private Transform settingsTransform;
        
        [Header("Camera Controls")]
        [SerializeField] private Slider focalLengthSlider;
        [SerializeField] private Slider isoSlider;
        [SerializeField] private Slider shutterSpeedSlider;
        [SerializeField] private Slider apertureSlider;
        
        [Header("Display Texts")]
        [SerializeField] private TextMeshProUGUI focalLengthValue;
        [SerializeField] private TextMeshProUGUI isoValue;
        [SerializeField] private TextMeshProUGUI shutterSpeedValue;
        [SerializeField] private TextMeshProUGUI apertureValue;
        
        [Header("Preset Buttons")]
        [SerializeField] private Button[] presetButtons;
        [SerializeField] private string[] presetNames;
        
        [Header("Quick Settings")]
        [SerializeField] private Button[] quickSettingButtons;
        [SerializeField] private TextMeshProUGUI[] quickSettingLabels;
        
        [Header("VR Interaction")]
        [SerializeField] private float uiDistance = 2f;
        [SerializeField] private float uiScale = 0.001f;
        [SerializeField] private bool followController = true;
        [SerializeField] private Transform targetController;
        
        [Header("Camera Reference")]
        [SerializeField] private VRCamera vrCamera;
        [SerializeField] private VRCameraInteraction cameraInteraction;
        
        // Camera presets
        [System.Serializable]
        public class CameraPreset
        {
            public string name;
            public float focalLength;
            public int iso;
            public float shutterSpeed;
            public float aperture;
            public string description;
        }
        
        [SerializeField] private CameraPreset[] cameraPresets;
        
        // Events
        public UnityEvent onSettingsOpened;
        public UnityEvent onSettingsClosed;
        public UnityEvent<CameraPreset> onPresetSelected;
        
        // Private variables
        private bool isSettingsOpen = false;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private VRCameraRig vrCameraRig;
        
        private void Awake()
        {
            FindComponents();
            InitializeUI();
            SetupPresets();
        }
        
        private void Start()
        {
            SetupSliderListeners();
            UpdateUIValues();
            HideSettings();
        }
        
        private void Update()
        {
            if (isSettingsOpen && followController)
            {
                UpdateSettingsPosition();
            }
        }
        
        private void FindComponents()
        {
            // 查找VR相机
            if (vrCamera == null)
                vrCamera = GetComponentInParent<VRCamera>();
                
            if (cameraInteraction == null)
                cameraInteraction = GetComponentInParent<VRCameraInteraction>();
            
            // 查找VR相机系统
            vrCameraRig = FindObjectOfType<VRCameraRig>();
            
            // 设置目标控制器
            if (vrCameraRig != null)
            {
                targetController = vrCameraRig.RightController?.transform;
            }
        }
        
        private void InitializeUI()
        {
            // 初始化滑块范围
            if (focalLengthSlider != null)
            {
                focalLengthSlider.minValue = 24f;
                focalLengthSlider.maxValue = 200f;
                focalLengthSlider.value = 50f;
            }
            
            if (isoSlider != null)
            {
                isoSlider.minValue = 100f;
                isoSlider.maxValue = 6400f;
                isoSlider.value = 100f;
            }
            
            if (shutterSpeedSlider != null)
            {
                shutterSpeedSlider.minValue = 1f;
                shutterSpeedSlider.maxValue = 8000f;
                shutterSpeedSlider.value = 60f;
            }
            
            if (apertureSlider != null)
            {
                apertureSlider.minValue = 1.4f;
                apertureSlider.maxValue = 22f;
                apertureSlider.value = 2.8f;
            }
            
            // 设置UI位置和缩放
            if (settingsTransform != null)
            {
                settingsTransform.localScale = Vector3.one * uiScale;
            }
        }
        
        private void SetupSliderListeners()
        {
            if (focalLengthSlider != null)
            {
                focalLengthSlider.onValueChanged.AddListener(OnFocalLengthChanged);
            }
            
            if (isoSlider != null)
            {
                isoSlider.onValueChanged.AddListener(OnISOChanged);
            }
            
            if (shutterSpeedSlider != null)
            {
                shutterSpeedSlider.onValueChanged.AddListener(OnShutterSpeedChanged);
            }
            
            if (apertureSlider != null)
            {
                apertureSlider.onValueChanged.AddListener(OnApertureChanged);
            }
        }
        
        private void SetupPresets()
        {
            // 如果没有预设，创建默认预设
            if (cameraPresets == null || cameraPresets.Length == 0)
            {
                CreateDefaultPresets();
            }
            
            // 设置预设按钮
            if (presetButtons != null && cameraPresets != null)
            {
                for (int i = 0; i < presetButtons.Length && i < cameraPresets.Length; i++)
                {
                    int index = i; // 避免闭包问题
                    presetButtons[i].onClick.AddListener(() => OnPresetButtonClicked(index));
                    
                    // 设置按钮文本
                    var buttonText = presetButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = cameraPresets[i].name;
                    }
                }
            }
            
            // 设置快速设置按钮
            SetupQuickSettings();
        }
        
        private void CreateDefaultPresets()
        {
            cameraPresets = new CameraPreset[]
            {
                new CameraPreset { name = "人像", focalLength = 85f, iso = 200, shutterSpeed = 125f, aperture = 1.8f, description = "适合人像摄影" },
                new CameraPreset { name = "风景", focalLength = 24f, iso = 100, shutterSpeed = 250f, aperture = 11f, description = "适合风景摄影" },
                new CameraPreset { name = "运动", focalLength = 200f, iso = 800, shutterSpeed = 1000f, aperture = 2.8f, description = "适合运动摄影" },
                new CameraPreset { name = "微距", focalLength = 100f, iso = 400, shutterSpeed = 60f, aperture = 5.6f, description = "适合微距摄影" },
                new CameraPreset { name = "夜景", focalLength = 35f, iso = 1600, shutterSpeed = 30f, aperture = 1.4f, description = "适合夜景摄影" }
            };
        }
        
        private void SetupQuickSettings()
        {
            if (quickSettingButtons == null || quickSettingLabels == null)
                return;
            
            string[] quickLabels = { "AUTO", "P", "S", "A", "M" }; // 自动、程序、快门优先、光圈优先、手动
            
            for (int i = 0; i < quickSettingButtons.Length && i < quickLabels.Length; i++)
            {
                if (quickSettingLabels[i] != null)
                {
                    quickSettingLabels[i].text = quickLabels[i];
                }
                
                int index = i;
                quickSettingButtons[i].onClick.AddListener(() => OnQuickSettingClicked(index));
            }
        }
        
        // Slider Event Handlers
        
        private void OnFocalLengthChanged(float value)
        {
            vrCamera?.SetFocalLength(value);
            UpdateUIValues();
        }
        
        private void OnISOChanged(float value)
        {
            vrCamera?.SetISO(Mathf.RoundToInt(value));
            UpdateUIValues();
        }
        
        private void OnShutterSpeedChanged(float value)
        {
            vrCamera?.SetShutterSpeed(value);
            UpdateUIValues();
        }
        
        private void OnApertureChanged(float value)
        {
            vrCamera?.SetAperture(value);
            UpdateUIValues();
        }
        
        private void OnPresetButtonClicked(int presetIndex)
        {
            if (presetIndex >= 0 && presetIndex < cameraPresets.Length)
            {
                ApplyPreset(cameraPresets[presetIndex]);
                onPresetSelected?.Invoke(cameraPresets[presetIndex]);
            }
        }
        
        private void OnQuickSettingClicked(int settingIndex)
        {
            switch (settingIndex)
            {
                case 0: // AUTO
                    ApplyAutoSettings();
                    break;
                case 1: // P (程序)
                    ApplyProgramSettings();
                    break;
                case 2: // S (快门优先)
                    ApplyShutterPrioritySettings();
                    break;
                case 3: // A (光圈优先)
                    ApplyAperturePrioritySettings();
                    break;
                case 4: // M (手动)
                    // 手动模式，不改变设置
                    break;
            }
        }
        
        private void UpdateUIValues()
        {
            if (vrCamera == null) return;
            
            // 更新滑块值
            if (focalLengthSlider != null)
            {
                focalLengthSlider.value = vrCamera.CurrentFocalLength;
            }
            
            if (isoSlider != null)
            {
                isoSlider.value = vrCamera.ISO;
            }
            
            if (shutterSpeedSlider != null)
            {
                shutterSpeedSlider.value = vrCamera.ShutterSpeed;
            }
            
            if (apertureSlider != null)
            {
                apertureSlider.value = vrCamera.Aperture;
            }
            
            // 更新显示文本
            UpdateDisplayTexts();
        }
        
        private void UpdateDisplayTexts()
        {
            if (vrCamera == null) return;
            
            if (focalLengthValue != null)
            {
                focalLengthValue.text = $"{vrCamera.CurrentFocalLength:F0}mm";
            }
            
            if (isoValue != null)
            {
                isoValue.text = $"ISO {vrCamera.ISO}";
            }
            
            if (shutterSpeedValue != null)
            {
                if (vrCamera.ShutterSpeed >= 1f)
                    shutterSpeedValue.text = $"{vrCamera.ShutterSpeed:F0}\"";
                else
                    shutterSpeedValue.text = $"1/{(1f/vrCamera.ShutterSpeed):F0}";
            }
            
            if (apertureValue != null)
            {
                apertureValue.text = $"f/{vrCamera.Aperture:F1}";
            }
        }
        
        private void UpdateSettingsPosition()
        {
            if (targetController == null) return;
            
            // 设置UI位置在控制器前方
            targetPosition = targetController.position + targetController.forward * uiDistance;
            targetRotation = Quaternion.LookRotation(targetPosition - targetController.position);
            
            if (settingsTransform != null)
            {
                settingsTransform.position = Vector3.Lerp(settingsTransform.position, targetPosition, Time.deltaTime * 10f);
                settingsTransform.rotation = Quaternion.Slerp(settingsTransform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
        
        // Public Methods
        
        /// <summary>
        /// 显示设置面板
        /// </summary>
        public void ShowSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                isSettingsOpen = true;
                onSettingsOpened?.Invoke();
                
                // 初始化UI位置
                if (targetController != null && settingsTransform != null)
                {
                    settingsTransform.position = targetController.position + targetController.forward * uiDistance;
                    settingsTransform.rotation = Quaternion.LookRotation(settingsTransform.position - targetController.position);
                }
            }
        }
        
        /// <summary>
        /// 隐藏设置面板
        /// </summary>
        public void HideSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                isSettingsOpen = false;
                onSettingsClosed?.Invoke();
            }
        }
        
        /// <summary>
        /// 切换设置面板显示状态
        /// </summary>
        public void ToggleSettings()
        {
            if (isSettingsOpen)
                HideSettings();
            else
                ShowSettings();
        }
        
        /// <summary>
        /// 应用相机预设
        /// </summary>
        public void ApplyPreset(CameraPreset preset)
        {
            if (vrCamera == null || preset == null) return;
            
            vrCamera.SetFocalLength(preset.focalLength);
            vrCamera.SetISO(preset.iso);
            vrCamera.SetShutterSpeed(preset.shutterSpeed);
            vrCamera.SetAperture(preset.aperture);
            
            UpdateUIValues();
            
            Debug.Log($"应用相机预设: {preset.name}");
        }
        
        /// <summary>
        /// 应用自动设置
        /// </summary>
        public void ApplyAutoSettings()
        {
            var autoPreset = new CameraPreset
            {
                name = "Auto",
                focalLength = 50f,
                iso = 200,
                shutterSpeed = 125f,
                aperture = 4f
            };
            
            ApplyPreset(autoPreset);
        }
        
        /// <summary>
        /// 应用程序设置
        /// </summary>
        public void ApplyProgramSettings()
        {
            var programPreset = new CameraPreset
            {
                name = "Program",
                focalLength = 35f,
                iso = 400,
                shutterSpeed = 250f,
                aperture = 5.6f
            };
            
            ApplyPreset(programPreset);
        }
        
        /// <summary>
        /// 应用快门优先设置
        /// </summary>
        public void ApplyShutterPrioritySettings()
        {
            var shutterPreset = new CameraPreset
            {
                name = "Shutter Priority",
                focalLength = 100f,
                iso = 800,
                shutterSpeed = 1000f,
                aperture = 2.8f
            };
            
            ApplyPreset(shutterPreset);
        }
        
        /// <summary>
        /// 应用光圈优先设置
        /// </summary>
        public void ApplyAperturePrioritySettings()
        {
            var aperturePreset = new CameraPreset
            {
                name = "Aperture Priority",
                focalLength = 85f,
                iso = 200,
                shutterSpeed = 125f,
                aperture = 1.8f
            };
            
            ApplyPreset(aperturePreset);
        }
        
        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public void ResetToDefaults()
        {
            ApplyAutoSettings();
        }
        
        /// <summary>
        /// 获取当前设置作为预设
        /// </summary>
        public CameraPreset GetCurrentSettingsAsPreset()
        {
            if (vrCamera == null) return null;
            
            return new CameraPreset
            {
                name = "Custom",
                focalLength = vrCamera.CurrentFocalLength,
                iso = vrCamera.ISO,
                shutterSpeed = vrCamera.ShutterSpeed,
                aperture = vrCamera.Aperture,
                description = "用户自定义设置"
            };
        }
    }
}