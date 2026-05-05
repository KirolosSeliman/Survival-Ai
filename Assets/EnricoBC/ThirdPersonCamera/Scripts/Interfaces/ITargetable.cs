using UnityEngine;

namespace EnricoBC.ThirdPersonCamera {
    public interface ITargetable {
        Vector3 TargetPosition { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool CanBeTargeted();
    }
}