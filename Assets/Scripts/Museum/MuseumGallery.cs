using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace VRPhotography
{
    /// <summary>
    /// 博物馆照片展示系统
    /// 在Museum场景中展示拍摄的照片
    /// </summary>
    public class MuseumGallery : MonoBehaviour
    {
        [Header("Gallery Setup")]
        [SerializeField] private Transform galleryArea;
        [SerializeField] private GameObject photoFramePrefab;
        [SerializeField] private Transform[] photoFramePositions;
        
        [Header("Display Settings")]
        [SerializeField] private float photoSpacing = 2f;
        [SerializeField] private float frameWidth = 1.2f;
        [SerializeField] private float frameHeight = 0.8f;
        [SerializeField] private float viewingDistance = 3f;
        
        [Header("UI Elements")]
        [SerializeField] private GameObject photoInfoPanel;
        [SerializeField] private TextMeshProUGUI photoTitle;
        [SerializeField] private TextMeshProUGUI photoDescription;
        [SerializeField] private TextMeshProUGUI photoMetadata;
        [SerializeField] private TextMeshProUGUI photoCount;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button slideshowButton;
        [SerializeField] private Button returnButton;
        
        [Header("Navigation")]
        [SerializeField] private bool enableAutoNavigation = true;
        [SerializeField] private float autoNavigateInterval = 10f;
        [SerializeField] private bool enableTeleportation = true;
        
        [Header("Photo Loading")]
        [SerializeField] private bool loadPhotosOnStart = true;
        [SerializeField] private int maxPhotosPerWall = 20;
        
        [Header("VR Interaction")]
        [SerializeField] private float interactionDistance = 2f;
        [SerializeField] private bool highlightOnHover = true;
        [SerializeField] private Color highlightColor = Color.yellow;
        
        // Private variables
        private VRPhotoCapture photoCapture;
        private List<GalleryPhotoFrame> photoFrames = new List<GalleryPhotoFrame>();
        private int currentPhotoIndex = 0;
        private bool isSlideshowActive = false;
        private bool isGalleryReady = false;
        private Coroutine slideshowCoroutine;
        
        // Events
        public UnityEvent<VRPhotoCapture.CapturedPhoto> onPhotoSelected;
        public UnityEvent onGalleryReady;
        public UnityEvent onSlideshowStarted;
        public UnityEvent onSlideshowStopped;
        
        // Gallery photo frame component
        [System.Serializable]
        public class GalleryPhotoFrame
        {
            public GameObject frameObject;
            public Transform frameTransform;
            public Renderer frameRenderer;
            public RawImage photoDisplay;
            public Collider frameCollider;
            public Light frameLight;
            public VRPhotoCapture.CapturedPhoto photo;
            public Vector3 originalPosition;
            public Vector3 highlightedPosition;
            public bool isHighlighted = false;
            
            public GalleryPhotoFrame(GameObject frame)
            {
                frameObject = frame;
                frameTransform = frame.transform;
                frameRenderer = frame.GetComponent<Renderer>();
                frameCollider = frame.GetComponent<Collider>();
                
                // 查找照片显示组件
                photoDisplay = frame.GetComponentInChildren<RawImage>();
                
                // 查找光源
                frameLight = frame.GetComponentInChildren<Light>();
                
                originalPosition = frameTransform.position;
                highlightedPosition = originalPosition + frameTransform.forward * 0.1f;
            }
            
            public void SetPhoto(VRPhotoCapture.CapturedPhoto photoData)
            {
                photo = photoData;
                if (photoDisplay != null && photoData.thumbnail != null)
                {
                    photoDisplay.texture = photoData.thumbnail;
                }
            }
            
            public void SetHighlighted(bool highlighted)
            {
                isHighlighted = highlighted;
                
                if (frameRenderer != null)
                {
                    frameRenderer.material.color = highlighted ? highlightColor : Color.white;
                }
                
                if (frameLight != null)
                {
                    frameLight.intensity = highlighted ? 2f : 1f;
                }
                
                // 移动相框位置
                if (frameTransform != null)
                {
                    frameTransform.position = highlighted ? highlightedPosition : originalPosition;
                }
            }
        }
        
        public List<GalleryPhotoFrame> PhotoFrames => photoFrames;
        public bool IsGalleryReady => isGalleryReady;
        public bool IsSlideshowActive => isSlideshowActive;
        
        private void Awake()
        {
            FindComponents();
            SetupGallery();
        }
        
        private void Start()
        {
            SetupUI();
            LoadPhotos();
        }
        
        private void Update()
        {
            HandleInput();
            UpdatePhotoInfo();
        }
        
        private void FindComponents()
        {
            // 查找照片捕获系统
            photoCapture = FindObjectOfType<VRPhotoCapture>();
            
            // 如果没有指定相框位置，创建默认布局
            if (photoFramePositions == null || photoFramePositions.Length == 0)
            {
                CreateDefaultLayout();
            }
        }
        
        private void CreateDefaultLayout()
        {
            var positions = new List<Transform>();
            
            // 创建墙面布局
            for (int wall = 0; wall < 4; wall++)
            {
                for (int i = 0; i < 5; i++)
                {
                    var positionObj = new GameObject($"FramePos_Wall{wall}_Pos{i}");
                    positionObj.transform.SetParent(galleryArea);
                    
                    // 计算墙面位置
                    float angle = wall * 90f;
                    Vector3 wallDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    Vector3 wallPosition = wallDirection * viewingDistance;
                    
                    // 在墙面上排列相框
                    float xOffset = (i - 2f) * photoSpacing;
                    Vector3 framePosition = wallPosition + Quaternion.Euler(0, angle, 0) * new Vector3(xOffset, 0, 0);
                    
                    positionObj.transform.position = framePosition;
                    positionObj.transform.rotation = Quaternion.LookRotation(-wallDirection);
                    
                    positions.Add(positionObj.transform);
                }
            }
            
            photoFramePositions = positions.ToArray();
        }
        
        private void SetupGallery()
        {
            // 清空现有的相框
            ClearGallery();
            
            // 创建相框
            for (int i = 0; i < photoFramePositions.Length; i++)
            {
                CreatePhotoFrame(photoFramePositions[i], i);
            }
            
            Debug.Log($"创建了 {photoFrames.Count} 个相框");
        }
        
        private void CreatePhotoFrame(Transform position, int index)
        {
            if (photoFramePrefab == null)
            {
                CreateDefaultFrame();
            }
            
            GameObject frameObject = Instantiate(photoFramePrefab, position.position, position.rotation);
            frameObject.name = $"PhotoFrame_{index:000}";
            frameObject.transform.SetParent(galleryArea);
            
            var galleryFrame = new GalleryPhotoFrame(frameObject);
            photoFrames.Add(galleryFrame);
            
            // 添加交互组件
            var photoInteractable = frameObject.AddComponent<PhotoFrameInteractable>();
            photoInteractable.Initialize(this, galleryFrame);
        }
        
        private void CreateDefaultFrame()
        {
            // 创建默认相框预制体
            photoFramePrefab = new GameObject("DefaultPhotoFrame");
            
            // 相框主体
            var frameBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameBody.transform.SetParent(photoFramePrefab.transform);
            frameBody.transform.localScale = new Vector3(frameWidth, frameHeight, 0.05f);
            frameBody.transform.localPosition = Vector3.zero;
            
            // 照片显示平面
            var photoPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            photoPlane.transform.SetParent(photoFramePrefab.transform);
            photoPlane.transform.localScale = new Vector3(frameWidth * 0.9f, frameHeight * 0.9f, 1f);
            photoPlane.transform.localPosition = Vector3.forward * 0.03f;
            
            // 添加照片显示组件
            var photoDisplay = photoPlane.AddComponent<RawImage>();
            
            // 添加碰撞器
            var collider = photoFramePrefab.AddComponent<BoxCollider>();
            collider.size = new Vector3(frameWidth, frameHeight, 0.1f);
            
            // 添加光源
            var lightObj = new GameObject("FrameLight");
            lightObj.transform.SetParent(photoFramePrefab.transform);
            lightObj.transform.localPosition = Vector3.up * 0.5f + Vector3.forward * 0.2f;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Spot;
            light.intensity = 1f;
            light.spotAngle = 45f;
            light.range = 2f;
            light.color = Color.white;
        }
        
        private void SetupUI()
        {
            // 设置按钮事件
            if (previousButton != null)
            {
                previousButton.onClick.AddListener(PreviousPhoto);
            }
            
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(NextPhoto);
            }
            
            if (slideshowButton != null)
            {
                slideshowButton.onClick.AddListener(ToggleSlideshow);
            }
            
            if (returnButton != null)
            {
                returnButton.onClick.AddListener(ReturnToMenu);
            }
            
            // 初始化UI状态
            UpdatePhotoCount();
            HidePhotoInfo();
        }
        
        private void LoadPhotos()
        {
            if (photoCapture == null)
            {
                Debug.LogWarning("未找到照片捕获系统");
                return;
            }
            
            // 加载已保存的照片
            photoCapture.LoadSavedPhotos();
            
            var photos = photoCapture.CapturedPhotos;
            Debug.Log($"加载了 {photos.Count} 张照片到画廊");
            
            // 将照片分配给相框
            for (int i = 0; i < photoFrames.Count && i < photos.Count; i++)
            {
                photoFrames[i].SetPhoto(photos[i]);
            }
            
            isGalleryReady = true;
            onGalleryReady?.Invoke();
            
            // 启动自动导航
            if (enableAutoNavigation && photos.Count > 0)
            {
                StartCoroutine(AutoNavigateGallery());
            }
        }
        
        private void HandleInput()
        {
            // VR输入处理
            // 这里可以添加VR控制器输入逻辑
            
            // 键盘输入（用于测试）
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PreviousPhoto();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextPhoto();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleSlideshow();
            }
        }
        
        private void UpdatePhotoInfo()
        {
            if (currentPhotoIndex >= 0 && currentPhotoIndex < photoFrames.Count)
            {
                var frame = photoFrames[currentPhotoIndex];
                if (frame.photo != null)
                {
                    UpdatePhotoInfoDisplay(frame.photo);
                }
            }
        }
        
        private void UpdatePhotoInfoDisplay(VRPhotoCapture.CapturedPhoto photo)
        {
            if (photoTitle != null)
            {
                photoTitle.text = $"照片 {currentPhotoIndex + 1}";
            }
            
            if (photoDescription != null)
            {
                photoDescription.text = $"拍摄时间: {photo.captureTime:yyyy-MM-dd HH:mm:ss}";
            }
            
            if (photoMetadata != null)
            {
                var metadata = photo.metadata;
                photoMetadata.text = $"焦距: {metadata.focalLength:F0}mm\n" +
                                   $"ISO: {metadata.iso}\n" +
                                   $"快门: 1/{metadata.shutterSpeed:F0}s\n" +
                                   $"光圈: f/{metadata.aperture:F1}";
            }
        }
        
        private void UpdatePhotoCount()
        {
            if (photoCount != null && photoCapture != null)
            {
                photoCount.text = $"{photoCapture.GetPhotoCount()} 张照片";
            }
        }
        
        // Gallery Navigation
        
        public void SelectPhoto(int index)
        {
            if (index < 0 || index >= photoFrames.Count)
                return;
            
            // 取消之前的高亮
            if (currentPhotoIndex >= 0 && currentPhotoIndex < photoFrames.Count)
            {
                photoFrames[currentPhotoIndex].SetHighlighted(false);
            }
            
            // 高亮新选中的照片
            currentPhotoIndex = index;
            photoFrames[currentPhotoIndex].SetHighlighted(true);
            
            ShowPhotoInfo();
            onPhotoSelected?.Invoke(photoFrames[currentPhotoIndex].photo);
        }
        
        public void NextPhoto()
        {
            int nextIndex = (currentPhotoIndex + 1) % photoFrames.Count;
            SelectPhoto(nextIndex);
        }
        
        public void PreviousPhoto()
        {
            int prevIndex = (currentPhotoIndex - 1 + photoFrames.Count) % photoFrames.Count;
            SelectPhoto(prevIndex);
        }
        
        public void ShowPhotoInfo()
        {
            if (photoInfoPanel != null)
            {
                photoInfoPanel.SetActive(true);
            }
        }
        
        public void HidePhotoInfo()
        {
            if (photoInfoPanel != null)
            {
                photoInfoPanel.SetActive(false);
            }
        }
        
        // Slideshow
        
        public void ToggleSlideshow()
        {
            if (isSlideshowActive)
            {
                StopSlideshow();
            }
            else
            {
                StartSlideshow();
            }
        }
        
        public void StartSlideshow()
        {
            if (isSlideshowActive) return;
            
            isSlideshowActive = true;
            slideshowCoroutine = StartCoroutine(SlideshowCoroutine());
            onSlideshowStarted?.Invoke();
            
            if (slideshowButton != null)
            {
                var buttonText = slideshowButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "停止幻灯片";
                }
            }
        }
        
        public void StopSlideshow()
        {
            if (!isSlideshowActive) return;
            
            isSlideshowActive = false;
            
            if (slideshowCoroutine != null)
            {
                StopCoroutine(slideshowCoroutine);
                slideshowCoroutine = null;
            }
            
            onSlideshowStopped?.Invoke();
            
            if (slideshowButton != null)
            {
                var buttonText = slideshowButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "开始幻灯片";
                }
            }
        }
        
        private IEnumerator SlideshowCoroutine()
        {
            while (isSlideshowActive)
            {
                yield return new WaitForSeconds(autoNavigateInterval);
                
                if (isSlideshowActive)
                {
                    NextPhoto();
                }
            }
        }
        
        private IEnumerator AutoNavigateGallery()
        {
            yield return new WaitForSeconds(2f); // 等待画廊加载完成
            
            int photoIndex = 0;
            var photos = photoCapture?.CapturedPhotos;
            
            if (photos == null || photos.Count == 0)
                yield break;
            
            while (enableAutoNavigation && photoIndex < photos.Count)
            {
                SelectPhoto(photoIndex);
                yield return new WaitForSeconds(3f); // 每张照片展示3秒
                photoIndex++;
            }
        }
        
        // Gallery Management
        
        public void ClearGallery()
        {
            foreach (var frame in photoFrames)
            {
                if (frame.frameObject != null)
                {
                    DestroyImmediate(frame.frameObject);
                }
            }
            
            photoFrames.Clear();
            currentPhotoIndex = 0;
        }
        
        public void RefreshGallery()
        {
            LoadPhotos();
            UpdatePhotoCount();
        }
        
        public void ReturnToMenu()
        {
            // 停止幻灯片
            StopSlideshow();
            
            // 返回主菜单
            Debug.Log("返回主菜单");
            // 这里可以添加场景切换逻辑
            // UnityEngine.SceneManagement.SceneManager.LoadScene("WillsRoom_HDRP");
        }
        
        // Private helper class for photo frame interaction
        private class PhotoFrameInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            private MuseumGallery gallery;
            private GalleryPhotoFrame galleryFrame;
            
            public void Initialize(MuseumGallery galleryRef, GalleryPhotoFrame frameRef)
            {
                gallery = galleryRef;
                galleryFrame = frameRef;
            }
            
            public void OnPointerEnter(PointerEventData eventData)
            {
                if (gallery != null && galleryFrame != null)
                {
                    galleryFrame.SetHighlighted(true);
                }
            }
            
            public void OnPointerExit(PointerEventData eventData)
            {
                if (gallery != null && galleryFrame != null && gallery.currentPhotoIndex != gallery.photoFrames.IndexOf(galleryFrame))
                {
                    galleryFrame.SetHighlighted(false);
                }
            }
            
            public void OnPointerClick(PointerEventData eventData)
            {
                if (gallery != null && galleryFrame != null)
                {
                    int index = gallery.photoFrames.IndexOf(galleryFrame);
                    gallery.SelectPhoto(index);
                }
            }
        }
    }
}