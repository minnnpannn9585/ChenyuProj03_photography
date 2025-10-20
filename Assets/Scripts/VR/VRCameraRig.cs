using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPhotography
{
    /// <summary>
    /// VR相机系统，管理XR Origin和控制器追踪
    /// </summary>
    public class VRCameraRig : MonoBehaviour
    {
        [Header("XR Setup")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private ActionBasedController leftController;
        [SerializeField] private ActionBasedController rightController;

        [Header("Camera Settings")]
        [SerializeField] private float standingHeight = 1.7f;
        [SerializeField] private Vector3 headOffset = Vector3.zero;

        public XROrigin XROrigin => xrOrigin;
        public ActionBasedController LeftController => leftController;
        public ActionBasedController RightController => rightController;

        private void Awake()
        {
            InitializeVR();
        }

        private void InitializeVR()
        {
            // 如果没有指定XR Origin，尝试查找
            if (xrOrigin == null)
            {
                xrOrigin = FindObjectOfType<XROrigin>();
            }

            // 查找控制器
            if (leftController == null)
            {
                var controllers = FindObjectsOfType<ActionBasedController>();
                foreach (var controller in controllers)
                {
                    if (controller.controllerNode == XRNode.LeftHand)
                    {
                        leftController = controller;
                    }
                    else if (controller.controllerNode == XRNode.RightHand)
                    {
                        rightController = controller;
                    }
                }
            }

            // 设置玩家高度
            if (xrOrigin != null)
            {
                xrOrigin.CameraYOffset = standingHeight;
            }
        }

        /// <summary>
        /// 获取右手控制器位置（用于放置相机）
        /// </summary>
        public Vector3 GetRightHandPosition()
        {
            if (rightController != null)
            {
                return rightController.transform.position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// 获取右手控制器旋转（用于相机朝向）
        /// </summary>
        public Quaternion GetRightHandRotation()
        {
            if (rightController != null)
            {
                return rightController.transform.rotation;
            }
            return Quaternion.identity;
        }

        /// <summary>
        /// 检查VR系统是否就绪
        /// </summary>
        public bool IsVRReady()
        {
            return xrOrigin != null &&
                   leftController != null &&
                   rightController != null &&
                   XRSettings.isDeviceActive;
        }
    }
}