using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// VR主菜单控制器
/// 集成Meta Building Blocks和VR交互功能
/// </summary>
public class VRMenuController : MonoBehaviour
{
    [Header("场景配置")]
    public SceneItem[] sceneItems;

    [Header("UI配置")]
    public GameObject sceneInfoPanel;
    public TMP_Text sceneNameText;
    public Image previewImage;
    public Image backgroundImage;

    [Header("交互设置")]
    public float grabAnimationDuration = 0.3f;
    public float previewDisplayDelay = 0.5f;
    public Vector3 previewOffset = new Vector3(0, 0.1f, 0.2f);
    public float previewScale = 0.8f;

    [Header("返回动画设置")]
    public float returnToOriginDuration = 0.8f;
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool enableFloatingOnReturn = true;

    [Header("菜单返回设置")]
    public float menuHoldDuration = 3f;
    public GameObject menuFeedbackPanel;
    public Image menuProgressBar;
    public TMP_Text menuFeedbackText;

    [Header("手柄设置")]
    public Transform rightHandAnchor;
    public Transform leftHandAnchor;

    [Header("音效")]
    public AudioClip grabSound;
    public AudioClip selectSound;
    public AudioClip returnSound;

    private AudioSource audioSource;
    private float menuHoldTimer = 0f;
    private bool isMenuPressed = false;
    private Transform currentGrabbedItem;
    private SceneItem currentSceneItem;

    [System.Serializable]
    public class SceneItem
    {
        public string sceneName;
        public string displayName;
        public Sprite previewSprite;
        public GameObject itemPrefab;
        public Transform spawnPoint;
        public bool isUnlocked = true;
    }

    void Start()
    {
        InitializeController();
        SpawnSceneItems();
    }

    void Update()
    {
        HandleMenuReturn();
        HandleSceneSelection();
    }

    /// <summary>
    /// 初始化控制器
    /// </summary>
    private void InitializeController()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 隐藏UI面板
        if (sceneInfoPanel) sceneInfoPanel.SetActive(false);
        if (menuFeedbackPanel) menuFeedbackPanel.SetActive(false);

        Debug.Log("VRMenuController initialized");
    }

    /// <summary>
    /// 生成场景物品
    /// </summary>
    private void SpawnSceneItems()
    {
        foreach (var sceneItem in sceneItems)
        {
            if (!sceneItem.isUnlocked || sceneItem.itemPrefab == null) continue;

            GameObject item = Instantiate(sceneItem.itemPrefab);

            // 设置位置
            if (sceneItem.spawnPoint != null)
            {
                item.transform.position = sceneItem.spawnPoint.position;
                item.transform.rotation = sceneItem.spawnPoint.rotation;
            }

            // PortalGrab脚本已被移除，使用新的VRGrabbable系统

            // 添加VR交互事件
            VRGrabbable grabbable = item.GetComponent<VRGrabbable>();
            if (grabbable == null)
            {
                grabbable = item.AddComponent<VRGrabbable>();
            }

            grabbable.Initialize(this, sceneItem);

            // 添加悬浮效果
            AddFloatingEffect(item);
        }
    }

    /// <summary>
    /// 添加悬浮动画效果
    /// </summary>
    public void AddFloatingEffect(GameObject item)
    {
        if (item == null) return;

        // 停止之前的动画
        item.transform.DOKill();

        // 悬浮动画
        item.transform.DOMoveY(item.transform.position.y + 0.05f, 2f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // 轻微旋转
        item.transform.DORotate(new Vector3(0, 360, 0), 20f, RotateMode.WorldAxisAdd)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

  
    /// <summary>
    /// 物品被抓住时调用
    /// </summary>
    public void OnItemGrabbed(GameObject item, SceneItem sceneItem)
    {
        currentGrabbedItem = item.transform;
        currentSceneItem = sceneItem;

        // 播放抓取音效
        if (grabSound) audioSource.PlayOneShot(grabSound);

        // 显示场景信息面板
        StartCoroutine(ShowSceneInfoDelayed(sceneItem));

        // 将物品移到手柄前方
        MoveItemToHand(item);

        Debug.Log($"Grabbed item: {sceneItem.displayName}");
    }

    /// <summary>
    /// 物品被松开时调用
    /// </summary>
    public void OnItemReleased(GameObject item)
    {
        currentGrabbedItem = null;
        currentSceneItem = null;

        // 隐藏场景信息面板
        if (sceneInfoPanel)
        {
            sceneInfoPanel.SetActive(false);
        }

        // 返回原位动画
        ReturnItemToOriginalPosition(item);

        Debug.Log("Released item");
    }

    /// <summary>
    /// 延迟显示场景信息
    /// </summary>
    private IEnumerator ShowSceneInfoDelayed(SceneItem sceneItem)
    {
        yield return new WaitForSeconds(previewDisplayDelay);

        if (sceneInfoPanel)
        {
            sceneInfoPanel.SetActive(true);

            // 设置场景名称
            if (sceneNameText)
            {
                sceneNameText.text = sceneItem.displayName;
            }

            // 设置预览图
            if (previewImage && sceneItem.previewSprite)
            {
                previewImage.sprite = sceneItem.previewSprite;
                previewImage.DOFade(1f, 0.3f);
            }

            // 设置背景
            if (backgroundImage)
            {
                backgroundImage.DOFade(0.8f, 0.3f);
            }

            // 面板出现动画
            sceneInfoPanel.transform.localScale = Vector3.zero;
            sceneInfoPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
    }

    /// <summary>
    /// 将物品移到手柄位置
    /// </summary>
    private void MoveItemToHand(GameObject item)
    {
        if (rightHandAnchor != null)
        {
            Vector3 targetPosition = rightHandAnchor.position +
                                   rightHandAnchor.rotation * previewOffset;

            // 停止之前的动画
            item.transform.DOKill();

            // 移动到手柄前方
            item.transform.DOMove(targetPosition, grabAnimationDuration)
                .SetEase(Ease.OutCubic);

            // 调整大小和方向
            item.transform.DOScale(previewScale, grabAnimationDuration);
            item.transform.DOLookAt(rightHandAnchor.position, grabAnimationDuration);
        }
    }

    /// <summary>
    /// 物品返回原位
    /// </summary>
    private void ReturnItemToOriginalPosition(GameObject item)
    {
        VRGrabbable grabbable = item.GetComponent<VRGrabbable>();
        if (grabbable != null)
        {
            // 使用VRGrabbable的返回逻辑
            grabbable.ResetToOriginalPosition();
        }
        else
        {
            // 默认返回动画
            item.transform.DOScale(1f, grabAnimationDuration);
            AddFloatingEffect(item);
        }
    }

    /// <summary>
    /// 处理菜单返回功能
    /// </summary>
    private void HandleMenuReturn()
    {
        // 检测菜单键按下 (OVRInput.Menu)
        bool menuPressed = OVRInput.Get(OVRInput.Button.Start) ||
                          OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);

        if (menuPressed && !isMenuPressed)
        {
            isMenuPressed = true;
            menuHoldTimer = 0f;

            // 显示反馈面板
            if (menuFeedbackPanel)
            {
                menuFeedbackPanel.SetActive(true);
                menuFeedbackText.text = "按住返回主菜单...";

                if (menuProgressBar)
                {
                    menuProgressBar.fillAmount = 0f;
                }
            }
        }
        else if (!menuPressed && isMenuPressed)
        {
            isMenuPressed = false;

            // 隐藏反馈面板
            if (menuFeedbackPanel)
            {
                menuFeedbackPanel.SetActive(false);
            }

            menuHoldTimer = 0f;
        }

        // 持续按住菜单键
        if (isMenuPressed)
        {
            menuHoldTimer += Time.deltaTime;

            // 更新进度条
            if (menuProgressBar)
            {
                menuProgressBar.fillAmount = menuHoldTimer / menuHoldDuration;
            }

            // 检查是否达到返回时间
            if (menuHoldTimer >= menuHoldDuration)
            {
                ReturnToMainMenu();
            }
        }
    }

    /// <summary>
    /// 处理场景选择
    /// </summary>
    private void HandleSceneSelection()
    {
        // 检测A键按下进行场景切换
        if (currentGrabbedItem != null && currentSceneItem != null)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.A) ||
                OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                LoadScene(currentSceneItem);
            }
        }
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    private void LoadScene(SceneItem sceneItem)
    {
        if (string.IsNullOrEmpty(sceneItem.sceneName))
        {
            Debug.LogError("Scene name is empty!");
            return;
        }

        // 播放选择音效
        if (selectSound) audioSource.PlayOneShot(selectSound);

        Debug.Log($"Loading scene: {sceneItem.sceneName}");

        // 淡出效果
        if (backgroundImage)
        {
            backgroundImage.DOFade(1f, 0.5f).OnComplete(() => {
                SceneManager.LoadSceneAsync(sceneItem.sceneName);
            });
        }
        else
        {
            SceneManager.LoadSceneAsync(sceneItem.sceneName);
        }
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    private void ReturnToMainMenu()
    {
        // 播放返回音效
        if (returnSound) audioSource.PlayOneShot(returnSound);

        Debug.Log("Returning to main menu");

        // 重置计时器
        menuHoldTimer = 0f;
        isMenuPressed = false;

        // 隐藏反馈面板
        if (menuFeedbackPanel)
        {
            menuFeedbackPanel.SetActive(false);
        }

        // 如果已经在主菜单，则不需要再加载
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            return;
        }

        // 返回主菜单
        SceneManager.LoadSceneAsync("MainMenu");
    }

    /// <summary>
    /// 设置手柄锚点
    /// </summary>
    public void SetHandAnchors(Transform rightHand, Transform leftHand)
    {
        rightHandAnchor = rightHand;
        leftHandAnchor = leftHand;
    }

    /// <summary>
    /// 获取当前抓取的物品
    /// </summary>
    public SceneItem GetCurrentSceneItem()
    {
        return currentSceneItem;
    }

    void OnDestroy()
    {
        // 清理DOTween动画
        transform.DOKill();
    }
}