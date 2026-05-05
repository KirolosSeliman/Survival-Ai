using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EnricoBC.ThirdPersonCamera {
    public class UISliderValue : MonoBehaviour {
        public UnityEngine.UI.Text ValueText;

        void Awake() {
            ValueText.text = "" + GetComponent<UnityEngine.UI.Slider>().value;
        }

        public void OnChange(float value) {
            ValueText.text = $"{value}";
        }
    }
}