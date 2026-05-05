using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EnricoBC.ThirdPersonCamera {
    public class ThirdPersonCamera : MonoBehaviour {

        [Header("Camera Properties")]
        [SerializeField]
        private Transform FollowTarget;

        [Tooltip("Offset position to apply to the camera")]
        public Vector2 ViewOffset = Vector2.zero;

        [Range(10f, 5000f)]
        [Tooltip("How fast the camera rotates from input")]
        public float RotationSpeed = 500.0f;

        [Tooltip("Maximum view distance to target")]
        public float MaxDistance = 5f;

        [Tooltip("Minimum view distance to target")]
        public float MinDistanceToTarget = 0.1f;

        private float Distance = 5f;

        [SerializeField]
        [Tooltip("Maximum time to wait to determine when the camera has finished rotation towards the target selected by any of the LookAt functions")]
        private float LookAtMaxTime = 0.3f;

        [SerializeField]
        [Tooltip("Angle in degrees to check when the camera has finished rotation towards the target selected by any of the LookAt functions")]
        private float LookAtMinAngle = 5.0f;
        private float CurrentLookAtTimer = 0f;

        private float cx = 0f, cy = Mathf.PI / 2;

        [Tooltip("How fast the camera transitions between lock on and lock off states")]
        public float CameraStateSwitchSpeed = 4f;

        [Range(0f, 100f)]
        [Tooltip("How much the camera smooths between movement, lower values make the camera rotate smoothly")]
        public float SmoothRotation = 10f;

        [Range(1f, 20f)]
        [Tooltip("How fast the camera follows the target")]
        public float FollowSpeed = 8f;

        [Header("Camera Collision")]
        [SerializeField]
        [Tooltip("Radius for the sphere collider to check for collisions to adjust position")]
        private float SphereColliderRadius = 0.5f;

        [SerializeField]
        [Tooltip("Layers in which the camera will check for collisions to adjust position")]
        private LayerMask CollisionLayers = 1;

        /// <summary>
        /// How many frames to skip during lock on before checking for an obstacle
        /// </summary>
        private int ObstacleCheckFrameSkip = 5;
        private int CurrentObstacleCheckFrame = 0;
        private bool HasObstacleDuringLockOn = false;

        Vector3 CurrentPosition;
        Vector3 TargetPosition;
        Vector3 LastLookAt, CurrentLookAt;
        Quaternion TargetRotation = Quaternion.identity;
        private Quaternion CurrentRotation;

        [Header("Lock On")]
        [SerializeField]
        [Tooltip("Layer mask on which the camera will attempt to lock on")]
        private LayerMask LockOnMask = 1;

        [Tooltip("Position offset for the camera when locked on target")]
        public Vector2 LockedOnViewOffset = Vector2.zero;

        [SerializeField]
        [Tooltip("How far the camera will try to find a target to lock on")]
        private float LockOnRange = 25f;

        [SerializeField]
        [Tooltip("How far the camera can go before attempting to stop lock on automatically")]
        private float MaxLockOnDistance = 25f;

        [Tooltip("How much time to wait after the camera has reached the maximum lock on distance to remove lock\nIf the camera enters the maximum allowed distance again the timer is reset")]
        public float LockOffTime = 2f;

        private float CurrentLockOffTime = 0f;

        [SerializeField]
        private LockOnUI LockOnCanvas;

        [Tooltip("The maximum angle to check for target to lock on")]
        public float MaxLockOnSelectAngle = 45f;

        [SerializeField]
        private bool DebugLogs = false;

        private ITargetable LockedOnTargetable;
        public Transform LockedOnTarget { get; private set; }
        public bool HasLockedOnTarget { get { return LockedOnTarget != null; } }

        /// <summary>
        /// If != Vector3.zero, rotate to look at this value.
        /// </summary>
        private Vector3 ForceLookAtDirection;

        private SphereCollider coll;

        private float inputX, inputY;

        private float CameraSwitchSmoothTime = 1f;

        void Awake() {
            WarpTo(transform.position);
        }

        void Start() {
            CurrentRotation = Quaternion.Euler(new Vector3(cy, cx, 0));
            if (!FollowTarget) {
                GameObject t = GameObject.FindGameObjectWithTag("Player");
            }
            if (FollowTarget != null)
                FollowTarget = FollowTarget.transform;

            coll = gameObject.AddComponent<SphereCollider>();
            coll.radius = SphereColliderRadius;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        void LateUpdate() {
            Vector3 direction = transform.position - FollowTarget.position;
            RaycastHit hit;
            if (Physics.SphereCast(FollowTarget.position,
                0.6f,
                direction,
                out hit,
                MaxDistance,
                CollisionLayers,
                QueryTriggerInteraction.Ignore)) {
                Distance = Mathf.Clamp(hit.distance, MinDistanceToTarget, MaxDistance);

            } else {
                Distance = MaxDistance;
            }
            OnCameraUpdate();
        }

        /// <summary>
        /// Forces the camera to move to the closest valid point to the specified location.
        /// </summary>
        /// <param name="location"></param>
        public void WarpTo(Vector3 location) {
            CurrentPosition = location;
            TargetPosition = location;
            transform.position = location;
            LastLookAt = CurrentLookAt = FollowTarget.position;

            // By default set camera's position to the closest location
            Quaternion lookatrot = Quaternion.LookRotation(FollowTarget.position - location, Vector3.up);
            cx = lookatrot.eulerAngles.y;
            cy = lookatrot.eulerAngles.x;
            CurrentRotation = TargetRotation = lookatrot;
            Log($"WarpTo: eulerAngles: {lookatrot.eulerAngles}, cx: {cx}, cy: {cy}");
        }

        /// <summary>
        /// Makes the camera rotate and look at the specified position.
        /// </summary>
        /// <param name="lookAtPosition"></param>
        public void LookAt(Vector3 lookAtPosition) {
            LookAtDirection(lookAtPosition - transform.position);
        }

        /// <summary>
        /// Makes the camera rotate and look towards the specified direction.
        /// </summary>
        /// <param name="lookAtPosition"></param>
        public void LookAtDirection(Vector3 lookAtDirection) {
            if (!HasLockedOnTarget) {
                CurrentLookAtTimer = 0f;
                ForceLookAtDirection = lookAtDirection.normalized;
            }
        }

        public static float ScalarProjection(Vector3 vector, Vector3 project_onto) {
            var proj_norm = project_onto.normalized;
            return Vector3.Dot(vector.normalized, proj_norm) / Vector3.Magnitude(proj_norm);
        }

        protected bool CanSelectTransform(Transform t) {
            if (t.TryGetComponent<ITargetable>(out ITargetable targetable)) {
                bool hasObstacle = HasObstacle(t);
                return !hasObstacle && targetable.CanBeTargeted();
            }
            return false;
        }

        protected bool HasObstacle(Transform t) {
            // check raycast
            RaycastHit[] hits;
            Vector3 direction = t.position - transform.position;
            float distance = direction.magnitude;
            hits = Physics.RaycastAll(transform.position, direction, distance, CollisionLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits) {
                if (hit.transform.root != t && hit.transform.root != FollowTarget) return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to lock on a targetable object, this is defined by implementing the `ITargetable` interface in your scripts.
        /// </summary>
        /// <param name="right">If a target is already locked on, try to target the next targetable object to the right.</param>
        public void LockOn(bool right = false) {
            Collider[] colliders = Physics.OverlapSphere(FollowTarget.transform.position, LockOnRange, LockOnMask, QueryTriggerInteraction.Ignore);
            Log("Trying to lock on target");
            List<TargetData> targets = new List<TargetData>();
            if (colliders != null) {
                Log("Collider count: " + colliders.Length);
                TargetData closest_to_zero = null;
                for (int i = 0; i < colliders.Length; i++) {
                    if (colliders[i].transform != FollowTarget.transform) {
                        Transform t = colliders[i].transform.root;
                        if (CanSelectTransform(colliders[i].transform)) {
                            TargetData data = new TargetData();
                            data.Target = colliders[i].transform;
                            data.DistanceFromPlayer = Vector3.Distance(data.Target.position, FollowTarget.transform.position);
                            data.AngleForward = Vector3.Angle(transform.forward, data.Target.transform.position - transform.position);
                            data.AngleRight = Vector3.Angle(transform.right, data.Target.transform.position - transform.position);
                            if (data.AngleForward < MaxLockOnSelectAngle) {
                                if (data.AngleRight < 90f) {
                                    data.AngleForward = -1 * data.AngleForward;
                                }
                                if (closest_to_zero == null || Mathf.Abs(data.AngleForward) < Mathf.Abs(closest_to_zero.AngleForward)) {
                                    closest_to_zero = data;
                                }
                                targets.Add(data);
                            }
                        }
                    }
                }
                // sort by angle.
                TargetData.SortForward = false;
                targets.Sort();
                if (targets.Count == 0) {
                    LockOnTarget(null);
                    return;
                }
                if (LockedOnTarget == null) {
                    LockOnTarget(closest_to_zero.Target);
                    Log("Selected " + LockedOnTarget.name + " with angle " + closest_to_zero.AngleForward);
                } else {
                    // find current
                    int idx = -1;
                    for (int i = 0; i < targets.Count; i++) {
                        if (targets[i].Target == LockedOnTarget.transform) {
                            idx = i;
                            break;
                        }
                    }
                    if (idx == -1) {
                        idx = targets.BinarySearch(closest_to_zero);
                    }
                    if (idx < targets.Count) {
                        Transform found = targets[idx].Target;
                        if (found == LockedOnTarget) {
                            if (!right && idx + 1 < targets.Count) {
                                LockOnTarget(targets[idx + 1].Target);
                            } else if (right && idx - 1 >= 0) {
                                LockOnTarget(targets[idx - 1].Target);
                            }
                        } else {
                            LockOnTarget(targets[idx].Target);
                        }
                    } else if (idx - 1 >= 0) {
                        Transform found = targets[idx].Target;
                        if (found == LockedOnTarget) {
                            if (!right && idx - 2 >= 0) {
                                LockOnTarget(targets[idx - 2].Target);
                            } else if (right && idx < targets.Count) {
                                LockOnTarget(targets[idx].Target);
                            }
                        } else {
                            LockOnTarget(targets[idx - 1].Target);
                        }
                    }
                }
            }
        }
        
        public void SetCameraAngles(float x, float y) {
            cy = y;
            cx = x;
        }

        /// <summary>
        /// Updates camera rotation based on input.
        /// </summary>
        /// <param name="x">How much rotation is applied along the vertical axis.</param>
        /// <param name="y">How much rotation is applied along the horizontal axis.</param>
        public void UpdateCamera(float x, float y) {
            inputX = x; inputY = y;
        }

        void OnCameraUpdate() {
            if (LockedOnTarget) {
                cy = Mathf.PI / 2;
                cx = 0;
                Vector3 cxvec = Vector3.ProjectOnPlane(FollowTarget.transform.position - LockedOnTarget.transform.position, Vector3.up);
                var offset = transform.rotation * LockedOnViewOffset;
                TargetPosition = (cxvec.normalized * Distance) + FollowTarget.position + offset;
                transform.position = Vector3.Lerp(transform.position, TargetPosition, Time.deltaTime * FollowSpeed);
                LastLookAt = CurrentLookAt;
                CurrentLookAt = Vector3.Lerp(LastLookAt, LockedOnTarget.transform.position, (1 - CameraSwitchSmoothTime) * CameraStateSwitchSpeed * Time.deltaTime);

                if (CurrentObstacleCheckFrame >= ObstacleCheckFrameSkip) {
                    CurrentObstacleCheckFrame = 0;
                    HasObstacleDuringLockOn = HasObstacle(LockedOnTarget);
                }
                if (HasObstacleDuringLockOn || Vector3.Distance(transform.position, LockedOnTarget.position) > MaxLockOnDistance) {
                    if (CurrentLockOffTime == 0f) {
                        if (HasObstacleDuringLockOn)
                            Log($"There's an obstacle between the camera and the locked on target, camera will lock off in {LockOffTime} seconds");
                        else
                            Log($"Camera has left the allowed range to keep target lock on, camera will lock off in {LockOffTime} seconds");
                    }
                    CurrentLockOffTime += Time.deltaTime;
                    if (CurrentLockOffTime >= LockOffTime) {
                        Log($"Locked off after {CurrentLockOffTime} seconds");
                        LockOnTarget(null);
                        CurrentLockOffTime = 0f;
                    }
                } else {
                    if (CurrentLockOffTime != 0f)
                        Log("Camera has entered the allowed range to keep target lock on");
                    CurrentLockOffTime = 0f;
                }
                if (LockedOnTargetable != null && !LockedOnTargetable.CanBeTargeted()) {
                    LockOn();
                }
                CurrentObstacleCheckFrame++;
            } else if (FollowTarget) {
                if (ForceLookAtDirection != Vector3.zero) {
                    TargetRotation = Quaternion.LookRotation(ForceLookAtDirection, Vector3.up);
                    if (Vector3.Angle(transform.forward, ForceLookAtDirection) <= LookAtMinAngle || CurrentLookAtTimer >= LookAtMaxTime) {
                        ForceLookAtDirection = Vector3.zero;
                        cx = TargetRotation.eulerAngles.y;
                        cy = TargetRotation.eulerAngles.x;
                    }
                } else {
                    cx += inputX * RotationSpeed * Time.deltaTime; cy -= inputY * RotationSpeed * Time.deltaTime;
                    cy = Mathf.Clamp(cy, -50f, 50f); // TODO: expose as parameters
                    TargetRotation = Quaternion.Euler(new Vector3(cy, cx, 0));
                }
                CurrentRotation = Quaternion.Slerp(CurrentRotation, TargetRotation, SmoothRotation * Time.deltaTime);

                var offset = CurrentRotation * ViewOffset;
                var pos = CurrentRotation * new Vector3(0f, 0f, -Distance);
                TargetPosition = Vector3.Lerp(transform.position, pos + FollowTarget.position + offset, Time.deltaTime * FollowSpeed);

                transform.position = TargetPosition;
                LastLookAt = CurrentLookAt;
                CurrentLookAt = Vector3.Lerp(LastLookAt, FollowTarget.position + offset, (1 - CameraSwitchSmoothTime) * CameraStateSwitchSpeed * Time.deltaTime);
            }
            transform.LookAt(CurrentLookAt);
            CameraSwitchSmoothTime = Mathf.Clamp(CameraSwitchSmoothTime - Time.deltaTime, 0, 1);
            if (CurrentLookAtTimer <= LookAtMaxTime)
                CurrentLookAtTimer += Time.deltaTime;

            if (LockedOnTarget != null) {
                LockOnCanvas.SetLockOnPosition(Camera.main.WorldToScreenPoint(LockedOnTarget.transform.position + LockedOnTargetable.TargetPosition));
            }
        }

        public void LockOnTarget(Transform target) {
            if (LockedOnTarget != null && target == null) {
                // calculate cx & cy
                Vector3 r = transform.position - FollowTarget.position;
                cy = Mathf.Asin(r.y / r.magnitude);
                cx = 180f - Mathf.Acos(r.z / (r.magnitude * Mathf.Cos(cy))) * Mathf.Rad2Deg;
                if (r.x > 0) {
                    cx = -cx;
                }
                cy *= Mathf.Rad2Deg;
                CurrentRotation = Quaternion.Euler(new Vector3(cy, cx, 0));
                LockOnCanvas.Enable(false);
            }
            if (LockedOnTarget != target) {
                CameraSwitchSmoothTime = 1f;
            }
            LockedOnTarget = target;
            if (LockedOnTarget != null) {
                LockedOnTarget.TryGetComponent<ITargetable>(out LockedOnTargetable);
            } else {
                LockedOnTargetable = null;
            }
            LockOnCanvas.Enable(LockedOnTarget != null);
        }

        private void Log(string log, GameObject target = null) {
            if (DebugLogs) {
                Debug.Log($"[ThirdPersonCamera] {log}", target);
            }
        }
    }
}
