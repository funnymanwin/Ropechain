using TIM;
using UnityEngine;

namespace TIM
{
    public class CircleRopechainShape : RopechainShape
    {
        public float Radius = 1;
    
        private float GetCircleLength => 2 * Mathf.PI * Radius;
    
        public override bool IsReadyToUse()
        {
            if (Radius > 0)
                return true;
            else
                return false;
        }

        public override Vector3 GetElementPosition(float elementLength, int elementIndex, int elementsCount, bool useWorldSpace)
        {
            Vector3 pos = Quaternion.Euler(0,0, 360f * elementLength * elementIndex / GetCircleLength) * Vector3.right;
            if (useWorldSpace)
                pos += transform.position;

            return pos;
        }

        public override int GetMaxElementsCount(float elementLength)
        {
            return (int) (GetCircleLength / elementLength);
        }
    }
}