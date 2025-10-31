using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR移动控制器
/// 为浏览场景添加多种VR移动方式
/// </summary>
public class VRLocomotionController : MonoBehaviour
{
    [Header("移动设置")]
    public LocomotionType locomotionType = LocomotionType.Smooth;
    public float moveSpeed = 3f;
    public float rotationSpeed = 60f;
    public bool enableStrafe = true;
    public bool enableAutoRun = false;

    [Header("平滑移动")]
    public float acceleration = 10f;
    public float friction = 8f;
    public float gravity = -20f;
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 0.2f;

    [Header("瞬移移动")]
    public GameObject teleportReticle;
    public LineRenderer teleportArc;
    public float teleportMaxDistance = 10f;
    public float teleportMinDistance = 1f;
    public float teleportArcHeight = 2f;
    public bool showTeleportArc = true;

    [Header("手柄设置")]
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    public Transform headTransform; // VR头显位置

    [Header("菜单返回")]
    public float menuHoldDuration = 3f;
    public GameObject menuFeedbackPanel;
    public Image menuProgressBar;
    public TMP_Text menuFeedbackText;

    [Header("音效")]
    public AudioClip teleportSound;
    public AudioClip footstepSound;
    public AudioClip menuSound;
    public AudioSource audioSource;

    [Header("UI显示")]
    public GameObject movementInfoPanel;
    public TMP_Text movementModeText;
    public GameObject movementIndicators;

    // 移动状态
    private CharacterController characterController;
    private Vector3 velocity = Vector3.zero;
    private Vector3 moveInput = Vector3.zero;
    private bool isGrounded = false;

    // 瞬移状态
    private bool isTeleporting = false;
    private bool isTeleportAiming = false;
    private Vector3 teleportTarget;
    private RaycastHit teleportHit;

    // 菜单返回状态
    private bool isMenuPressed = false;
    private float menuHoldTimer = 0f;

    // 脚步系统
    private float footstepTimer = 0f;
    private float footstepInterval = 0.5f;

    // 移动类型枚举
    public enum LocomotionType
    {
        Smooth,    // 平滑移动
        Teleport,  // 瞬移
        Hybrid     // 混合模式
    }

    void Start()
    {
        InitializeController();
        SetupHandAnchors();
        InitializeTeleport();
        InitializeMenuReturn();
    }

    void Update()
    {
        HandleInput();
        HandleLocomotion();
        HandleMenuReturn();
        UpdateMovementInfo();
        HandleFootsteps();
    }

    /// <summary>
    /// 初始化控制器
    /// </summary>
    private void InitializeController()
    {
        // 获取或添加CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.3f;
            characterController.height = 1.8f;
            characterController.center = Vector3.up * 0.9f;
        }

        // 初始化音频源
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        Debug.Log("VRLocomotionController initialized");
    }

    /// <summary>
    /// 设置手柄锚点
    /// </summary>
    private void SetupHandAnchors()
    {
        // 自动查找手柄锚点
        if (leftHandAnchor == null || rightHandAnchor == null)
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                Transform parent = centerEye.transform.parent;

                if (leftHandAnchor == null)
                {
                    Transform leftHand = parent?.Find("LeftHandAnchor");
                    if (leftHand != null) leftHandAnchor = leftHand;
                }

                if (rightHandAnchor == null)
                {
                    Transform rightHand = parent?.Find("RightHandAnchor");
                    if (rightHand != null) rightHandAnchor = rightHand;
                }
            }
        }

        // 查找头显位置
        if (headTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                headTransform = mainCamera.transform;
            }
        }

        Debug.Log($"Hand Anchors - Left: {(leftHandAnchor != null ? "Found" : "Created")}, Right: {(rightHandAnchor != null ? "Found" : "Created")}");
    }

    /// <summary>
    /// 初始化瞬移系统
    /// </summary>
    private void InitializeTeleport()
    {
        if (locomotionType == LocomotionType.Teleport || locomotionType == LocomotionType.Hybrid)
        {
            // 创建瞬移指示器
            if (teleportReticle == null)
            {
                teleportReticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                teleportReticle.name = "TeleportReticle";
                teleportReticle.transform.localScale = new Vector3(0.3f, 0.01f, 0.3f);
                Renderer renderer = teleportReticle.GetComponent<Renderer>();
                renderer.material.color = new Color(0, 1, 1, 0.5f);

                // 添加碰撞体使其不会与玩家碰撞
                Collider collider = teleportReticle.GetComponent<Collider>();
                if (collider != null) collider.isTrigger = true;
            }

            // 创建瞬移弧线
            if (teleportArc == null)
            {
                teleportArc = gameObject.AddComponent<LineRenderer>();
                teleportArc.material = new Material(Shader.Find("Sprites/Default"));
                teleportArc.startWidth = 0.02f;
                teleportArc.endWidth = 0.01f;
                teleportArc.positionCount = 30;
                teleportArc.startColor = Color.cyan;
            teleportArc.endColor = Color.cyan;
                teleportArc.enabled = false;
            }
        }
    }

    /// <summary>
    /// 初始化菜单返回功能
    /// </summary>
    private void InitializeMenuReturn()
    {
        // 隐藏菜单反馈面板
        if (menuFeedbackPanel != null)
        {
            menuFeedbackPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        // 左手摇杆控制移动
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        moveInput = new Vector3(leftStick.x, 0, leftStick.y);

        // 右手摇杆控制旋转
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (Mathf.Abs(rightStick.x) > 0.1f)
        {
            transform.Rotate(Vector3.up, rightStick.x * rotationSpeed * Time.deltaTime);
        }

        // 瞬移瞄准（左手柄B键）
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
        {
            StartTeleportAim();
        }

        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger))
        {
            StopTeleportAim();
        }

        // 瞬移执行（左手柄A键）
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) && isTeleportAiming)
        {
            ExecuteTeleport();
        }
    }

    /// <summary>
    /// 处理移动
    /// </summary>
    private void HandleLocomotion()
    {
        switch (locomotionType)
        {
            case LocomotionType.Smooth:
                HandleSmoothLocomotion();
                break;

            case LocomotionType.Teleport:
                HandleTeleportLocomotion();
                break;

            case LocomotionType.Hybrid:
                // 根据移动速度决定使用哪种移动方式
                if (moveInput.magnitude > 0.1f)
                {
                    HandleSmoothLocomotion();
                }
                else
                {
                    HandleTeleportLocomotion();
                }
                break;
        }
    }

    /// <summary>
    /// 处理平滑移动
    /// </summary>
    private void HandleSmoothLocomotion()
    {
        // 根据头显方向调整移动方向
        Vector3 headForward = headTransform != null ? headTransform.forward : transform.forward;
        headForward.y = 0;
        headForward.Normalize();

        Vector3 headRight = headTransform != null ? headTransform.right : transform.right;
        headRight.y = 0;
        headRight.Normalize();

        // 计算移动方向
        Vector3 movementDirection = (headForward * moveInput.z + headRight * moveInput.x).normalized;

        // 应用加速度
        if (movementDirection.magnitude > 0.1f)
        {
            velocity += movementDirection * acceleration * Time.deltaTime;
        }
        else
        {
            // 应用摩擦力
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, friction * Time.deltaTime);
        }

        // 应用重力
        velocity.y += gravity * Time.deltaTime;

        // 地面检测
        CheckGrounded();

        // 移动
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// 处理瞬移移动
    /// </summary>
    private void HandleTeleportLocomotion()
    {
        // 更新瞬移瞄准
        if (isTeleportAiming)
        {
            UpdateTeleportAim();
        }
    }

    /// <summary>
    /// 开始瞬移瞄准
    /// </summary>
    private void StartTeleportAim()
    {
        if (locomotionType == LocomotionType.Smooth) return;

        isTeleportAiming = true;

        if (showTeleportArc)
        {
            teleportArc.enabled = true;
        }

        Debug.Log("Teleport aiming started");
    }

    /// <summary>
    /// 停止瞬移瞄准
    /// </summary>
    private void StopTeleportAim()
    {
        isTeleportAiming = false;

        if (showTeleportArc)
        {
            teleportArc.enabled = false;
        }

        if (teleportReticle != null)
        {
            teleportReticle.SetActive(false);
        }

        Debug.Log("Teleport aiming stopped");
    }

    /// <summary>
    /// 更新瞬移瞄准
    /// </summary>
    private void UpdateTeleportAim()
    {
        if (leftHandAnchor == null) return;

        // 发射射线检测瞬移目标
        Vector3 rayOrigin = leftHandAnchor.position;
        Vector3 rayDirection = leftHandAnchor.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out teleportHit, teleportMaxDistance))
        {
            // 检查距离是否在有效范围内
            float distance = Vector3.Distance(rayOrigin, teleportHit.point);
            if (distance >= teleportMinDistance && distance <= teleportMaxDistance)
            {
                teleportTarget = teleportHit.point;

                // 显示瞬移指示器
                if (teleportReticle != null)
                {
                    teleportReticle.SetActive(true);
                    teleportReticle.transform.position = teleportTarget;
                    teleportReticle.transform.up = teleportHit.normal;
                }

                // 更新瞬移弧线
                if (showTeleportArc && teleportArc != null)
                {
                    UpdateTeleportArc(rayOrigin, teleportTarget);
                }
            }
            else
            {
                // 距离太近或太远
                if (teleportReticle != null)
                {
                    teleportReticle.SetActive(false);
                }
            }
        }
        else
        {
            // 没有击中任何物体
            if (teleportReticle != null)
            {
                teleportReticle.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 更新瞬移弧线
    /// </summary>
    private void UpdateTeleportArc(Vector3 start, Vector3 end)
    {
        if (teleportArc == null) return;

        int pointCount = teleportArc.positionCount;
        teleportArc.positionCount = pointCount;

        Vector3[] points = new Vector3[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            Vector3 point = Vector3.Lerp(start, end, t);

            // 添加抛物线效果
            point.y += Mathf.Sin(t * Mathf.PI) * teleportArcHeight;

            points[i] = point;
        }

        teleportArc.SetPositions(points);
    }

    /// <summary>
    /// 执行瞬移
    /// </summary>
    private void ExecuteTeleport()
    {
        if (isTeleporting || !teleportReticle.activeSelf) return;

        isTeleporting = true;

        // 播放瞬移音效
        if (teleportSound != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }

        // 瞬移动画
        transform.DOMove(teleportTarget, 0.2f)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => {
                isTeleporting = false;
                StopTeleportAim();
            });

        Debug.Log($"Teleported to: {teleportTarget}");
    }

    /// <summary>
    /// 地面检测
    /// </summary>
    private void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(transform.position + Vector3.down * (characterController.height * 0.5f),
                                       groundCheckDistance, groundLayer);
    }

    /// <summary>
    /// 处理菜单返回功能
    /// </summary>
    private void HandleMenuReturn()
    {
        // 检测菜单键按下
        bool menuPressed = OVRInput.Get(OVRInput.Button.Start);

        if (menuPressed && !isMenuPressed)
        {
            isMenuPressed = true;
            menuHoldTimer = 0f;

            // 显示反馈面板
            if (menuFeedbackPanel != null)
            {
                menuFeedbackPanel.SetActive(true);
                if (menuFeedbackText != null)
                {
                    menuFeedbackText.text = "按住返回主菜单...";
                }

                if (menuProgressBar != null)
                {
                    menuProgressBar.fillAmount = 0f;
                }
            }
        }
        else if (!menuPressed && isMenuPressed)
        {
            isMenuPressed = false;

            // 隐藏反馈面板
            if (menuFeedbackPanel != null)
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
            if (menuProgressBar != null)
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
    /// 返回主菜单
    /// </summary>
    private void ReturnToMainMenu()
    {
        // 播放菜单音效
        if (menuSound != null)
        {
            audioSource.PlayOneShot(menuSound);
        }

        Debug.Log("Returning to main menu from museum");

        // 重置计时器
        menuHoldTimer = 0f;
        isMenuPressed = false;

        // 隐藏反馈面板
        if (menuFeedbackPanel != null)
        {
            menuFeedbackPanel.SetActive(false);
        }

        // 返回主菜单
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
    }

    /// <summary>
    /// 更新移动信息显示
    /// </summary>
    private void UpdateMovementInfo()
    {
        if (movementModeText == null) return;

        string modeText = "";
        switch (locomotionType)
        {
            case LocomotionType.Smooth:
                modeText = "平滑移动";
                break;
            case LocomotionType.Teleport:
                modeText = "瞬移移动";
                break;
            case LocomotionType.Hybrid:
                modeText = "混合移动";
                break;
        }

        movementModeText.text = $"移动模式: {modeText}";
    }

    /// <summary>
    /// 处理脚步声
    /// </summary>
    private void HandleFootsteps()
    {
        if (footstepSound == null) return;

        // 只在平滑移动且有地面接触时播放脚步声
        if (locomotionType == LocomotionType.Smooth && isGrounded && moveInput.magnitude > 0.1f)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                audioSource.PlayOneShot(footstepSound, 0.3f);
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    /// <summary>
    /// 切换移动类型
    /// </summary>
    public void SwitchLocomotionType()
    {
        switch (locomotionType)
        {
            case LocomotionType.Smooth:
                locomotionType = LocomotionType.Teleport;
                break;
            case LocomotionType.Teleport:
                locomotionType = LocomotionType.Hybrid;
                break;
            case LocomotionType.Hybrid:
                locomotionType = LocomotionType.Smooth;
                break;
        }

        Debug.Log($"Switched to {locomotionType} locomotion");
    }

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>
    /// 设置旋转速度
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(10f, speed);
    }

    /// <summary>
    /// 获取当前移动类型
    /// </summary>
    public LocomotionType GetCurrentLocomotionType()
    {
        return locomotionType;
    }

    /// <summary>
    /// 启用/禁用移动
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    /// <summary>
    /// 强制传送到位置
    /// </summary>
    public void ForceTeleport(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;

        Debug.Log($"Force teleported to: {position}");
    }

    void OnDestroy()
    {
        // 清理DOTween动画
        transform.DOKill();

        // 清理瞬移指示器
        if (teleportReticle != null)
        {
            DestroyImmediate(teleportReticle);
        }

        // 停止所有协程
        StopAllCoroutines();
    }

    void OnDrawGizmos()
    {
        // 绘制地面检测
        if (characterController != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * (characterController.height * 0.5f),
                                    groundCheckDistance);
        }

        // 绘制瞬移范围
        if (leftHandAnchor != null && (locomotionType == LocomotionType.Teleport || locomotionType == LocomotionType.Hybrid))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftHandAnchor.position, teleportMinDistance);
            Gizmos.DrawWireSphere(leftHandAnchor.position, teleportMaxDistance);
        }
    }
}