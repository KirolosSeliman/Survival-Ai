using System;
using UnityEngine;

namespace EnricoBC.ThirdPersonCamera {
    public class TargetData : IComparable {
        public Transform Target { get; set; }
        public float DistanceFromPlayer { get; set; }
        public float AngleForward { get; set; }
        public float AngleRight { get; set; }
        public static bool SortForward = true;

        public int CompareTo(object other) {
            TargetData other_data = (TargetData)other;
            if (SortForward) {
                if (this.AngleForward > other_data.AngleForward)
                    return 1;
                else if (this.AngleForward < other_data.AngleForward)
                    return -1;
            } else {
                if (this.AngleRight > other_data.AngleRight)
                    return 1;
                else if (this.AngleRight < other_data.AngleRight)
                    return -1;
            }
            return 0;
        }
    }
}