using UnityEngine;

namespace TIM
{
#if UNITY_EDITOR
    [Icon("Assets/TIM/Ropechain/Icons/RopechainRenderer.png")]
#endif
    public abstract class RopechainRenderer : MonoBehaviour
    {
        public Ropechain Ropechain
        {
            get => _ropechain;
            set
            {
                _ropechain = value;
                
                if (_ropechain && _ropechain.RopechainRenderer != this)
                    _ropechain.RopechainRenderer = this;
            }
        }

        [SerializeField, HideInInspector] private Ropechain _ropechain;
        
        /// <summary>
        /// Calls when Ropechain regenerated the list of Elements
        /// </summary>
        public abstract void OnRegenerated(bool playMode);

        /// <summary>
        /// Calls every time when physics simulation has been performed
        /// </summary>
        public abstract void OnShapeChanged(bool playMode);
    }
}