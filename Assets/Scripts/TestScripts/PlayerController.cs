using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5.0f;

    [Header("视角设置")]
    public float mouseSensitivity = 2.0f;
    public float verticalLookLimit = 85.0f;

    [Header("相机引用")]
    public Transform cameraMount; // 玩家视角/主相机
    public PhotoCamera photoCamera; // 引用 PhotoCamera 脚本

    private float rotationX = 0;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (photoCamera == null)
        {
            Debug.LogError("未找到 PhotoCamera 脚本引用!");
        }
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        
        // 只在右键按下时才处理参数显示和拍照
        if (Input.GetMouseButton(1))
        {
            // 拍照脚本中会处理左键拍照逻辑
        }
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -verticalLookLimit, verticalLookLimit);
        cameraMount.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }
}