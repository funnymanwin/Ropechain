using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace TIM
{
#if UNITY_EDITOR
    [Icon("Assets/TIM/Ropechain/Icons/Ropechain.png")]
#endif
    [ExecuteAlways]
    public class Ropechain : MonoBehaviour
    {
        #region Fields

        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2), Optional]
        public RopechainShape Shape
        {
            get => _shape;
            set
            {
                _shape = value;
                Regenerate();
            }
        }
        
        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2), Optional]
        public RopechainRenderer RopechainRenderer
        {
            get => _ropechainRenderer;
            set
            {
                if (_ropechainRenderer)
                {
                    _ropechainRenderer.Ropechain = null;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(_ropechainRenderer);
#endif
                }
                
                _ropechainRenderer = value;

                if (_ropechainRenderer)
                {
                    if (_ropechainRenderer.Ropechain != this)
                    {
                        _ropechainRenderer.Ropechain = this;
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(_ropechainRenderer);
#endif
                    }
                    
                    _ropechainRenderer.OnRegenerated(Application.isPlaying);
                }
            }
        }
        
        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2), InlineButton(nameof(FitCountToRopechainShape), Label = " Fill", ShowIf = nameof(CountFitAvailable))]
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                Regenerate();
            }
        }

        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2)]
        public float ElementLength
        {
            get => _elementLength;
            set
            {
                _elementLength = value;
                if (_elementLength < 0.001f)
                    _elementLength = 0.001f;
                
                Regenerate();
            }
        }
        
        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2), PropertyRange(0, 1), HideIf(nameof(Shape))]
        public float GenerationCenter
        {
            get => _generationCenter;
            set
            {
                _generationCenter = Mathf.Clamp01(Mathf.Round(value * 20)/20);
                Regenerate();
            }
        }
        
        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2), ToggleLeft, ShowIf("@Count > 2")]
        public bool Looped
        {
            get => _looped;
            set
            {
                _looped = value;
            }
        }
        
        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2), ToggleLeft]
        public bool UseWorldSpace
        {
            get => _useWorldSpace;
            set
            {
                bool changed = _useWorldSpace != value;
                _useWorldSpace = value;
                
                if(changed)
                    Regenerate();
            }
        }
        
        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2), ToggleLeft]
        public bool PhysicalPrecalculation
        {
            get => _physicalPrecalculation;
            set
            {
                bool changed = _physicalPrecalculation != value;
                _physicalPrecalculation = value;
                
                if(changed)
                    Regenerate();
            }
        }

        [FoldoutGroup("General"), ShowInInspector, PropertyOrder(-2), MinValue(0), ShowIf(nameof(PhysicalPrecalculation))]
        public float PrecalculationDuration
        {
            get => _precalculationDuration;
            set
            {
                _precalculationDuration = Mathf.Clamp(Mathf.Round(value * 20) / 20, 0, 60);
                Regenerate();
            }
        }
        
        [FoldoutGroup("Events"), PropertyOrder(1)] public UnityEvent<Ropechain> RegeneratedEvent = new UnityEvent<Ropechain>();
        [FoldoutGroup("Events"), PropertyOrder(1)] public UnityEvent<Ropechain> ChangeEvent = new UnityEvent<Ropechain>();
        
        [BoxGroup("vis", false), PropertyOrder(2)] public GizmosParams gizmos;
        [PropertySpace(10), PropertyOrder(2), InlineEditor] public List<PinPoint> PinPoints = new List<PinPoint>();
        
        [Title("Procedural Data:")]
        [PropertySpace(20)] public List<Element> Elements { private set; get; } = new List<Element>();

        [SerializeField, HideInInspector] private RopechainShape _shape;
        [SerializeField, HideInInspector] private RopechainRenderer _ropechainRenderer;
        [SerializeField, HideInInspector] private int _count = 8;
        [SerializeField, HideInInspector] private float _elementLength = 0.1f;
        [SerializeField, HideInInspector] private float _generationCenter = 0.5f;
        [SerializeField, HideInInspector] private bool _looped;
        [SerializeField, HideInInspector] private bool _useWorldSpace = false;
        [SerializeField, HideInInspector] private bool _physicalPrecalculation;
        [SerializeField, HideInInspector] private float _precalculationDuration = 5f;

        public bool CountFitAvailable => Shape && Shape.GetMaxElementsCount(ElementLength) > Count;
        
        #endregion

        #region Unity's Methods

        private void OnEnable()
        {
            Regenerate();
            StartEditorPhysicsLoopAsync();
            if (Application.isPlaying && PhysicsEnabled)
            {
                StartRuntimePhysicsLoop();
            }
        }

        private async UniTask StartRuntimePhysicsLoop()
        {
            while (true)
            {
                await UniTask.Delay((int) (1000 * GetDeltaTime));
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                
                 if(this == null || !Application.isPlaying || !PhysicsEnabled || !enabled || CustomCall)
                     return;
                 
                 CalcPhysics(GetDeltaTime * SimulationSpeed);
            }
        }

        #endregion
        
        #region Basic Algorithms

        [Button(ButtonSizes.Medium, Icon = SdfIconType.ArrowClockwise, IconAlignment = IconAlignment.LeftEdge)]
        [GUIColor(0.4f, 0.8f, 1), HorizontalGroup("buttons", 110), PropertyOrder(-10)]
        public void Regenerate()
        {
            ClampCount();
            
            Elements.Clear();
            
            if (Shape && Shape.IsReadyToUse())
            {
                for (int i = 0; i < Count; i++)
                {
                    Vector3 p = Shape.GetElementPosition(ElementLength, i, Count, UseWorldSpace);
                
                    Element el = new Element(this, i, p);
                
                    Elements.Add(el);
                }
            }
            else // default straight line shape generation:
            {
                for (int i = 0; i < Count; i++)
                {
                    Vector3 p = new Vector3((i - (Count-1) * _generationCenter) * ElementLength, 0, 0);
                    if (UseWorldSpace)
                        p += transform.position;
                
                    Element el = new Element(this, i, p);
                
                    Elements.Add(el);
                }
            }

            if (PhysicalPrecalculation)
            {
                for (float t = 0; t < PrecalculationDuration; t+= Time.fixedDeltaTime)
                {
                    CalcPhysics(Time.fixedDeltaTime, true);
                }
            }
            
            RefreshElementsVectors();
            
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif

            if (_ropechainRenderer)
            {
                _ropechainRenderer.OnRegenerated(Application.isPlaying);
                _ropechainRenderer.OnShapeChanged(Application.isPlaying);
            }
            
            RegeneratedEvent?.Invoke(this);
            ChangeEvent?.Invoke(this);
        }

        public Vector3 GetElementWorldPosition(Vector3 localPos)
        {
            return UseWorldSpace ? localPos : transform.position + transform.rotation * localPos;
        }
        
        public Vector3 GetElementLocalPosition(Vector3 worldPos)
        {
            return UseWorldSpace ? worldPos : Quaternion.Inverse(transform.rotation) * (worldPos - transform.position);
        }

        public int GetElementIndexFromPosition01(float position01)
        {
            return Mathf.RoundToInt(position01 * (Elements.Count-1));
        }

        public void FitCountToRopechainShape()
        {
            if(!Shape || !Shape.IsReadyToUse())
                return;

            Count = Shape.GetMaxElementsCount(ElementLength);
        }
        
        private void ClampCount()
        {
            if (Shape && Shape.IsReadyToUse())
                _count = Mathf.Clamp(_count, 0, Shape.GetMaxElementsCount(ElementLength));
            else
                _count = Mathf.Clamp(_count, 0, 1024);

            if (_count < 3)
                _looped = false;
        }

        private void RefreshElementsVectors()
        {
            if(Count < 2)
                return;
            
            int totalCount = Looped ? Count + 1 : Count;
            for (int i = 0; i < totalCount; i++)
            {
                Element previous;
                Element current;
                Element next;
                if (i == Count)
                {
                    previous = Elements[i-1];
                    current = Elements[0];
                    next = Elements[1];
                }
                else if (i == Count - 1)
                {
                    previous = Elements[i-1];
                    current = Elements[i];
                    if (Looped)
                        next = Elements[0];
                    else
                        next = null;
                }
                else if(i == 0)
                {
                    if (Looped)
                        previous = Elements[^1];
                    else
                        previous = null;
                    current = Elements[i];
                    next = Elements[i+1];
                }
                else
                {
                    previous = Elements[i-1];
                    current = Elements[i];
                    next = Elements[i+1];
                }

                if (next != null)
                    current.LocalDirectionToNext = next.LocalPosition - current.LocalPosition;
                else
                    current.LocalDirectionToNext = current.LocalPosition - previous.LocalPosition;

                if (previous != null)
                    current.LocalDirectionToPrevious = previous.LocalPosition - current.LocalPosition;
                else
                    current.LocalDirectionToPrevious = current.LocalPosition - next.LocalPosition;
                
                Vector3 upNormal = Vector3.Cross(current.LocalDirectionToNext, Vector3.right);
                if (upNormal == Vector3.zero)
                    upNormal = Vector3.up;
                else
                    upNormal = upNormal.normalized;
                current.Up = upNormal;
                current.Right = Vector3.Cross(current.Up, current.LocalDirectionToNext);
            }
        }

        #endregion
        
        #region Physics

        [FoldoutGroup("Physics"), ShowInInspector, ToggleLeft, PropertyOrder(-1)]
        public bool PhysicsEnabled
        {
            get => _physicsEnabled;
            set
            {
                bool changed = _physicsEnabled != value;
                _physicsEnabled = value;
                if(changed)
                    Regenerate();

                if (_physicsEnabled)
                {
                    if (Application.isPlaying)
                        StartRuntimePhysicsLoop();
                    else
                        StartEditorPhysicsLoopAsync();
                }
            }
        }
        
        [FoldoutGroup("Physics"), ShowIf(nameof(EditorPreviewAvailable)), ShowInInspector, ToggleLeft, PropertyOrder(-1)]
        public bool EditorPreview
        {
            get => _editorPreview;
            set
            {
                bool changed = _editorPreview != value;
                _editorPreview = value;
                if(changed)
                    Regenerate();

                if(_editorPreview)
                    StartEditorPhysicsLoopAsync();
            }
        }
        
        [FoldoutGroup("Physics"), ShowIf(nameof(IsSimulationPlaying)), ShowInInspector, ToggleLeft, PropertyOrder(-1)]
        public bool Pause
        {
            get => _pause;
            set
            {
                _pause = value;
            }
        }

        [Title("Basic options:")]
        [FoldoutGroup("Physics"), Min(0)] public float PointRadius = 0.01f;
        [FoldoutGroup("Physics")] public float Gravity = -9.8f;
        [FoldoutGroup("Physics"), MinValue(0)] public float SimulationSpeed = 1;
        [FoldoutGroup("Physics"), Range(0, 1)] public float Bounciness = 0;
        [FoldoutGroup("Physics"), Range(0, 1)] public float Friction = 0.5f;
        [FoldoutGroup("Physics"), Range(0, 1)] public float AirFriction = 0.05f;
        
        [Title("Collision:")]
        [FoldoutGroup("Physics"), ToggleLeft] public bool Collision = true;
        [FoldoutGroup("Physics"), ToggleLeft] public bool SelfCollision = false;
        [FoldoutGroup("Physics")] public LayerMask CollisionLayerMask = 1;
        
        [Title("Advanced options:")]
        [FoldoutGroup("Physics"), ToggleLeft] public bool UnscaledTime;
        [FoldoutGroup("Physics"), ToggleLeft] public bool CustomSimulationRate;
        [FoldoutGroup("Physics"), ToggleLeft, ShowIf(nameof(CustomSimulationRate))] public bool CustomCall;
        [FoldoutGroup("Physics"), Range(1, 120), ShowIf(nameof(CustomSimulationRate))] public int SimulationRate = 50;
        [FoldoutGroup("Physics"), Range(0, 1)] public float Hardness = 0.75f;
        [FoldoutGroup("Physics")] public ContractionQualityEnum ContractionQuality = ContractionQualityEnum.Medium;
        [FoldoutGroup("Physics"), Min(0)] public float ContractionForce = 50;

        public bool IsSimulationPlaying => PhysicsEnabled && (Application.isPlaying || EditorPreview);
        private bool EditorPreviewAvailable => !Application.isPlaying && this != null && enabled && PhysicsEnabled && gameObject.activeInHierarchy;
        public float GetDeltaTime
        {
            get
            {
                if (UnscaledTime)
                    return CustomSimulationRate ? 1f / SimulationRate : Time.fixedUnscaledDeltaTime;
                else
                    return  CustomSimulationRate ? Time.timeScale / SimulationRate : Time.fixedDeltaTime;
            }
        }

        [SerializeField, HideInInspector] private bool _physicsEnabled = true;
        private bool _editorPreview;
        [SerializeField, HideInInspector] private bool _pause;

        private Vector3 _lastPos;
        private Quaternion _lastRot;
        private bool IsPlaying => Application.isPlaying;

        private async UniTask StartEditorPhysicsLoopAsync()
        {
            if (!EditorPreviewAvailable || !EditorPreview)
            {
                _editorPreview = false;
                return;
            }
            
            while (true)
            {
                await UniTask.Delay((int)(GetDeltaTime * 1000));

                if (!EditorPreviewAvailable || !EditorPreview)
                {
                    _editorPreview = false;
                    return;
                }

                CalcPhysics(GetDeltaTime * SimulationSpeed);
            }
        }

        public void CalcPhysics(float deltaTime, bool precalculationMode = false)
        {
            if(!precalculationMode && Pause)
                return;

            ApplyGravityAndAirFriction(deltaTime);
            ApplyVelocity(deltaTime);
            ApplyPinPoints();

            ApplyContraction(!precalculationMode);
            
            if(Collision)
                ApplyCollision();

            if (SelfCollision)
                ApplySelfCollision();

            if (!precalculationMode)
            {
                RefreshElementsVectors();
                if(_ropechainRenderer)
                    _ropechainRenderer.OnShapeChanged(Application.isPlaying);
                
                ChangeEvent?.Invoke(this);
            }
        }

        private void ApplyGravityAndAirFriction(float deltaTime)
        {
            foreach (Element element in Elements)
            {
                // reset pins:
                element.Pinned = false;
                element.HardPinned = false;
                
                // physics:
                element.WorldVelocity += Gravity * deltaTime * Vector3.up;
                element.WorldVelocity *= 1-AirFriction;
            }
        }

        private void ApplyPinPoints()
        {
            if(Elements == null || Elements.Count == 0)
                return;
            
            foreach (PinPoint pinPoint in PinPoints)
            {
                if (pinPoint == null || !pinPoint.Enabled)
                    continue;
                
                var el = Elements[GetElementIndexFromPosition01(pinPoint.PositionInChain)];
                el.PinWorldPos = pinPoint.transform.position;
                el.Pinned = true;
                el.HardPinned = pinPoint.Hard;
                el.WorldPosition = el.PinWorldPos;
                el.WorldVelocity = Vector3.zero;
                if(pinPoint.Hard)
                    el.LastCastWorldPosition = el.PinWorldPos;
            }
        }
        
        private void ApplyVelocity(float deltaTime)
        {
            foreach (Element element in Elements)
            {
                element.WorldPosition += element.WorldVelocity * deltaTime;
            }
        }

        private void ApplyCollision()
        {
            foreach (Element el in Elements)
            {
                el.CastCollision();
            }
        }

        private void ApplySelfCollision()
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                Element current = Elements[i];
                for (int t = i+1; t < Elements.Count; t++)
                {
                    Element target = Elements[t];

                    Vector3 currentPos = current.LocalPosition;
                    Vector3 targetPos = target.LocalPosition;

                    float distanceSqr = (targetPos - currentPos).sqrMagnitude;
                    if (distanceSqr < PointRadius * PointRadius * 4)
                    {
                        Vector3 midPoint = (currentPos + targetPos)/2;
                        Vector3 toTargetDir = (targetPos - currentPos).normalized;
                        target.LocalPosition = midPoint + toTargetDir * PointRadius;
                        current.LocalPosition = midPoint - toTargetDir * PointRadius;
                    }
                }
            }
        }

        private void ApplyContraction(bool calcVelocity = true)
        {
            if(Elements.Count < 2)
                return;
            
            int contractionIterations = (int)ContractionQuality;

            for (int iteration = 0; iteration < contractionIterations; iteration++)
            {
                int totalCount = Looped ? Count + 1 : Count;
                for (int i = 1; i < totalCount; i++)
                {
                    Element previous;
                    Element current;
                    if (i == Count)
                    {
                        previous = Elements[^1];
                        current = Elements[0];
                    }
                    else
                    {
                        previous = Elements[i - 1];
                        current = Elements[i];
                    }
                    
                    Vector3 currentPos = current.LocalPosition;
                    Vector3 previousPos = previous.LocalPosition;

                    Vector3 dir = currentPos - previousPos;
                    dir = dir.normalized;
                    Vector3 velocityBonus = (previous.LocalPosition - current.LocalPosition) + dir * ElementLength;
                    if (!UseWorldSpace)
                        velocityBonus = transform.rotation * velocityBonus;
                    velocityBonus = Hardness * ContractionForce * velocityBonus / contractionIterations;

                    if (!current.HardPinned)
                    {
                        current.WorldVelocity += velocityBonus;
                        current.LocalPosition = Vector3.Lerp(currentPos, previousPos + dir * ElementLength, Hardness);
                    }

                    
                    if(!previous.HardPinned)
                    {
                        previous.WorldVelocity -= velocityBonus;
                        previous.LocalPosition = Vector3.Lerp(previousPos, currentPos - dir * ElementLength, Hardness);
                    }
                    
                }
            }
        }

        #endregion
        
        #region Visualization

        private void OnDrawGizmos()
        {
            if(!gizmos.Enabled || !gizmos.DrawAlways)
                return;
            
            DrawLines();
            if(gizmos.DrawPoints)
                DrawPoints();
        }
        
        private void OnDrawGizmosSelected()
        {
            if(!gizmos.Enabled || gizmos.DrawAlways)
                return;

            DrawLines();            
            if(gizmos.DrawPoints)
                DrawPoints();
        }

        private void DrawLines()
        {
            if(Elements.Count < 2)
                return;
            
            Gizmos.color = gizmos.Color;
            int totalCount = Looped ? Elements.Count+1 : Elements.Count;
            for (var i = 1; i < totalCount; i++)
            {
                if(i == Elements.Count)
                    Gizmos.DrawLine(Elements[0].WorldPosition, Elements[^1].WorldPosition);
                else
                    Gizmos.DrawLine(Elements[i-1].WorldPosition, Elements[i].WorldPosition);
            }
        }
        
        private void DrawPoints()
        {
            for (var i = 0; i < Elements.Count; i++)
            {
                Vector3 elementWorldPos = Elements[i].WorldPosition;
                bool pinned = false;
                foreach (PinPoint pinPoint in PinPoints)
                {
                    if(pinPoint == null || !pinPoint.Enabled)
                        continue;

                    if (GetElementIndexFromPosition01(pinPoint.PositionInChain) == i)
                    {
                        pinned = true;
                        var pinPointPos = pinPoint.transform.position;
                        Gizmos.color = gizmos.PinnedColor;
                        Gizmos.DrawWireSphere(pinPointPos, PointRadius);
                        Gizmos.DrawLine(pinPointPos, elementWorldPos);
                        break;
                    }
                }
                
                if(!pinned)
                    Gizmos.color = gizmos.Color;
                
                Gizmos.DrawSphere(elementWorldPos, PointRadius);
            }
        }
        
        [System.Serializable]
        public class GizmosParams
        {
            [ToggleLeft] public bool Enabled = true;
            [ToggleLeft] public bool DrawAlways = true;
            [ToggleLeft] public bool DrawPoints = true;
            public Color Color = Color.green;
            public Color PinnedColor = Color.red;
        }

        #endregion

        public enum ContractionQualityEnum
        {
            Low = 1,
            Medium = 3,
            High = 6,
            Ultra = 9,
        }
        
        [System.Serializable]
        public class Element
        {
            public Vector3 WorldPosition
            {
                get => Ropechain.GetElementWorldPosition(LocalPosition);
                set { LocalPosition = Ropechain.GetElementLocalPosition(value); }
            }
            public Vector3 LocalPosition;
            
            public Vector3 Up;
            public Vector3 Right;
            public Vector3 LocalDirectionToNext;
            public Vector3 LocalDirectionToPrevious;

            public Vector3 WorldVelocity
            {
                get => _worldVelocity;
                set
                {
                    _worldVelocity = Vector3.ClampMagnitude(value, 5);
                }
            }

            public Vector3 LastCastWorldPosition;

            public bool Pinned { set; get; }
            public bool HardPinned { set; get; }
            public Vector3 PinWorldPos { set; get; }
            
            [HideInInspector] public int Index;
            [HideInInspector] public Ropechain Ropechain;
            [SerializeField, HideInInspector] private Vector3 _worldVelocity;
            
            private RaycastHit _collisionHit = new RaycastHit();

            public Element(Ropechain ropechain, int index, Vector3 localPosition)
            {
                Ropechain = ropechain;
                Index = index;
                LocalPosition = localPosition;
                LastCastWorldPosition = WorldPosition;
            }

            public void CastCollision()
            {
                if(HardPinned)
                    return;
                
                Vector3 current = WorldPosition;

                if (Physics.Linecast(LastCastWorldPosition, current, out _collisionHit, Ropechain.CollisionLayerMask))
                {
                    Vector3 newPos = _collisionHit.point + _collisionHit.normal * 0.002f;
                    Vector3 deltaVector = newPos - current;
                    if (deltaVector.sqrMagnitude > Mathf.Pow(Ropechain.ElementLength * 2, 2))
                        deltaVector = deltaVector.normalized * Ropechain.ElementLength;
                    
                    newPos = current + deltaVector;
                    WorldPosition = newPos;
                    Vector3 normalProjection = Vector3.Project(WorldVelocity, _collisionHit.normal);
                    Vector3 planeProjection = WorldVelocity - normalProjection;
                    WorldVelocity = planeProjection * (1-Ropechain.Friction) - normalProjection * Ropechain.Bounciness;
                    LastCastWorldPosition = newPos;
                }
                else
                {
                    LastCastWorldPosition = current;
                }
            }
        }
    }
}
