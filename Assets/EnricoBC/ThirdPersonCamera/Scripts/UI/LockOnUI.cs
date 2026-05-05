using UnityEngine;
using UnityEngine.UI;

namespace EnricoBC.ThirdPersonCamera {
    public class LockOnUI : MonoBehaviour {
        [SerializeField]
        private RectTransform LockOnImage;

        public void SetLockOnPosition(Vector2 position) {
            LockOnImage.SetPositionAndRotation(position, Quaternion.identity);
        }

        public void Enable(bool enable) {
            LockOnImage.gameObject.SetActive(enable);
        }
    }
}