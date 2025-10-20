using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace VRPhotography
{
    /// <summary>
    /// VR手持相机系统
    /// 提供变焦、ISO、快门速度等摄影功能
    /// </summary>
    public class VRCamera : MonoBehaviour
    {
        [Header("Camera Components")]
        [SerializeField] private Camera captureCamera;
        [SerializeField] private Transform cameraBody;
        [SerializeField] private Transform lensTransform;
        [SerializeField] private RawImage viewfinderDisplay;
        
        [Header("Photography Settings")]
        [SerializeField] private float minFocalLength = 24f;
        [SerializeField] private float maxFocalLength = 200f;
        [SerializeField] private float currentFocalLength = 50f;
        
        [Range(100, 6400)]
        [SerializeField] private int iso = 100;
        
        [Range(1f, 8000f)]
        [SerializeField] private float shutterSpeed = 60f; // 1/60s
        
        [Range(1.4f, 22f)]
        [SerializeField] private float aperture = 2.8f;
        
        [Header("Zoom Controls")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float zoomSmoothTime = 0.1f;
        
        [Header("Camera Physics")]
        [SerializeField] private float cameraSize = 0.15f;
        [SerializeField] private float cameraWeight = 0.8f;
        
        // Private variables
        private VRCameraRig vrCameraRig;
        private float targetFocalLength;
        private float zoomVelocity;
        private RenderTexture viewfinderTexture;
        private bool isPhotoMode = true;
        
        // Properties
        public float CurrentFocalLength => currentFocalLength;
        public int ISO => iso;
        public float ShutterSpeed => shutterSpeed;
        public float Aperture => aperture;
        public Camera CaptureCamera => captureCamera;
        public bool IsPhotoMode { get => isPhotoMode; set => isPhotoMode = value; }
        
        private void Awake()
        {
            InitializeCamera();
        }
        
        private void Start()
        {
            SetupViewfinder();
            FindVRCameraRig();
        }
        
        private void Update()
        {
            UpdateCameraPosition();
            UpdateZoom();
            UpdateCameraSettings();
        }
        
        private void InitializeCamera()
        {
            // 获取或创建相机组件
            if (captureCamera == null)
            {
                captureCamera = GetComponentInChildren<Camera>();
            }
            
            if (captureCamera == null)
            {
                var cameraObj = new GameObject("CaptureCamera");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = Vector3.zero;
                cameraObj.transform.localRotation = Quaternion.identity;
                captureCamera = cameraObj.AddComponent<Camera>();
            }
            
            // 设置相机初始参数
            targetFocalLength = currentFocalLength;
            
            // 创建相机物理属性
            if (cameraBody == null)
                cameraBody = transform;
                
            if (lensTransform == null)
                lensTransform = transform.Find("Lens");
                
            if (lensTransform == null)
            {
                var lensObj = new GameObject("Lens");
                lensObj.transform.SetParent(transform);
                lensObj.transform.localPosition = Vector3.forward * 0.05f;
                lensTransform = lensObj.transform;
            }
        }
        
        private void SetupViewfinder()
        {
            // 创建取景器渲染纹理
            if (viewfinderDisplay != null && captureCamera != null)
            {
                viewfinderTexture = new RenderTexture(1920, 1080, 24);
                viewfinderTexture.Create();
                
                captureCamera.targetTexture = viewfinderTexture;
                viewfinderDisplay.texture = viewfinderTexture;
            }
        }
        
        private void FindVRCameraRig()
        {
            vrCameraRig = FindObjectOfType<VRCameraRig>();
        }
        
        private void UpdateCameraPosition()
        {
            if (vrCameraRig != null && vrCameraRig.IsVRReady())
            {
                // 将相机附加到右手控制器
                transform.position = vrCameraRig.GetRightHandPosition();
                transform.rotation = vrCameraRig.GetRightHandRotation();
            }
        }
        
        private void UpdateZoom()
        {
            // 平滑变焦
            if (Mathf.Abs(targetFocalLength - currentFocalLength) > 0.01f)
            {
                currentFocalLength = Mathf.SmoothDamp(currentFocalLength, targetFocalLength, 
                    ref zoomVelocity, zoomSmoothTime);
                UpdateFieldOfView();
            }
        }
        
        private void UpdateCameraSettings()
        {
            if (captureCamera == null) return;
            
            // 更新相机参数
            UpdateFieldOfView();
            UpdateExposure();
            UpdateDepthOfField();
        }
        
        private void UpdateFieldOfView()
        {
            // 将焦距转换为视场角 (基于35mm等效)
            float fov = Mathf.Atan(24f / (2f * currentFocalLength)) * 2f * Mathf.Rad2Deg;
            captureCamera.fieldOfView = fov;
            
            // 更新镜头模型视觉效果
            if (lensTransform != null)
            {
                float lensScale = Mathf.Lerp(0.8f, 1.2f, (currentFocalLength - minFocalLength) / (maxFocalLength - minFocalLength));
                lensTransform.localScale = new Vector3(lensScale, lensScale, 1f);
            }
        }
        
        private void UpdateExposure()
        {
            // 简化的曝光计算
            float exposureValue = CalculateEV();
            
            // 调整相机曝光（如果使用Post Processing）
            var volume = captureCamera.GetComponent<Volume>();
            if (volume != null && volume.profile.TryGet(out UnityEngine.Rendering.Universal.Exposure exposure))
            {
                exposure.fixedExposure.value = exposureValue;
            }
        }
        
        private float CalculateEV()
        {
            // EV = log2(N²/t) where N = aperture, t = shutter time
            float shutterTime = 1f / shutterSpeed;
            return Mathf.Log10(aperture * aperture / shutterTime) / Mathf.Log10(2f) - Mathf.Log10(iso / 100f) / Mathf.Log10(2f);
        }
        
        private void UpdateDepthOfField()
        {
            // 更新景深效果
            var volume = captureCamera.GetComponent<Volume>();
            if (volume != null && volume.profile.TryGet(out UnityEngine.Rendering.Universal.DepthOfField dof))
            {
                dof.focusDistance.value = 10f; // 简化为固定对焦距离
                dof.aperture.value = aperture;
            }
        }
        
        // Public Methods
        
        /// <summary>
        /// 调整变焦
        /// </summary>
        /// <param name="zoomDelta">变焦增量 (-1 to 1)</param>
        public void AdjustZoom(float zoomDelta)
        {
            targetFocalLength = Mathf.Clamp(targetFocalLength + zoomDelta * zoomSpeed, minFocalLength, maxFocalLength);
        }
        
        /// <summary>
        /// 设置焦距
        /// </summary>
        public void SetFocalLength(float focalLength)
        {
            targetFocalLength = Mathf.Clamp(focalLength, minFocalLength, maxFocalLength);
        }
        
        /// <summary>
        /// 设置ISO
        /// </summary>
        public void SetISO(int newISO)
        {
            iso = Mathf.Clamp(newISO, 100, 6400);
        }
        
        /// <summary>
        /// 设置快门速度
        /// </summary>
        public void SetShutterSpeed(float speed)
        {
            shutterSpeed = Mathf.Clamp(speed, 1f, 8000f);
        }
        
        /// <summary>
        /// 设置光圈
        /// </summary>
        public void SetAperture(float f)
        {
            aperture = Mathf.Clamp(f, 1.4f, 22f);
        }
        
        /// <summary>
        /// 拍照
        /// </summary>
        public void CapturePhoto()
        {
            if (captureCamera == null) return;
            
            // 实现拍照逻辑
            Debug.Log($"拍照参数: 焦距={currentFocalLength}mm, ISO={iso}, 快门=1/{shutterSpeed}s, 光圈=f/{aperture}");
            
            // 这里将调用具体的拍照保存功能
            var photoCapture = GetComponent<VRPhotoCapture>();
            if (photoCapture != null)
            {
                photoCapture.TakePhoto();
            }
        }
        
        private void OnDestroy()
        {
            // 清理渲染纹理
            if (viewfinderTexture != null)
            {
                viewfinderTexture.Release();
                Destroy(viewfinderTexture);
            }
        }
        
        private void OnValidate()
        {
            // 在Inspector中修改参数时实时更新
            if (Application.isPlaying)
            {
                targetFocalLength = currentFocalLength;
            }
        }
    }
}