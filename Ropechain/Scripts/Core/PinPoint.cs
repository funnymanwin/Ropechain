using Sirenix.OdinInspector;
using UnityEngine;

namespace TIM
{
#if UNITY_EDITOR
    [Icon("Assets/TIM/Ropechain/Icons/PinPoint.png")]
#endif
    public class PinPoint : MonoBehaviour
    {
        [ToggleLeft, PropertyOrder(-1)] public bool Enabled = true;
        [Tooltip("Enable it, if you need to pin Chain element to point even when physically it's not possible")]
        [ToggleLeft, PropertyOrder(-1)] public bool Hard = true;

        [ShowInInspector, PropertyRange(0, 1), PropertyOrder(-1), LabelWidth(110)]
        public float PositionInChain
        {
            get => _positionInChain;
            set
            {
                _positionInChain = value;
                if (_positionInChain > 1)
                    _positionInChain %= 1f;
                else if (_positionInChain < 0)
                {
                    _positionInChain = 1 - Mathf.Abs(_positionInChain) % 1;
                }
            }
        }

        [SerializeField, HideInInspector] private float _positionInChain = 0.5f;
    }
}