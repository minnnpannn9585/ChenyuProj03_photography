using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using System.Collections;

namespace VRPhotography
{
    /// <summary>
    /// WillsRoom主菜单管理系统
    /// 处理场景切换和用户界面
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject aboutPanel;
        [SerializeField] private GameObject galleryPreviewPanel;
        
        [Header("Menu Items")]
        [SerializeField] private Button photographButton;
        [SerializeField] private Button museumButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button aboutButton;
        [SerializeField] private Button exitButton;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private TextMeshProUGUI photoCountText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Animator menuAnimator;
        
        [Header("VR Interaction")]
        [SerializeField] private Transform vrCameraTransform;
        [SerializeField] private float menuDistance = 3f;
        [SerializeField] private float menuHeight = 1.5f;
        [SerializeField] private bool followVRCamera = true;
        
        [Header("Scene Loading")]
        [SerializeField] private bool showLoadingScreen = true;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TextMeshProUGUI loadingText;
        
        [Header("Audio")]
        [SerializeField] private AudioSource menuAudioSource;
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip backgroundMusic;
        
        [Header("Gallery Preview")]
        [SerializeField] private RawImage[] photoPreviewSlots;
        [SerializeField] private int maxPreviewPhotos = 4;
        
        // Private variables
        private VRCameraRig vrCameraRig;
        private VRPhotoCapture photoCapture;
        private bool isMenuActive = true;
        private Vector3 targetMenuPosition;
        private Quaternion targetMenuRotation;
        
        // Events
        public UnityEvent onMainMenuOpened;
        public UnityEvent onMainMenuClosed;
        public UnityEvent<string> onSceneLoadStarted;
        public UnityEvent<string> onSceneLoadCompleted;
        
        // Menu states
        public enum MenuState
        {
            Main,
            Settings,
            About,
            GalleryPreview,
            Hidden
        }
        
        private MenuState currentMenuState = MenuState.Main;
        
        public MenuState CurrentMenuState => currentMenuState;
        public bool IsMenuActive => isMenuActive;
        
        private void Awake()
        {
            FindComponents();
            InitializeMenu();
        }
        
        private void Start()
        {
            SetupButtonListeners();
            LoadGalleryPreviews();
            PlayBackgroundMusic();
            UpdateUIPosition();
        }
        
        private void Update()
        {
            if (isMenuActive && followVRCamera)
            {
                UpdateMenuPosition();
            }
            
            HandleInput();
        }
        
        private void FindComponents()
        {
            // 查找VR相机系统
            vrCameraRig = FindObjectOfType<VRCameraRig>();
            
            // 查找照片捕获系统
            photoCapture = FindObjectOfType<VRPhotoCapture>();
            
            // 设置VR相机变换
            if (vrCameraRig != null && vrCameraRig.XROrigin != null)
            {
                vrCameraTransform = vrCameraRig.XROrigin.Camera.transform;
            }
        }
        
        private void InitializeMenu()
        {
            // 设置版本信息
            if (versionText != null)
            {
                versionText.text = $"版本 {Application.version}";
            }
            
            // 设置初始菜单状态
            SetMenuState(MenuState.Main);
            
            // 隐藏加载界面
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
            
            Debug.Log("主菜单已初始化");
        }
        
        private void SetupButtonListeners()
        {
            // 主菜单按钮
            if (photographButton != null)
            {
                photographButton.onClick.AddListener(() => OnPhotographyModeSelected());
                AddButtonSound(photographButton);
            }
            
            if (museumButton != null)
            {
                museumButton.onClick.AddListener(() => OnMuseumModeSelected());
                AddButtonSound(museumButton);
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() => OnSettingsSelected());
                AddButtonSound(settingsButton);
            }
            
            if (aboutButton != null)
            {
                aboutButton.onClick.AddListener(() => OnAboutSelected());
                AddButtonSound(aboutButton);
            }
            
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(() => OnExitSelected());
                AddButtonSound(exitButton);
            }
        }
        
        private void AddButtonSound(Button button)
        {
            var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>() ?? 
                              button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            // 悬停声音
            var hoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            hoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            hoverEntry.callback.AddListener((data) => PlayHoverSound());
            eventTrigger.triggers.Add(hoverEntry);
            
            // 点击声音
            var clickEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            clickEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => PlayClickSound());
            eventTrigger.triggers.Add(clickEntry);
        }
        
        private void UpdateUIPosition()
        {
            if (vrCameraTransform == null) return;
            
            // 计算菜单位置
            Vector3 cameraPosition = vrCameraTransform.position;
            Vector3 cameraForward = vrCameraTransform.forward;
            
            targetMenuPosition = cameraPosition + cameraForward * menuDistance + Vector3.up * menuHeight;
            targetMenuRotation = Quaternion.LookRotation(targetMenuPosition - cameraPosition);
            
            transform.position = targetMenuPosition;
            transform.rotation = targetMenuRotation;
        }
        
        private void UpdateMenuPosition()
        {
            if (vrCameraTransform == null) return;
            
            Vector3 cameraPosition = vrCameraTransform.position;
            Vector3 cameraForward = vrCameraTransform.forward;
            
            targetMenuPosition = cameraPosition + cameraForward * menuDistance + Vector3.up * menuHeight;
            targetMenuRotation = Quaternion.LookRotation(targetMenuPosition - cameraPosition);
            
            // 平滑移动
            transform.position = Vector3.Lerp(transform.position, targetMenuPosition, Time.deltaTime * 5f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetMenuRotation, Time.deltaTime * 5f);
        }
        
        private void HandleInput()
        {
            // VR控制器输入处理
            // 这里可以添加特定的VR输入逻辑
            
            // 键盘输入（用于测试）
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleMenu();
            }
        }
        
        private void LoadGalleryPreviews()
        {
            if (photoCapture == null || photoPreviewSlots == null)
                return;
            
            var photos = photoCapture.CapturedPhotos;
            
            for (int i = 0; i < photoPreviewSlots.Length && i < photos.Count; i++)
            {
                if (photoPreviewSlots[i] != null && photos[i].thumbnail != null)
                {
                    photoPreviewSlots[i].texture = photos[i].thumbnail;
                    photoPreviewSlots[i].gameObject.SetActive(true);
                }
            }
            
            // 更新照片数量显示
            UpdatePhotoCount();
        }
        
        private void UpdatePhotoCount()
        {
            if (photoCountText != null && photoCapture != null)
            {
                int photoCount = photoCapture.GetPhotoCount();
                photoCountText.text = $"{photoCount} 张照片";
            }
        }
        
        // Button Event Handlers
        
        private void OnPhotographyModeSelected()
        {
            Debug.Log("选择摄影模式");
            LoadScene("PhotographExperience");
        }
        
        private void OnMuseumModeSelected()
        {
            Debug.Log("选择博物馆模式");
            LoadScene("Museum");
        }
        
        private void OnSettingsSelected()
        {
            Debug.Log("打开设置");
            SetMenuState(MenuState.Settings);
        }
        
        private void OnAboutSelected()
        {
            Debug.Log("打开关于");
            SetMenuState(MenuState.About);
        }
        
        private void OnExitSelected()
        {
            Debug.Log("退出应用");
            Application.Quit();
        }
        
        // Menu State Management
        
        public void SetMenuState(MenuState newState)
        {
            currentMenuState = newState;
            
            // 隐藏所有面板
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (aboutPanel != null)
                aboutPanel.SetActive(false);
            if (galleryPreviewPanel != null)
                galleryPreviewPanel.SetActive(false);
            
            // 显示当前面板
            switch (newState)
            {
                case MenuState.Main:
                    if (mainMenuPanel != null)
                        mainMenuPanel.SetActive(true);
                    break;
                case MenuState.Settings:
                    if (settingsPanel != null)
                        settingsPanel.SetActive(true);
                    break;
                case MenuState.About:
                    if (aboutPanel != null)
                        aboutPanel.SetActive(true);
                    break;
                case MenuState.GalleryPreview:
                    if (galleryPreviewPanel != null)
                        galleryPreviewPanel.SetActive(true);
                    break;
                case MenuState.Hidden:
                    // 所有面板都隐藏
                    break;
            }
            
            // 播放动画
            if (menuAnimator != null)
            {
                menuAnimator.SetTrigger("MenuStateChanged");
            }
        }
        
        public void ToggleMenu()
        {
            if (isMenuActive)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }
        
        public void ShowMenu()
        {
            isMenuActive = true;
            SetMenuState(MenuState.Main);
            onMainMenuOpened?.Invoke();
            
            // 播放显示动画
            if (menuAnimator != null)
            {
                menuAnimator.SetTrigger("ShowMenu");
            }
        }
        
        public void HideMenu()
        {
            isMenuActive = false;
            SetMenuState(MenuState.Hidden);
            onMainMenuClosed?.Invoke();
            
            // 播放隐藏动画
            if (menuAnimator != null)
            {
                menuAnimator.SetTrigger("HideMenu");
            }
        }
        
        public void ReturnToMainMenu()
        {
            SetMenuState(MenuState.Main);
        }
        
        // Scene Loading
        
        public void LoadScene(string sceneName)
        {
            Debug.Log($"加载场景: {sceneName}");
            onSceneLoadStarted?.Invoke(sceneName);
            
            if (showLoadingScreen)
            {
                StartCoroutine(LoadSceneAsync(sceneName));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            // 显示加载界面
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }
            
            // 开始异步加载
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;
            
            float progress = 0f;
            
            while (!asyncLoad.isDone)
            {
                progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                
                // 更新加载进度
                if (loadingSlider != null)
                {
                    loadingSlider.value = progress;
                }
                
                if (loadingText != null)
                {
                    loadingText.text = $"加载中... {progress * 100:F0}%";
                }
                
                // 当进度达到90%时激活场景
                if (progress >= 0.9f)
                {
                    if (loadingText != null)
                    {
                        loadingText.text = "按任意键继续...";
                    }
                    
                    // 等待用户输入或延迟后自动继续
                    yield return new WaitForSeconds(1f);
                    asyncLoad.allowSceneActivation = true;
                }
                
                yield return null;
            }
            
            onSceneLoadCompleted?.Invoke(sceneName);
        }
        
        // Audio Management
        
        private void PlayBackgroundMusic()
        {
            if (menuAudioSource != null && backgroundMusic != null)
            {
                menuAudioSource.clip = backgroundMusic;
                menuAudioSource.loop = true;
                menuAudioSource.volume = 0.3f;
                menuAudioSource.Play();
            }
        }
        
        private void PlayHoverSound()
        {
            if (menuAudioSource != null && hoverSound != null)
            {
                menuAudioSource.PlayOneShot(hoverSound);
            }
        }
        
        private void PlayClickSound()
        {
            if (menuAudioSource != null && clickSound != null)
            {
                menuAudioSource.PlayOneShot(clickSound);
            }
        }
        
        // Public Methods
        
        /// <summary>
        /// 设置菜单位置
        /// </summary>
        public void SetMenuPosition(Vector3 position, Quaternion rotation)
        {
            targetMenuPosition = position;
            targetMenuRotation = rotation;
            followVRCamera = false;
        }
        
        /// <summary>
        /// 设置是否跟随VR相机
        /// </summary>
        public void SetFollowVRCamera(bool follow)
        {
            followVRCamera = follow;
        }
        
        /// <summary>
        /// 刷新画廊预览
        /// </summary>
        public void RefreshGalleryPreviews()
        {
            LoadGalleryPreviews();
        }
        
        /// <summary>
        /// 获取当前菜单状态
        /// </summary>
        public string GetMenuStateString()
        {
            return currentMenuState.ToString();
        }
        
        private void OnDestroy()
        {
            // 清理音频
            if (menuAudioSource != null)
            {
                menuAudioSource.Stop();
            }
        }
    }
}