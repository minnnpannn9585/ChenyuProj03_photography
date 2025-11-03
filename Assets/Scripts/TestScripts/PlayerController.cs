using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("基础设置")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 3f;
    public CameraController cameraController;  // 拍照控制脚本

    [Header("VR设置")]
    public bool enableVRMode = true; // 启用VR模式
    public XRController leftController; // 左手控制器
    public GameObject xrOrigin; // XR Origin
    public float vrMoveSpeed = 2f; // VR移动速度

    // 私有变量
    private CharacterController controller;
    private float yaw;    // 水平旋转角度（绕Y轴）
    private float pitch;  // 垂直旋转角度（绕X轴）
    private bool isVRActive = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 检测VR是否激活
        isVRActive = enableVRMode && XRSettings.isDeviceActive;

        // 查找VR组件
        if (isVRActive)
        {
            FindVRComponents();
        }

        if (!isVRActive)
        {
            // 传统模式初始化
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;

            // 初始化角度为当前朝向
            yaw = transform.localEulerAngles.y;
            pitch = transform.localEulerAngles.x;
        }

        Debug.Log("玩家控制器初始化 - VR模式: " + (isVRActive ? "启用" : "禁用"));
    }

    void Update()
    {
        if (isVRActive)
        {
            UpdateVRMovement();
        }
        else
        {
            UpdateTraditionalMovement();
        }
    }

    /// <summary>
    /// 查找VR组件
    /// </summary>
    private void FindVRComponents()
    {
        // 查找左手控制器 - XR Interaction Toolkit 2.6.5+ 新方式
        if (leftController == null)
        {
            XRController[] controllers = FindObjectsOfType<XRController>();
            foreach (XRController controller in controllers)
            {
                // 在新版本中，通过名称或位置判断左手控制器
                if (controller.name.ToLower().Contains("left") ||
                    (controller.transform.parent != null && controller.transform.parent.name.ToLower().Contains("left")) ||
                    controller.transform.position.x < 0)
                {
                    leftController = controller;
                    break;
                }
            }
        }

        // 查找XR Origin
        if (xrOrigin == null)
        {
            // 尝试通过名称查找XR Origin
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj != null)
            {
                xrOrigin = xrOriginObj;
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
                }
            }
        }

        Debug.Log("VR组件查找完成 - 左手控制器: " + (leftController != null ? "找到" : "未找到") +
                 ", XR Origin: " + (xrOrigin != null ? "找到" : "未找到"));
    }

    /// <summary>
    /// VR模式移动更新
    /// </summary>
    private void UpdateVRMovement()
    {
        if (leftController == null) return;

        // 获取左手摇杆输入
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // 如果摇杆输入足够大，才进行移动
        if (thumbstick.magnitude > 0.1f)
        {
            // 计算移动方向（基于控制器朝向）
            Vector3 moveDirection = leftController.transform.forward * thumbstick.y + leftController.transform.right * thumbstick.x;
            moveDirection.y = 0f; // 保持在水平面上
            moveDirection.Normalize();

            // 应用移动
            controller.Move(moveDirection * vrMoveSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 传统模式移动更新
    /// </summary>
    private void UpdateTraditionalMovement()
    {
        // 使用新Input System获取键盘输入
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && mouse != null)
        {
            // 移动
            Vector2 moveInput = new Vector2(
                keyboard.dKey.isPressed ? 1f : (keyboard.aKey.isPressed ? -1f : 0f),
                keyboard.wKey.isPressed ? 1f : (keyboard.sKey.isPressed ? -1f : 0f)
            );
            Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
            controller.Move(moveDir * moveSpeed * Time.deltaTime);

            // 读取鼠标输入
            Vector2 mouseDelta = mouse.delta.ReadValue();
            float mouseX = mouseDelta.x * mouseSensitivity;
            float mouseY = mouseDelta.y * mouseSensitivity;

            // 累加旋转角度
            yaw   += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -80f, 80f);  // 限制俯仰范围

            // 应用旋转：注意使用 Quaternion 防止欧拉角累积问题
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            // 鼠标右键拍照（传统模式）
            if (mouse.rightButton.wasPressedThisFrame && cameraController != null)
            {
                cameraController.CapturePhoto();
            }
        }
    }
}