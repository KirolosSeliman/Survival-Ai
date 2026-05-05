using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EnricoBC.ThirdPersonCamera {
    public class TargetableCapsule : MonoBehaviour, ITargetable {
        public bool EnableTarget = true;

        public bool CanBeTargeted() {
            return EnableTarget;
        }

        public Vector3 TargetPosition {
            get {
                // TODO: get center by calling Renderer.bounds.center
                return Vector3.zero;
            }
        }
    }
}