using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

/// <summary>
/// VR探索控制器 - 管理Museum场景的简单移动和交互
/// 提供基础的VR移动功能和返回菜单机制
/// </summary>
public class VRExplorerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 2f; // 移动速度
    public float rotationSpeed = 60f; // 旋转速度
    public bool enableSmoothTurn = true; // 启用平滑转向
    public float snapTurnAngle = 30f; // 快速转向角度

    [Header("控制器设置")]
    public XRController leftController; // 左手控制器 (可选，会自动查找)
    public CharacterController characterController; // 角色控制器
    public GameObject xrOrigin; // XR Origin (必需，要移动的对象)

    [Header("物理设置")]
    public float gravity = -9.81f; // 重力
    public LayerMask groundLayer; // 地面层
    public float groundCheckDistance = 0.1f; // 地面检查距离

    [Header("UI设置")]
    public Canvas infoCanvas; // 信息Canvas
    public UnityEngine.UI.Text infoText; // 信息文本
    public float infoDisplayDuration = 3f; // 信息显示持续时间

    [Header("音频设置")]
    public AudioClip moveSound; // 移动音效
    public AudioClip turnSound; // 转向音效
    public float audioVolume = 0.3f; // 音频音量

    // 私有变量
    private AudioSource audioSource;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private float currentRotation = 0f;
    private bool isInitialized = false;

    // 输入状态
    private Vector2 leftThumbstick;
    private bool leftThumbstickClicked;
    private bool rightThumbstickClicked;

    void Start()
    {
        InitializeExplorer();
    }

    /// <summary>
    /// 初始化探索控制器
    /// </summary>
    private void InitializeExplorer()
    {
        Debug.Log("初始化VR探索控制器...");

        // 查找必要的组件
        FindRequiredComponents();

        // 初始化音频系统
        InitializeAudio();

        // 设置角色控制器
        SetupCharacterController();

        // 初始化UI
        InitializeUI();

        // 验证VR系统
        VerifyVRSystem();

        isInitialized = true;
        Debug.Log("VR探索控制器初始化完成");

        // 显示欢迎信息
        ShowInfo("欢迎来到博物馆！\n使用左手摇杆移动\n长按菜单键3秒返回主菜单");
    }

    /// <summary>
    /// 查找必要组件
    /// </summary>
    private void FindRequiredComponents()
    {
        // 查找左手控制器 - XR Interaction Toolkit 2.6.5+ 新方式
        if (leftController == null)
        {
            // 首先尝试查找XRController
            XRController[] controllers = FindObjectsOfType<XRController>();
            if (controllers.Length > 0)
            {
                foreach (XRController controller in controllers)
                {
                    // 在新版本中，通过名称或位置判断左手控制器
                    if (controller.name.ToLower().Contains("left") ||
                        (controller.transform.parent != null && controller.transform.parent.name.ToLower().Contains("left")) ||
                        controller.transform.position.x < 0)
                    {
                        leftController = controller;
                        Debug.Log("找到左手控制器: " + controller.name);
                        break;
                    }
                }
            }
            else
            {
                // 如果没有XRController，尝试查找ActionBasedController
                var actionBasedControllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
                foreach (var controller in actionBasedControllers)
                {
                    if (controller.name.ToLower().Contains("left") ||
                        (controller.transform.parent != null && controller.transform.parent.name.ToLower().Contains("left")) ||
                        controller.transform.position.x < 0)
                    {
                        // ActionBasedController不是XRController类型，我们需要特殊处理
                        Debug.Log("找到Action-based左手控制器: " + controller.name);
                        // 由于类型不匹配，我们只能记录但不赋值
                        break;
                    }
                }

                if (leftController == null && actionBasedControllers.Length > 0)
                {
                    Debug.LogWarning("找到Action-based Controller但类型不匹配。请检查脚本兼容性。");
                }
            }
        }

        // 查找XR Origin - 这是必须的组件
        if (xrOrigin == null)
        {
            // 尝试通过名称查找XR Origin
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj != null)
            {
                xrOrigin = xrOriginObj;
                Debug.Log("找到XR Origin: " + xrOriginObj.name);
            }
            else
            {
                // 回退到查找XRRig (兼容旧版本)
#pragma warning disable CS0618 // 忽略过时警告
                UnityEngine.XR.Interaction.Toolkit.XRRig xrRig = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRRig>();
#pragma warning restore CS0618
                if (xrRig != null)
                {
                    xrOrigin = xrRig.gameObject;
                    Debug.Log("找到XRRig: " + xrRig.gameObject.name);
                }
            }
        }

        // 查找角色控制器 - 应该在XR Origin上
        if (characterController == null)
        {
            // 首先尝试在XR Origin上查找
            if (xrOrigin != null)
            {
                characterController = xrOrigin.GetComponent<CharacterController>();
            }

            // 如果没找到，在当前GameObject上查找（旧配置）
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
                if (characterController != null)
                {
                    Debug.LogWarning("角色控制器在当前GameObject上，建议将其移动到XR Origin上以获得正确的行为");
                }
            }
        }

        // 验证组件
        if (leftController == null)
        {
            Debug.LogWarning("左手XR控制器未找到，但移动功能仍可通过键盘调试");
        }

        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin未找到！这是必需的组件");
        }

        if (characterController == null)
        {
            Debug.LogError("角色控制器未找到！请在XR Origin上添加CharacterController组件");
        }
    }

    /// <summary>
    /// 初始化音频系统
    /// </summary>
    private void InitializeAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = audioVolume;
        audioSource.spatialBlend = 0f; // 2D音效
    }

    /// <summary>
    /// 设置角色控制器
    /// </summary>
    private void SetupCharacterController()
    {
        if (characterController != null)
        {
            characterController.slopeLimit = 45f;
            characterController.stepOffset = 0.3f;
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0f;
        }
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }

        if (infoText != null)
        {
            infoText.text = "";
        }
    }

    /// <summary>
    /// 验证VR系统
    /// </summary>
    private void VerifyVRSystem()
    {
        if (XRSettings.isDeviceActive)
        {
            Debug.Log($"VR设备已激活: {XRSettings.loadedDeviceName}");
        }
        else
        {
            Debug.LogWarning("VR设备未激活！");
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // 更新输入状态
        UpdateInput();

        // 处理移动
        HandleMovement();

        // 处理转向
        HandleRotation();

        // 应用重力
        ApplyGravity();

        // 移动角色
        MoveCharacter();

        // 检查地面
        CheckGround();
    }

    /// <summary>
    /// 更新输入状态
    /// </summary>
    private void UpdateInput()
    {
        // 左手摇杆输入
        leftThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        leftThumbstickClicked = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick);
        rightThumbstickClicked = OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick);

        // 传统输入支持（键盘调试） - 使用新Input System
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            leftThumbstick.x = keyboard.dKey.isPressed ? 1f : (keyboard.aKey.isPressed ? -1f : 0f);
            leftThumbstick.y = keyboard.wKey.isPressed ? 1f : (keyboard.sKey.isPressed ? -1f : 0f);
        }
    }

    /// <summary>
    /// 处理移动
    /// </summary>
    private void HandleMovement()
    {
        if (characterController == null) return;

        // 获取移动方向
        Vector3 moveDirection = Vector3.zero;

        // 优先使用XR Origin的方向
        if (xrOrigin != null)
        {
            // 前后移动
            moveDirection += xrOrigin.transform.forward * leftThumbstick.y;
            // 左右移动
            moveDirection += xrOrigin.transform.right * leftThumbstick.x;
        }
        else if (leftController != null)
        {
            // 备用：使用控制器方向
            moveDirection += leftController.transform.forward * leftThumbstick.y;
            moveDirection += leftController.transform.right * leftThumbstick.x;
        }
        else
        {
            // 最后备用：使用transform方向
            moveDirection += transform.forward * leftThumbstick.y;
            moveDirection += transform.right * leftThumbstick.x;
        }

        // 应用移动速度
        moveDirection *= moveSpeed;

        // 只在水平面上移动（忽略Y轴）
        moveDirection.y = 0f;

        // 如果正在移动，播放音效
        if (moveDirection.magnitude > 0.1f && audioSource != null && !audioSource.isPlaying)
        {
            if (moveSound != null)
            {
                audioSource.clip = moveSound;
                audioSource.Play();
            }
        }
        else if (moveDirection.magnitude <= 0.1f && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        playerVelocity.x = moveDirection.x;
        playerVelocity.z = moveDirection.z;
    }

    /// <summary>
    /// 处理转向
    /// </summary>
    private void HandleRotation()
    {
        if (xrOrigin == null) return;

        // 快速转向（右手摇杆点击）
        if (rightThumbstickClicked)
        {
            float turnDirection = leftThumbstick.x > 0.1f ? 1f : -1f;
            float turnAngle = snapTurnAngle * turnDirection;

            if (enableSmoothTurn)
            {
                StartCoroutine(SmoothTurn(turnAngle));
            }
            else
            {
                InstantTurn(turnAngle);
            }

            // 播放转向音效
            if (turnSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(turnSound);
            }
        }

        // 左手摇杆点击也可以触发转向
        if (leftThumbstickClicked && Mathf.Abs(leftThumbstick.x) > 0.5f)
        {
            float turnDirection = leftThumbstick.x > 0 ? 1f : -1f;
            float turnAngle = snapTurnAngle * turnDirection;

            if (enableSmoothTurn)
            {
                StartCoroutine(SmoothTurn(turnAngle));
            }
            else
            {
                InstantTurn(turnAngle);
            }
        }
    }

    /// <summary>
    /// 平滑转向
    /// </summary>
    private IEnumerator SmoothTurn(float angle)
    {
        float startRotation = currentRotation;
        float targetRotation = startRotation + angle;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            currentRotation = Mathf.Lerp(startRotation, targetRotation, t);

            if (xrOrigin != null)
            {
                xrOrigin.transform.rotation = Quaternion.Euler(0f, currentRotation, 0f);
            }

            yield return null;
        }

        currentRotation = targetRotation;
    }

    /// <summary>
    /// 瞬时转向
    /// </summary>
    private void InstantTurn(float angle)
    {
        currentRotation += angle;

        if (xrOrigin != null)
        {
            xrOrigin.transform.rotation = Quaternion.Euler(0f, currentRotation, 0f);
        }
    }

    /// <summary>
    /// 应用重力
    /// </summary>
    private void ApplyGravity()
    {
        if (isGrounded && playerVelocity.y < 0f)
        {
            playerVelocity.y = -2f; // 小的向下力确保接触地面
        }

        playerVelocity.y += gravity * Time.deltaTime;
    }

    /// <summary>
    /// 移动角色
    /// </summary>
    private void MoveCharacter()
    {
        if (characterController == null || xrOrigin == null) return;

        // 注意：CharacterController应该位于XR Origin上
        // 移动XR Origin（包含CharacterController组件的对象）
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    /// <summary>
    /// 检查地面
    /// </summary>
    private void CheckGround()
    {
        if (characterController == null) return;

        isGrounded = characterController.isGrounded;

        // 可以添加额外的地面检查
        if (!isGrounded)
        {
            // 向下射线检查地面 - 使用XR Origin的位置
            Vector3 checkPosition = xrOrigin != null ? xrOrigin.transform.position : transform.position;
            Vector3 groundCheckPosition = checkPosition + Vector3.down * (characterController.height / 2f + groundCheckDistance);
            isGrounded = Physics.CheckSphere(groundCheckPosition, groundCheckDistance, groundLayer);
        }
    }

    /// <summary>
    /// 显示信息
    /// </summary>
    public void ShowInfo(string message)
    {
        if (infoText != null)
        {
            infoText.text = message;
        }

        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(true);
            StartCoroutine(HideInfoAfterDelay());
        }
    }

    /// <summary>
    /// 延迟隐藏信息
    /// </summary>
    private IEnumerator HideInfoAfterDelay()
    {
        yield return new WaitForSeconds(infoDisplayDuration);

        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
        Debug.Log("移动速度设置为: " + moveSpeed);
    }

    /// <summary>
    /// 设置转向速度
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(10f, speed);
        Debug.Log("转向速度设置为: " + rotationSpeed);
    }

    /// <summary>
    /// 切换平滑转向
    /// </summary>
    public void ToggleSmoothTurn()
    {
        enableSmoothTurn = !enableSmoothTurn;
        Debug.Log("平滑转向: " + (enableSmoothTurn ? "开启" : "关闭"));
    }

    /// <summary>
    /// 传送到指定位置
    /// </summary>
    public void TeleportToPosition(Vector3 position)
    {
        if (characterController != null && xrOrigin != null)
        {
            characterController.enabled = false;
            xrOrigin.transform.position = position;
            characterController.enabled = true;
        }
    }

    void OnGUI()
    {
        if (!isInitialized) return;

        // 显示调试信息（仅在编辑器中）
        #if UNITY_EDITOR
        GUILayout.BeginArea(new Rect(10, 10, 250, 300));
        GUILayout.Label("VR探索控制器状态");
        GUILayout.Label("左手控制器: " + (leftController != null ? "已连接" : "未找到"));
        GUILayout.Label("角色控制器: " + (characterController != null ? "已连接" : "未找到"));
        GUILayout.Label("XR Origin: " + (xrOrigin != null ? "已找到" : "未找到"));
        GUILayout.Label("接地状态: " + (isGrounded ? "接地" : "悬空"));
        GUILayout.Label("移动速度: " + moveSpeed);
        GUILayout.Label("平滑转向: " + (enableSmoothTurn ? "开启" : "关闭"));

        GUILayout.Space(10);
        GUILayout.Label("输入状态:");
        GUILayout.Label("摇杆X: " + leftThumbstick.x.ToString("F2"));
        GUILayout.Label("摇杆Y: " + leftThumbstick.y.ToString("F2"));

        GUILayout.Space(10);
        if (GUILayout.Button("切换平滑转向"))
        {
            ToggleSmoothTurn();
        }

        if (GUILayout.Button("显示测试信息"))
        {
            ShowInfo("这是测试信息！");
        }

        GUILayout.EndArea();
        #endif
    }

    void OnDrawGizmosSelected()
    {
        // 绘制地面检查范围
        if (characterController != null)
        {
            Gizmos.color = Color.green;
            Vector3 checkPosition = xrOrigin != null ? xrOrigin.transform.position : transform.position;
            Vector3 groundCheckPosition = checkPosition + Vector3.down * (characterController.height / 2f + groundCheckDistance);
            Gizmos.DrawWireSphere(groundCheckPosition, groundCheckDistance);
        }
    }
}