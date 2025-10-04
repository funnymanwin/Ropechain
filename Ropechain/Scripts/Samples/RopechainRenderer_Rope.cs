using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace TIM
{
#if UNITY_EDITOR
    [Icon("Assets/TIM/Ropechain/Icons/RopeRenderer.png")]
#endif
    [ExecuteAlways]
public class RopechainRenderer_Rope : RopechainRenderer
{
    [FoldoutGroup("Geometry"), ShowInInspector, PropertyOrder(-1)]
    public int EdgeCount
    {
        get => _edgeCount;
        set
        {
            _edgeCount = Mathf.Clamp(value, 3, 16);
            InitMesh();
        }
    }
    [FoldoutGroup("Geometry"), ShowInInspector, PropertyRange(0, 4), PropertyOrder(-1)]
    public int Subdivisions
    {
        get => _subdivisions;
        set
        {
            _subdivisions = Mathf.Clamp(value, 0, 4);
            InitMesh();
        }
    }
    [FoldoutGroup("Geometry"), ShowInInspector, PropertyOrder(-2)] public float Radius
    {
        get => _radius;
        set
        {
            _radius = Mathf.Max(value, 0);
            UpdateForm();
        }
    }
    
    [FoldoutGroup("Rendering")] public Material Material;
    [FoldoutGroup("Rendering")] public ShadowCastingMode Shadows = ShadowCastingMode.On;
    [FoldoutGroup("Rendering")] public bool ReceiveShadows = true;

    [FoldoutGroup("Rendering"), ShowInInspector]
    public Vector2 UVScale
    {
        get => _uvScale;
        set
        {
            _uvScale = value;
            UpdateForm();
        }
    }

    public Vector3[] Vertices {private set;get;}
    public Vector3[] Normals {private set;get;}
    public Vector2[] UVs {private set;get;}
    public int[] Triangles {private set;get;}
    
    [SerializeField, HideInInspector] private int _edgeCount = 4;
    [SerializeField, HideInInspector] private int _subdivisions = 1;
    [SerializeField, HideInInspector] private float _radius = 0.05f;
    [SerializeField, HideInInspector] private Vector2 _uvScale = Vector2.one;
    
    
    private Mesh Mesh
    {
        get
        {
            if(!_mesh)
                InitMesh();
            
            return _mesh;
        }
        set => _mesh = value;
    }

    private Mesh _mesh;
    private Vector3 _origin;
    private Quaternion _rotation;
    private RopePoint[] RopePoints;

    public override void OnRegenerated(bool playMode)
    {
        InitMesh();
    }

    public override void OnShapeChanged(bool playMode)
    {
        UpdateForm();
    }

    private void InitMesh()
    {
        if(!_mesh)
            _mesh = new Mesh();
        
        if(Radius <= 0 || !Ropechain || Ropechain.Count < 2)
            return;
        
        _origin = Ropechain.UseWorldSpace ? Vector3.zero : Ropechain.transform.position;
        _rotation = Ropechain.UseWorldSpace ? Quaternion.identity : Ropechain.transform.rotation;

        int segmentsCount = Ropechain.Count * (1 + Subdivisions);
        if (Ropechain.Looped)
            segmentsCount++;
        
        Vertices = new Vector3[EdgeCount * segmentsCount];
        Normals = new Vector3[EdgeCount * segmentsCount];
        UVs = new Vector2[EdgeCount * segmentsCount];
        Triangles = new int[3 * 2 * EdgeCount * (segmentsCount - 1)];
        
        UpdateForm();
    }
    
    private void UpdateForm()
    {
        if(Radius <= 0 || !Ropechain || Ropechain.Count < 2)
            return;
        
        // draw:
        float circleLength = 2 * Mathf.PI * _radius;
        int trianglePointsPerElement = 3 * 2 * EdgeCount;
        for (int elementIndex = 0; elementIndex < Ropechain.Count; elementIndex++)
        {
            Ropechain.Element el = Ropechain.Elements[elementIndex];
            if(el.LocalDirectionToNext.sqrMagnitude <= 0)
                continue;

            for (int i = 0; i < EdgeCount; i++)
            {
                Vector3 midDir = el.LocalDirectionToNext;
                if (elementIndex > 0)
                {
                    midDir += Ropechain.Elements[elementIndex - 1].LocalDirectionToNext;
                    midDir /= 2;
                }
                Quaternion forward = Quaternion.LookRotation(midDir);
                Vector3 normal = forward * Quaternion.Euler(0,0,360f * i / EdgeCount) * Vector3.up;
                Vertices[elementIndex*EdgeCount + i] = el.LocalPosition + normal * Radius;
                Normals[elementIndex * EdgeCount + i] = normal;
                UVs[elementIndex * EdgeCount + i] = new Vector2(UVScale.x * circleLength * i / EdgeCount, UVScale.y * el.Index * Ropechain.ElementLength);
            }

            if (elementIndex < Ropechain.Count-1 || Ropechain.Looped)
            {
                int nextElementIndex = elementIndex == Ropechain.Count - 1 ? 0 : elementIndex + 1;
                
                for (int edge = 0; edge < EdgeCount; edge++)
                {
                    if (edge == EdgeCount-1)
                    {
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 0] = (nextElementIndex) * EdgeCount + edge;
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 1] = elementIndex * EdgeCount + edge;
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 2] = (nextElementIndex) * EdgeCount + 0;
                    
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 3] = elementIndex * EdgeCount + edge;
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 4] = elementIndex * EdgeCount + 0;
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 5] = (nextElementIndex) * EdgeCount + 0;
                    }
                    else
                    {
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 0] = (nextElementIndex) * EdgeCount + edge;
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 1] = elementIndex * EdgeCount + edge;
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 2] = (nextElementIndex) * EdgeCount + edge+1;
                    
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 3] = elementIndex * EdgeCount + edge;
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 4] = elementIndex * EdgeCount + edge+1;
                        Triangles[(elementIndex) * trianglePointsPerElement + edge * 6 + 5] = (nextElementIndex) * EdgeCount + edge+1;
                    }
                }
            }
        }

        Mesh.Clear();
        Mesh.vertices = Vertices;
        Mesh.normals = Normals;
        Mesh.triangles = Triangles;
        Mesh.uv = UVs;
        Mesh.RecalculateBounds();
    }

    private void Update()
    {
        if(!Ropechain || !Mesh || !Material)
            return;
        
        Matrix4x4 matrix = Matrix4x4.TRS(_origin, _rotation, Vector3.one);
        Graphics.DrawMesh(Mesh, matrix, Material, gameObject.layer, null, 0, null, Shadows, ReceiveShadows);
    }

    private void RefreshPointsArray()
    {
        RopePoints = new RopePoint[Ropechain.Count * (1 + Subdivisions)];
        int pointsPerEl = Subdivisions + 1;
        for (int i = 0; i < Ropechain.Count; i++)
        {
            for (int p = 0; p < pointsPerEl; p++)
            {
                var el = Ropechain.Elements[i];
                RopePoints[i + p] = new RopePoint()
                {
                    LocalPosition = el.LocalPosition + el.LocalDirectionToNext * (p*1f/pointsPerEl)
                };
            }
        }
    }

    private struct RopePoint
    {
        public Vector3 LocalDirectionToNext;
        public Vector3 LocalPosition;
    }
}

}
