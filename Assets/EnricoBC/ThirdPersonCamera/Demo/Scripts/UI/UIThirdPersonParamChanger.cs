using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EnricoBC.ThirdPersonCamera {
    public class UIThirdPersonParamChanger : MonoBehaviour {
        [SerializeField]
        private ThirdPersonCamera TPSCamera;

        [SerializeField]
        private GameObject UIContainer;

        bool Active = true;

        public void ChangeViewOffsetX(float value) {
            TPSCamera.ViewOffset.x = value;
        }

        public void ChangeViewOffsetY(float value) {
            TPSCamera.ViewOffset.y = value;
        }

        public void ChangeMaxDistance(float value) {
            TPSCamera.MaxDistance = value;
        }

        public void ChangeMinDistance(float value) {
            TPSCamera.MinDistanceToTarget = value;
        }

        public void ChangeStateSwitchSpeed(float value) {
            TPSCamera.CameraStateSwitchSpeed = value;
        }

        public void ChangeRotationSpeed(float value) {
            TPSCamera.RotationSpeed = value;
        }

        public void ChangeSmoothRotation(float value) {
            TPSCamera.SmoothRotation = value;
        }

        public void ChangeFollowSpeed(float value) {
            TPSCamera.FollowSpeed = value;
        }

        public void ChangeLockOnViewOffsetX(float value) {
            TPSCamera.LockedOnViewOffset.x = value;
        }

        public void ChangeLockOnViewOffsetY(float value) {
            TPSCamera.LockedOnViewOffset.y = value;
        }

        public void ChangeMaxLockOnAngle(float value) {
            TPSCamera.LockOffTime = value;
        }

        public void ChangeLockOffTime(float value) {
            TPSCamera.LockOffTime = value;
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                Active = !Active;
                UIContainer.SetActive(Active);
            }
        }
    }
}