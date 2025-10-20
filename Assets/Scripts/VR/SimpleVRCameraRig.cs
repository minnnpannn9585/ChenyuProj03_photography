using UnityEngine;
using UnityEngine.XR;

namespace VRPhotography
{
    /// <summary>
    /// 简化的VR相机系统，不依赖XR Interaction Toolkit
    /// </summary>
    public class SimpleVRCameraRig : MonoBehaviour
    {
        [Header("VR Setup")]
        [SerializeField] private Camera vrCamera;
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;

        [Header("Camera Settings")]
        [SerializeField] private float standingHeight = 1.7f;

        public Camera VRCamera => vrCamera;
        public Transform LeftHandAnchor => leftHandAnchor;
        public Transform RightHandAnchor => rightHandAnchor;

        private void Awake()
        {
            InitializeVR();
        }

        private void InitializeVR()
        {
            // 如果没有指定相机，尝试查找主相机
            if (vrCamera == null)
            {
                vrCamera = Camera.main;
            }

            // 创建手部锚点（如果没有）
            CreateHandAnchors();

            // 设置VR设备
            EnableVR();
        }

        private void CreateHandAnchors()
        {
            // 创建左手锚点
            if (leftHandAnchor == null)
            {
                var leftHandObj = new GameObject("LeftHandAnchor");
                leftHandObj.transform.SetParent(transform);
                leftHandAnchor = leftHandObj.transform;
            }

            // 创建右手锚点
            if (rightHandAnchor == null)
            {
                var rightHandObj = new GameObject("RightHandAnchor");
                rightHandObj.transform.SetParent(transform);
                rightHandAnchor = rightHandObj.transform;
            }
        }

        private void EnableVR()
        {
            // 启用VR
            if (!XRSettings.isDeviceActive)
            {
                XRSettings.LoadDeviceByName("OpenXR");
            }

            // 设置相机位置
            if (vrCamera != null)
            {
                vrCamera.transform.position = new Vector3(0, standingHeight, 0);
            }
        }

        /// <summary>
        /// 获取右手控制器位置
        /// </summary>
        public Vector3 GetRightHandPosition()
        {
            if (rightHandAnchor != null)
            {
                // 这里可以添加实际的VR控制器追踪逻辑
                return rightHandAnchor.position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// 获取右手控制器旋转
        /// </summary>
        public Quaternion GetRightHandRotation()
        {
            if (rightHandAnchor != null)
            {
                return rightHandAnchor.rotation;
            }
            return Quaternion.identity;
        }

        /// <summary>
        /// 检查VR系统是否就绪
        /// </summary>
        public bool IsVRReady()
        {
            return vrCamera != null && XRSettings.isDeviceActive;
        }

        /// <summary>
        /// 更新手部锚点位置（用于测试）
        /// </summary>
        private void Update()
        {
            // 简单的测试：使用键盘模拟手部移动
            if (rightHandAnchor != null)
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");

                if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
                {
                    Vector3 movement = new Vector3(horizontal, 0, vertical) * Time.deltaTime * 2f;
                    rightHandAnchor.position += movement;
                }
            }
        }
    }
}