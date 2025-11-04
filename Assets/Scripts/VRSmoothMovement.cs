using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

[RequireComponent(typeof(CharacterController))]
public class VRSmoothMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.0f; // 移动速度
    public float turnSpeed = 60.0f; // 转向速度（度/秒）
    public float gravity = -9.81f; // 重力强度

    [Header("Input Settings")]
    public InputActionAsset vrInputActions; // VR输入动作资源

    [Header("Ground Check")]
    public Transform groundCheck; // 地面检测点
    public float groundDistance = 0.1f; // 地面检测距离
    public LayerMask groundMask; // 地面层

    private CharacterController characterController;
    private InputAction leftAxisAction;
    private InputAction rightAxisAction;

    private Vector2 moveInput;
    private Vector2 turnInput;
    private Vector3 velocity;
    private bool isGrounded;
    private Transform xrOrigin; // XR Origin / Camera Rig

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        SetupInputActions();
        FindXROrigin();
    }

    void OnDestroy()
    {
        CleanupInputActions();
    }

    // 查找Meta SDK的结构
    private void FindXROrigin()
    {
        // 方法1: 查找包含CenterEyeAnchor的对象
        var centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            xrOrigin = centerEye.transform.parent;
            Debug.Log($"通过CenterEyeAnchor找到Meta SDK结构: {xrOrigin.name}");
            return;
        }

        // 方法2: 查找带有相机的父对象（通常是OVRCameraRig）
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 向上查找父对象，寻找可能是XR根对象的结构
            Transform parent = mainCamera.transform.parent;
            while (parent != null)
            {
                // 检查对象名称中是否包含常见的Meta SDK标识
                if (parent.name.Contains("OVRCameraRig") ||
                    parent.name.Contains("CameraRig") ||
                    parent.name.Contains("XR") ||
                    parent.name.Contains("VR"))
                {
                    xrOrigin = parent;
                    Debug.Log($"通过相机层级找到Meta SDK结构: {xrOrigin.name}");
                    return;
                }
                parent = parent.parent;
            }

            // 如果没找到特定名称的父对象，使用相机的直接父对象
            if (mainCamera.transform.parent != null)
            {
                xrOrigin = mainCamera.transform.parent;
                Debug.Log($"使用相机父对象作为XR Origin: {xrOrigin.name}");
                return;
            }
        }

        // 方法3: 尝试通过名称查找
        string[] possibleNames = { "OVRCameraRig", "CameraRig", "XR Origin", "VR Rig" };
        foreach (string name in possibleNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                xrOrigin = obj.transform;
                Debug.Log($"通过名称找到Meta SDK结构: {xrOrigin.name}");
                return;
            }
        }

        // 方法4: 查找OVRManager
        var ovrManager = Object.FindObjectOfType<OVRManager>();
        if (ovrManager != null)
        {
            xrOrigin = ovrManager.transform;
            Debug.Log($"通过OVRManager找到Meta SDK结构: {xrOrigin.name}");
            return;
        }

        Debug.LogWarning("未找到Meta SDK的Camera Rig结构。移动功能可能不会正常工作。");
    }

    // 设置Input Actions
    private void SetupInputActions()
    {
        if (vrInputActions == null)
        {
            Debug.LogError("VRInputActions资源未分配！请在Inspector中分配VRInputActions资源。");
            return;
        }

        vrInputActions.Enable();

        InputActionMap questActionMap = vrInputActions.FindActionMap("Quest");
        if (questActionMap != null)
        {
            // 获取左摇杆输入（移动）
            leftAxisAction = questActionMap.FindAction("LeftAxis");
            if (leftAxisAction != null)
            {
                leftAxisAction.performed += OnMoveInput;
                leftAxisAction.canceled += OnMoveInputCanceled;
                leftAxisAction.Enable();
            }

            // 获取右摇杆输入（转向）
            rightAxisAction = questActionMap.FindAction("RightAxis");
            if (rightAxisAction != null)
            {
                rightAxisAction.performed += OnTurnInput;
                rightAxisAction.canceled += OnTurnInputCanceled;
                rightAxisAction.Enable();
            }
        }
        else
        {
            Debug.LogError("在VRInputActions中未找到Quest Action Map！");
        }
    }

    // 清理Input Actions
    private void CleanupInputActions()
    {
        if (leftAxisAction != null)
        {
            leftAxisAction.performed -= OnMoveInput;
            leftAxisAction.canceled -= OnMoveInputCanceled;
            leftAxisAction.Disable();
        }

        if (rightAxisAction != null)
        {
            rightAxisAction.performed -= OnTurnInput;
            rightAxisAction.canceled -= OnTurnInputCanceled;
            rightAxisAction.Disable();
        }

        if (vrInputActions != null)
        {
            vrInputActions.Disable();
        }
    }

    // 移动输入事件
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // 移动输入取消事件
    private void OnMoveInputCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    // 转向输入事件
    private void OnTurnInput(InputAction.CallbackContext context)
    {
        turnInput = context.ReadValue<Vector2>();
    }

    // 转向输入取消事件
    private void OnTurnInputCanceled(InputAction.CallbackContext context)
    {
        turnInput = Vector2.zero;
    }

    void Update()
    {
        HandleMovement();
        HandleTurning();
        HandleGravity();
    }

    // 处理移动
    private void HandleMovement()
    {
        if (characterController == null || xrOrigin == null) return;

        // 获取相机方向
        Transform cameraTransform = Camera.main?.transform;
        if (cameraTransform == null)
        {
            // 尝试在XR Origin下查找相机
            cameraTransform = xrOrigin.GetComponentInChildren<Camera>()?.transform;
        }

        if (cameraTransform == null)
        {
            // 尝试查找CenterEyeAnchor（Meta SDK的标准相机锚点）
            var centerEye = xrOrigin.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                cameraTransform = centerEye;
            }
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("无法找到相机，移动功能将无法正常工作");
            return;
        }

        // 计算移动方向
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        // 应用移动输入
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        Vector3 movement = moveDirection * moveSpeed;

        // 应用重力
        movement.y = velocity.y;

        // 移动角色控制器
        characterController.Move(movement * Time.deltaTime);
    }

    // 处理转向
    private void HandleTurning()
    {
        if (xrOrigin == null) return;

        // 使用右摇杆的x轴输入进行转向
        if (Mathf.Abs(turnInput.x) > 0.1f)
        {
            float turnAmount = turnInput.x * turnSpeed * Time.deltaTime;
            xrOrigin.Rotate(0, turnAmount, 0);
        }
    }

    // 处理重力
    private void HandleGravity()
    {
        if (characterController == null) return;

        // 地面检测
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 保持在地面上
        }

        // 应用重力
        velocity.y += gravity * Time.deltaTime;

        // 应用重力移动
        characterController.Move(velocity * Time.deltaTime);
    }
}