using UnityEngine;

namespace EnricoBC.ThirdPersonCamera {
    [RequireComponent(typeof(CharacterController))]
    public class MovementController : MonoBehaviour {
        private CharacterController Controller;
        [SerializeField]
        private float Speed = 5f;
        [SerializeField]
        private ThirdPersonCamera Cam;

        private bool Active = false;

        void Awake() {
            Controller = GetComponent<CharacterController>();
        }

        void Update() {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            if (Input.GetKeyDown(KeyCode.Space)) {
                Active = !Active;
                Cursor.lockState = Active ? CursorLockMode.Confined : CursorLockMode.None;
                Cursor.visible = !Active;
            }
            if (!Active) {
                return;
            }
            Cam.UpdateCamera(mouseX, mouseY);

            if (Input.GetKeyDown(KeyCode.Mouse2)) {
                if (Cam.HasLockedOnTarget)
                    Cam.LockOnTarget(null);
                else
                    Cam.LockOn();
            }
            if (Input.GetKeyDown(KeyCode.E)) {
                if (Cam.HasLockedOnTarget)
                    Cam.LockOn(true);
            } else if (Input.GetKeyDown(KeyCode.Q)) {
                if (Cam.HasLockedOnTarget)
                    Cam.LockOn(false);
            }

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 move = Cam.transform.forward * vertical + Cam.transform.right * horizontal;
            move.Normalize();
            Controller.SimpleMove(move * Speed);
            move.y = 0f;
            transform.LookAt(move + transform.position);
            // Controller.Move(move.normalized * Speed);

            if (Input.GetKeyDown(KeyCode.F)) { // make target untargetable
                if (Cam.HasLockedOnTarget) {
                    Cam.LockedOnTarget.gameObject.GetComponent<TargetableCapsule>().EnableTarget = false;
                }
            }
            if (Input.GetKeyDown(KeyCode.R)) { // reset all enemies targetable status
                var capsules = FindObjectsOfType<TargetableCapsule>();
                foreach (var capsule in capsules) {
                    capsule.EnableTarget = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.LeftShift)) {
                // face forward
                Cam.LookAtDirection(transform.forward);
            }
        }
    }
}