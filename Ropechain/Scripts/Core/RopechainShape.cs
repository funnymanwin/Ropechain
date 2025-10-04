using UnityEngine;

namespace TIM
{
#if UNITY_EDITOR
    [Icon("Assets/TIM/Ropechain/Icons/RopechainShape.png")]
#endif
    public abstract class RopechainShape : MonoBehaviour
    {
        public abstract bool IsReadyToUse();
        public abstract Vector3 GetElementPosition(float elementLength, int elementIndex, int elementsCount, bool useWorldSpace);
        public abstract int GetMaxElementsCount(float elementLength);
    }
}