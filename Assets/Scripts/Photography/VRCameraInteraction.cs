using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace VRPhotography
{
    /// <summary>
    /// VR相机交互控制器
    /// 处理手柄输入和相机操作
    /// </summary>
    public class VRCameraInteraction : MonoBehaviour
    {
        [Header("VR Camera Reference")]
        [SerializeField] private VRCamera vrCamera;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference triggerAction;
        [SerializeField] private InputActionReference gripAction;
        [SerializeField] private InputActionReference primaryButtonAction;
        [SerializeField] private InputActionReference secondaryButtonAction;
        [SerializeField] private InputActionReference joystickAction;
        [SerializeField] private InputActionReference thumbstickAction;

        [Header("Camera Settings")]
        [SerializeField] private bool enableHapticFeedback = true;
        [SerializeField] private float hapticIntensity = 0.3f;
        [SerializeField] private float zoomSensitivity = 50f;

        [Header("Events")]
        public UnityEvent onPhotoCaptured;
        public UnityEvent onZoomIn;
        public UnityEvent onZoomOut;
        public UnityEvent onSettingsMenuToggle;

        // Private variables
        private VRCameraRig vrCameraRig;
        private ActionBasedController rightController;
        private bool isGripping = false;
        private Vector2 lastJoystickPosition;
        private float photoCooldown = 0f;

        private void Awake()
        {
            FindComponents();
            SetupInputActions();
        }

        private void Start()
        {
            InitializeController();
        }

        private void Update()
        {
            HandlePhotoCapture();
            HandleZoomControl();
            HandleSettingsMenu();
            HandleGripControl();
            UpdatePhotoCooldown();
        }

        private void FindComponents()
        {
            // 查找VR相机
            if (vrCamera == null)
            {
                vrCamera = GetComponentInParent<VRCamera>();
            }

            // 查找VR相机系统
            vrCameraRig = FindObjectOfType<VRCameraRig>();

            // 查找右手控制器
            if (vrCameraRig != null)
            {
                rightController = vrCameraRig.RightController;
            }
        }

        private void SetupInputActions()
        {
            // 设置输入动作回调
            if (triggerAction != null)
            {
                triggerAction.action.started += OnTriggerPressed;
                triggerAction.action.canceled += OnTriggerReleased;
            }

            if (gripAction != null)
            {
                gripAction.action.started += OnGripPressed;
                gripAction.action.canceled += OnGripReleased;
            }

            if (primaryButtonAction != null)
            {
                primaryButtonAction.action.started += OnPrimaryButtonPressed;
            }

            if (secondaryButtonAction != null)
            {
                secondaryButtonAction.action.started += OnSecondaryButtonPressed;
            }
        }

        private void InitializeController()
        {
            // 确保控制器已激活
            if (rightController != null)
            {
                Debug.Log("右手控制器已初始化");
            }
        }

        private void HandlePhotoCapture()
        {
            // 检查触发器按下（拍照）
            if (triggerAction != null && triggerAction.action.ReadValue<float>() > 0.5f && photoCooldown <= 0f)
            {
                CapturePhoto();
                photoCooldown = 0.5f; // 防止连拍

                // 触觉反馈
                if (enableHapticFeedback && rightController != null)
                {
                    SendHapticFeedback(0.5f);
                }
            }
        }

        private void HandleZoomControl()
        {
            // 使用摇杆或拇指摇杆控制变焦
            Vector2 joystickPosition = Vector2.zero;

            if (joystickAction != null)
            {
                joystickPosition = joystickAction.action.ReadValue<Vector2>();
            }
            else if (thumbstickAction != null)
            {
                joystickPosition = thumbstickAction.action.ReadValue<Vector2>();
            }

            // 垂直轴控制变焦
            if (Mathf.Abs(joystickPosition.y) > 0.1f)
            {
                float zoomDelta = joystickPosition.y * zoomSensitivity * Time.deltaTime;
                vrCamera?.AdjustZoom(zoomDelta);

                // 变焦触觉反馈
                if (enableHapticFeedback && rightController && Time.frameCount % 5 == 0)
                {
                    SendHapticFeedback(Mathf.Abs(joystickPosition.y) * 0.1f);
                }
            }

            lastJoystickPosition = joystickPosition;
        }

        private void HandleSettingsMenu()
        {
            // 主按钮切换设置菜单
            if (primaryButtonAction != null && primaryButtonAction.action.WasPressedThisFrame())
            {
                onSettingsMenuToggle?.Invoke();
                SendHapticFeedback(0.2f);
            }
        }

        private void HandleGripControl()
        {
            // 握持键用于稳固相机
            if (gripAction != null)
            {
                float gripValue = gripAction.action.ReadValue<float>();
                bool currentlyGripping = gripValue > 0.5f;

                if (currentlyGripping != isGripping)
                {
                    isGripping = currentlyGripping;

                    if (isGripping)
                    {
                        Debug.Log("相机握持稳固");
                        // 可以在这里添加相机稳定效果
                    }
                }
            }
        }

        private void UpdatePhotoCooldown()
        {
            if (photoCooldown > 0f)
            {
                photoCooldown -= Time.deltaTime;
            }
        }

        // Input Event Handlers

        private void OnTriggerPressed(InputAction.CallbackContext context)
        {
            // 在HandlePhotoCapture中处理
        }

        private void OnTriggerReleased(InputAction.CallbackContext context)
        {
            // 可以在这里添加释放快门的动画或效果
        }

        private void OnGripPressed(InputAction.CallbackContext context)
        {
            Debug.Log("握持相机");
        }

        private void OnGripReleased(InputAction.CallbackContext context)
        {
            Debug.Log("释放相机");
        }

        private void OnPrimaryButtonPressed(InputAction.CallbackContext context)
        {
            Debug.Log("主按钮按下 - 打开设置菜单");
        }

        private void OnSecondaryButtonPressed(InputAction.CallbackContext context)
        {
            Debug.Log("副按钮按下 - 切换拍照/录像模式");

            // 切换拍照/录像模式
            if (vrCamera != null)
            {
                vrCamera.IsPhotoMode = !vrCamera.IsPhotoMode;
                Debug.Log($"切换到{(vrCamera.IsPhotoMode ? "拍照" : "录像")}模式");
            }

            SendHapticFeedback(0.3f);
        }

        // Public Methods

        /// <summary>
        /// 拍照
        /// </summary>
        public void CapturePhoto()
        {
            if (vrCamera != null && photoCooldown <= 0f)
            {
                vrCamera.CapturePhoto();
                onPhotoCaptured?.Invoke();

                // 拍照效果
                PlayCaptureEffect();
            }
        }

        /// <summary>
        /// 手动调整变焦
        /// </summary>
        public void ManualZoom(float direction)
        {
            vrCamera?.AdjustZoom(direction);

            if (direction > 0)
                onZoomIn?.Invoke();
            else if (direction < 0)
                onZoomOut?.Invoke();
        }

        /// <summary>
        /// 设置焦距
        /// </summary>
        public void SetFocalLength(float focalLength)
        {
            vrCamera?.SetFocalLength(focalLength);
        }

        /// <summary>
        /// 发送触觉反馈
        /// </summary>
        private void SendHapticFeedback(float intensity)
        {
            if (rightController != null && enableHapticFeedback)
            {
                var haptic = rightController.GetComponent<XRDirectInteractor>();
                if (haptic != null)
                {
                    // 发送触觉反馈
                    rightController.SendHapticImpulse(intensity * hapticIntensity, 0.1f);
                }
            }
        }

        /// <summary>
        /// 播放拍照效果
        /// </summary>
        private void PlayCaptureEffect()
        {
            // 闪光灯效果
            var flashEffect = GetComponentInChildren<CameraFlashEffect>();
            if (flashEffect != null)
            {
                flashEffect.TriggerFlash();
            }

            // 快门声音
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        private void OnDestroy()
        {
            // 清理输入动作
            if (triggerAction != null)
            {
                triggerAction.action.started -= OnTriggerPressed;
                triggerAction.action.canceled -= OnTriggerReleased;
            }

            if (gripAction != null)
            {
                gripAction.action.started -= OnGripPressed;
                gripAction.action.canceled -= OnGripReleased;
            }

            if (primaryButtonAction != null)
            {
                primaryButtonAction.action.started -= OnPrimaryButtonPressed;
            }

            if (secondaryButtonAction != null)
            {
                secondaryButtonAction.action.started -= OnSecondaryButtonPressed;
            }
        }
    }
}