using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace TIM
{
#if UNITY_EDITOR
    [Icon("Assets/TIM/Ropechain/Icons/ChainRenderer.png")]
#endif
    [ExecuteAlways]
    public class RopechainRenderer_Chain : RopechainRenderer
    {
        [Title("Graphics:")]
        public Mesh Mesh;
        public Material Material;
        public ShadowCastingMode Shadows = ShadowCastingMode.On;
        public bool ReceiveShadows = true;
        
        [Title("Placement:")]
        public Vector3 Displacement;
        public Vector3 Rotation;
        public Vector3 DeltaRotationPerElement;
        public float Size = 1;
        public Vector3 Scale = Vector3.one;
        
        private Vector3 _origin;
        private Quaternion _originRotation = Quaternion.identity;
        
        public override void OnRegenerated(bool playMode)
        {
            
        }

        public override void OnShapeChanged(bool playMode)
        {
            _origin = Ropechain.UseWorldSpace ? Vector3.zero : Ropechain.transform.position;
            _originRotation = Ropechain.UseWorldSpace ? Quaternion.identity : Ropechain.transform.rotation;
        }

        private void Update()
        {
            Render();
        }

        private void Render()
        {
            if(!Ropechain || !Mesh || !Material || Ropechain.Count < 2)
                return;

            for (int i = 0; i < Ropechain.Count; i++)
            {
                Ropechain.Element element = Ropechain.Elements[i];
                if(element.LocalDirectionToNext.sqrMagnitude <= 0)
                    continue;
                
                Quaternion rot = Quaternion.LookRotation(element.LocalDirectionToNext);
                rot *= Quaternion.Euler(Rotation);
                rot *= Quaternion.Euler(DeltaRotationPerElement * i);
                Matrix4x4 originMatrix = Matrix4x4.identity;
                if (!Ropechain.UseWorldSpace)
                {
                    originMatrix = Matrix4x4.TRS(_origin, _originRotation, Vector3.one);
                }
                Matrix4x4 matrix = Matrix4x4.TRS(element.LocalPosition + rot * Displacement, rot, Size * Scale);

                Graphics.DrawMesh(Mesh, originMatrix * matrix, Material, gameObject.layer, null, 0, null, Shadows, ReceiveShadows);
            }
        }
    }
}
