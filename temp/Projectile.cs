using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

public partial class Projectile : FormattedMonoBehaviour
{
    #region <Consts>

    private const int UnitColliderLayerMask = 1 << 11;
    private const int ObstacleTerrainColliderLayerMask = 1 << 1 | 1 << 13;
    private const int MaxAcceptPointsOfBezier = 5;
    private const int MaxSizeOfBuffer = 16;

    #endregion    
    
    #region <Fields>
    
    /* Key for Validate */
    public ProjectileManager.Type Type { get; private set; }
    protected bool _isActive;
    public bool IsActive { get { return _isActive; } }
    public Unit Caster { get; private set; }
    [NonSerialized] public int RecursiveKey;
    
    /* Kinematic */
    public Vector3 Velocity { get; private set; }
    public Vector3 Direction { get { return Velocity.normalized; }}
    public Vector3 Acceleration { get; private set; }
    public float AccelerationFactor { get; private set; }
    public float Gravity { get; private set; }
    public Vector3 PeekedNestPosition;

    /* Collision Box Size */
    private ProjectileType _projectileType = ProjectileType.Box ;
    private Vector3 _halfExtentsForTerrain = Vector3.one * 0.003f + Vector3.forward;
    private Vector3 _halfExtents = Vector3.one * 0.003f;

    /* Collider member */
    public RaycastHit[] CastHitGroup { get; private set; }
    public Collider[] ColliderGroup { get; private set; }
    public List<Unit> CollidedUnitGroup { get; private set; }
    public List<Unit> ExCollidedUnitGroup { get; private set; }
    protected int _maxColliderNumber;    
    protected bool _isIgnoreObstacle;
    protected bool _isIgnoreUnit;
    private int _castHitNumber, _overLapCollideNumber;
    
    /* Action */
    private Action<CustomEventArgs.CommomActionArgs> _collideUnitAction;
    private Action<CustomEventArgs.CommomActionArgs> _collideObstacleAction;
    private Action<CustomEventArgs.CommomActionArgs> _updateAction;
    private Action<CustomEventArgs.CommomActionArgs> _expiredLifeAction;
    private Action<CustomEventArgs.CommomActionArgs> _onHeartBeatAction;
    private Action<CustomEventArgs.CommomActionArgs> _onRemoveStart,_onRemoveEnd;
    private Action<CustomEventArgs.CommomActionRefs> _onWhileRemovingAction;
    
    /* Heart beat */
    private int _onHeartBeatEventTension;
    private int _onHeartBeatEventTerm;
    private int _onHeartBeatEventCounter;
    private bool _isOnHeartBeat;
	    
    /* Trail Management */
    protected TrailRenderer _mTrail;
    
    /* Particle Management */
    protected ParticleSystem[] _particleList;
    protected bool _particleStopLoopWhenRemoveStartFlag;
    protected bool[] _stackedParticleLoopOriginFlagSet;
    
    /* Bezier Management */
    private Vector3[] CopiedBezierPointSet;
    
    /* Time Unit Management */
    private int _maxNumberOfHitTime;
    private float innerElapsedTime;
    public float ElapsedTime
    {
        get { return innerElapsedTime; }
        set
        {
            innerElapsedTime = value;
            if (_currentMoveAction.isValid && innerElapsedTime > _currentMoveActionCumulativeTime)
            {
                if(_currentMoveAction.ExpireAction!=null) {
                    _currentMoveAction.ExpireAction(new CustomEventArgs.CommomActionArgs()
                    .SetMorphable(this),_currentMoveAction);
                    _currentMoveAction.ExpireAction = null;
                }
                _currentMoveAction.isValid = false;
            }
        }
    }
    public float LifeTime { get; private set; }
    
    /* Arrow Viewing Relate */
    [NonSerialized] public Renderer _mRenderer;
    private bool _arrowFadeWhenRemovingStartFlag, _arrowFadeWhenRemovingWholeFlag;
    
    /* Deferred Remove Relate */
    [NonSerialized] public float RemoveDelay;
    [NonSerialized] public int RemoveDelayCount;
    [NonSerialized] public bool RemoveDeadlock;    

    /*Listed Projectile Move Action*/
    public List<Vector3> _moveActionSequenceValueSet;
    [NonSerialized] public float _currentMoveActionCumulativeTime;
    private Queue<ProjectileAnimationEventArgs> _projectileMoveActionSequenceList;
    private ProjectileAnimationEventArgs _currentMoveAction;
    
    #endregion </Fields>

    #region <Enum>
    
    public enum ProjectileType{
        Point,
        Box,
        Sphere
    }

    #endregion

    #region <Unity/Callbacks>
    
    protected void Awake()
    {
        _mTrail = GetComponentInChildren<TrailRenderer>();
        _mRenderer = GetComponentInChildren<Renderer>();
        _particleList = GetComponentsInChildren<ParticleSystem>();
        
        _stackedParticleLoopOriginFlagSet = new bool[_particleList.Length];
        CopiedBezierPointSet = new Vector3[MaxAcceptPointsOfBezier];
        CastHitGroup = new RaycastHit[MaxSizeOfBuffer];
        ColliderGroup = new Collider[MaxSizeOfBuffer];
        
        CollidedUnitGroup = new List<Unit>();
        ExCollidedUnitGroup = new List<Unit>();
        _moveActionSequenceValueSet = new List<Vector3>();
        
        _projectileMoveActionSequenceList = new Queue<ProjectileAnimationEventArgs>();
        
        for (var i = 0; i < _stackedParticleLoopOriginFlagSet.Length; i++)
        {
            _stackedParticleLoopOriginFlagSet[i] = _particleList[i].loop;
        }
    }    

    protected void FixedUpdate()
    {
        if (!_isActive) return;
        if (_arrowFadeWhenRemovingWholeFlag) _mRenderer.enabled = false;
        PeekedNestPosition = _Transform.position;
        
    #region <ProjectileMoveAnimation>
        PeekNextPositon();
    #endregion

    #region <ObstacleCollideCheck>
        CheckCollisionAboutObstaclesAndTerrain(_collideObstacleAction);
    #endregion

    #region <UnitCollideCheck>
        CheckCollisionAboutUnit(_collideUnitAction);
    #endregion

    #region <ApplyMove>

    ApplyMoveAndDirection(PeekedNestPosition);
    ApplyTimeUnitForward();
        
    #endregion
        
    #region <TimeExpiredCheck>

        // CASE#3: life time has been expired.
        if (LifeTime - ElapsedTime < .0f && _isActive)
        {
            // Defined Action: Expired.
            if (_expiredLifeAction != null)
            {
                _expiredLifeAction(new CustomEventArgs.CommomActionArgs()
                    .SetMorphable(this)
                );
            }
            Remove();
        }

    #endregion
        
    }
    
    #endregion </Unity/Callbacks>
    
    #region <Callbacks>

    public override void OnCreated()
    {
        PeekedNestPosition = Vector3.zero;
        _castHitNumber = _overLapCollideNumber = 0;
        _currentMoveActionCumulativeTime = 0f;
        RemoveDeadlock = false;
        _particleStopLoopWhenRemoveStartFlag = _arrowFadeWhenRemovingStartFlag = _arrowFadeWhenRemovingWholeFlag = false;
        _moveActionSequenceValueSet.Clear();
        _currentMoveAction.isValid = false;
        _projectileMoveActionSequenceList.Clear();
        RecursiveKey = 0;
        Gravity = 0f;
        AccelerationFactor = 1f;
        _isIgnoreObstacle = _isIgnoreUnit = false;
        _isActive = false;
        _isOnHeartBeat = false;
        _collideUnitAction = null;
        _collideObstacleAction = null;
        _expiredLifeAction = null;
        _onHeartBeatAction = null;
        _onWhileRemovingAction = null;
        _onRemoveStart = null;
        _onRemoveEnd = null;
        
        Type = ProjectileManager.Type.None;
        _maxColliderNumber = 0;
        CollidedUnitGroup.Clear();
        ExCollidedUnitGroup.Clear();

        _mRenderer.enabled = true;
        Velocity = Acceleration = Vector3.zero;
        SetDirection(Vector3.forward);
        LifeTime = ElapsedTime = 0f;
        RemoveDelayCount = 0;
        RemoveDelay = 0f;
        _projectileType = ProjectileType.Box;
        _maxNumberOfHitTime = 1;
    }
    
    public override void OnRemoved()
    {
        if (_onRemoveEnd != null)
        {
            _onRemoveEnd(new CustomEventArgs.CommomActionArgs().SetMorphable(this));
        }

        if (_arrowFadeWhenRemovingStartFlag || _arrowFadeWhenRemovingWholeFlag) _mRenderer.enabled = true; 
        
        if (!_particleStopLoopWhenRemoveStartFlag)
        {
            for (var i = 0 ; i < _particleList.Length ; i++)
            {
                _particleList[i].loop = _stackedParticleLoopOriginFlagSet[i];
            }
        }

        if (_mTrail != null)
        {
            _mTrail.Clear();
        }
    }
    
    public virtual void OnHeartBeat()
    {
        if (!_isActive || !_isOnHeartBeat) return;
        if (_arrowFadeWhenRemovingWholeFlag) _mRenderer.enabled = false;
        PeekedNestPosition = _Transform.position;
        
        #region <ProjectileMoveAnimation>
        PeekNextPositon();
        #endregion

        if (_onHeartBeatEventTerm > 0)
        {
            if(_onHeartBeatEventTerm > _onHeartBeatEventCounter)
            {
                _onHeartBeatEventCounter++;
                return;
            }
            _onHeartBeatEventCounter = 0;
        }

        if (_onHeartBeatEventTension > 0)
        {
            #region <UnitCollideCheck>
            if (CheckCollisionAboutUnit(_onHeartBeatAction)) return;
            #endregion
            --_onHeartBeatEventTension;
        }
    }

    #endregion </Callbacks>

    #region <Methods>

    public void Remove()
    {
        if (RemoveDeadlock) return;
        if (_onRemoveStart != null) _onRemoveStart(new CustomEventArgs.CommomActionArgs().SetMorphable(this));
        
        if (RemoveDelay <= Mathf.Epsilon)
        {
            ObjectManager.RemoveObject(this);
        }
        else
        {
            _isActive = false;
            RemoveDeadlock = true;
            if (_arrowFadeWhenRemovingStartFlag) _mRenderer.enabled = false; 
            if (_particleStopLoopWhenRemoveStartFlag)
            {
                for (var i = 0 ; i < _particleList.Length ; i++)
                {
                    _particleList[i].loop = false;
                }
            }
            
            #region <PooledCoroutineForAsyncProjectilRemove>
            var lContent = ObjectManager.GetInstance.GetObject<K514PooledCoroutine>(ObjectManager.PoolTag.Coroutine,
                    K514PrefabStorage.GetInstance.GetPrefab(K514PrefabStorage.PrefabType.PooledCoroutine));
            lContent._mParams.SetMorphable(this);
            lContent.SetAction(K514PooledCoroutine.ActionType.EndTrigger,ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        var result = 0.1f * proj.RemoveDelayCount > proj.RemoveDelay;
                        if (!result) proj.RemoveDelayCount++;
                        return result;
                    }
                )
                .SetInterval(0.1f, K514PooledCoroutine.DelayType.Interval)
                .SetAction(K514PooledCoroutine.ActionType.Finalize, ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        ObjectManager.RemoveObject(proj);
                    }
                );
            if (_onWhileRemovingAction != null)
            {
                lContent.SetAction(K514PooledCoroutine.ActionType.Activity, ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        proj._onWhileRemovingAction(ano);
                    }
                );
            }
            lContent.SetTrigger();
            #endregion
            
        }
    }

    private Vector3 DefaultLinearProgressKinematic()
    {
        Acceleration += Vector3.down * Gravity * Time.fixedDeltaTime;
        Velocity += Acceleration * Time.fixedDeltaTime;
        Velocity *= AccelerationFactor;
        return _Transform.position + Velocity * Time.fixedDeltaTime;
    }

    private void ApplyMoveAndDirection(Vector3 p_NestPosition)
    {
        var l_Direction = (p_NestPosition - _Transform.position);
        _Transform.position = p_NestPosition;
        if(l_Direction != Vector3.zero) _Transform.forward = l_Direction;
    }

    private void ApplyTimeUnitForward()
    {
        ElapsedTime += Time.fixedDeltaTime;
    }

    private int CheckCollisionWithCast(RaycastHit[] p_NonAllocStorage, Vector3 p_HitBox, Vector3 p_PeekedNextPosition, int p_LayerMask, Vector3 p_Center)
    {
        var l_Direction = (p_PeekedNextPosition - _Transform.position).normalized;
        var l_Distance = (p_PeekedNextPosition - _Transform.position).magnitude;
        int l_Result = 0;
        switch (_projectileType)
        {
            case ProjectileType.Point: //case point
                l_Result = Physics.RaycastNonAlloc(p_Center, l_Direction, p_NonAllocStorage, l_Distance, p_LayerMask);
                break;
            case ProjectileType.Box: //case box
                l_Result = Physics.BoxCastNonAlloc(p_Center, p_HitBox, l_Direction, p_NonAllocStorage,  _Transform.rotation, l_Distance, p_LayerMask);
                break;
            case ProjectileType.Sphere: //case sphere
                l_Result = Physics.SphereCastNonAlloc(p_Center, p_HitBox.magnitude, l_Direction, p_NonAllocStorage, l_Distance, p_LayerMask);
                break;
        }
        return l_Result;
    }
    
    private int CheckCollisionWithOverlap(Collider[] p_NonAllocStorage, Vector3 p_HitBox, int p_LayerMask, Vector3 p_Center)
    {
        int l_Result;
        switch (_projectileType)
        {
            case ProjectileType.Box: //case box
                l_Result = Physics.OverlapBoxNonAlloc(p_Center , p_HitBox, p_NonAllocStorage,  _Transform.rotation, p_LayerMask);
                break;
            default: //case sphere
                l_Result = Physics.OverlapSphereNonAlloc(p_Center, p_HitBox.magnitude, p_NonAllocStorage, p_LayerMask);
                break;
        }

        return l_Result;
    }

    private void PeekNextPositon()
    {
        if (_projectileMoveActionSequenceList.Count != 0 || _currentMoveAction.isValid)
        {
            // Projectile worked with Custom Sequenced Animation
            if (!_currentMoveAction.isValid)
            {
                _currentMoveAction = _projectileMoveActionSequenceList.Dequeue();
                ElapsedTime = _currentMoveActionCumulativeTime;
                _currentMoveActionCumulativeTime += _currentMoveAction.AnimationDuration;
            }
            else
            {
                if(_currentMoveAction.InitAction!=null) {
                    _currentMoveAction.InitAction(new CustomEventArgs.CommomActionArgs()
                        .SetMorphable(this),_currentMoveAction);
                    _currentMoveAction.InitAction = null;
                }
                if (ElapsedTime <= _currentMoveActionCumulativeTime)
                {
                    if(_currentMoveAction.MoveAction!=null) {
                        PeekedNestPosition = _currentMoveAction.MoveAction(new CustomEventArgs.CommomActionArgs()
                        .SetMorphable(this),_currentMoveAction,_currentMoveAction.LerpFunction);
                        
                    }
                }
            }
        }
        else
        {
            // Projectile worked following Law of Acceleration
            PeekedNestPosition = DefaultLinearProgressKinematic();
        }
    }

    private bool CheckCollisionAboutObstaclesAndTerrain(Action<CustomEventArgs.CommomActionArgs> p_ActionWhenOccurCollision = null)
    {
        if (!_isIgnoreObstacle)
        {
            _overLapCollideNumber = CheckCollisionWithCast(CastHitGroup, _halfExtentsForTerrain, PeekedNestPosition, ObstacleTerrainColliderLayerMask,
                _Transform.position);
            _castHitNumber = CheckCollisionWithOverlap(ColliderGroup, _halfExtentsForTerrain, ObstacleTerrainColliderLayerMask,
                _Transform.position);

            // CASE#1: has been collided on any obstacle.
            if (_castHitNumber > 0 || _overLapCollideNumber > 0)
            {
                // Defined Action: Collided Obstacle.
                if (p_ActionWhenOccurCollision != null)
                {
                    p_ActionWhenOccurCollision(new CustomEventArgs.CommomActionArgs()
                        .SetMorphable(this)
                    );
                }
                // Default Action: Collided Obstacle.
                else
                {
                    Remove();
                }
                return true;
            }
        }
        return false;
    }

    private bool CheckCollisionAboutUnit(Action<CustomEventArgs.CommomActionArgs> p_ActionWhenOccurCollision = null)
    {
      if (!_isIgnoreUnit)
        {
            CollidedUnitGroup.Clear();
            
            _overLapCollideNumber = CheckCollisionWithCast(CastHitGroup, _halfExtents, PeekedNestPosition, UnitColliderLayerMask,
                _Transform.position);
            _castHitNumber = CheckCollisionWithOverlap(ColliderGroup, _halfExtents, UnitColliderLayerMask,_Transform.position);
            var l_loopCount = Mathf.Min(Mathf.Max(_overLapCollideNumber, _castHitNumber), _maxColliderNumber);
            for (var index = 0; index < l_loopCount; index++)
            {
                #region <CollisionCheckByHitCast>
                
                if(index >= _overLapCollideNumber) goto SECTION_HIT_CHECK_OVER;
                var hittedUnit = CastHitGroup[index].collider.GetComponent<Unit>();
                if (Filter.IsPositive(hittedUnit, Caster)) goto SECTION_HIT_CHECK_OVER;
                for (var ExIndex = 0; ExIndex < ExCollidedUnitGroup.Count; ExIndex++)
                    if (Filter.IsPositive(hittedUnit, ExCollidedUnitGroup[ExIndex])) goto SECTION_HIT_CHECK_OVER;
                ExCollidedUnitGroup.Add(hittedUnit);
                CollidedUnitGroup.Add(hittedUnit);
                SECTION_HIT_CHECK_OVER:;

                #endregion

                #region <CollisionCheckbyOverlap>
                
                if(index >= _castHitNumber) goto SECTION_COLLIDE_CHECK_OVER;
                var collidedUnit = ColliderGroup[index].GetComponent<Unit>();
                if (Filter.IsPositive(collidedUnit, Caster)) goto SECTION_COLLIDE_CHECK_OVER;
                for (var ExIndex = 0; ExIndex < ExCollidedUnitGroup.Count; ExIndex++)
                {
                    if (Filter.IsPositive(collidedUnit, ExCollidedUnitGroup[ExIndex])) goto SECTION_COLLIDE_CHECK_OVER;
                }
                ExCollidedUnitGroup.Add(collidedUnit);
                CollidedUnitGroup.Add(collidedUnit);
                SECTION_COLLIDE_CHECK_OVER:;  

                #endregion
            }
   
            // CASE#2: has been collided on any negative unit.
            if (CollidedUnitGroup.Count > 0)
            {
                // Defined Action: Collided Unit.
                if (p_ActionWhenOccurCollision != null)
                {
                    p_ActionWhenOccurCollision(new CustomEventArgs.CommomActionArgs()
                        .SetMorphable(this)
                    );
                }
                // Default Action: Collided Unit.
                else
                {
                    Remove();
                }
                
                if (_maxNumberOfHitTime > 1)
                {
                    _maxNumberOfHitTime--;
                    ExCollidedUnitGroup.Clear();
                }
                return true;
            }
        }
        return false;
    }

    #endregion </Methods>

    #region <Structs>

    public struct ProjectileAnimationEventArgs
    {
        #region <Fields>

        /* setter property member */
        public FormattedMonoBehaviour ProjectileToMove;
        public float AnimationDuration { get; private set;}
        public Action<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs> InitAction;
        public Func<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs,Func<float, float, float, float, float>,Vector3> MoveAction;
        public Action<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs> ExpireAction;
        public Func<float, float, float, float, float> LerpFunction;
        public bool isValid;
        
        /* Each action referencing this custom member */
        public int NumberToReadQueue { get; private set;}
        public bool VariableUseFlag;
        
        #endregion

        #region <Methods/Setter>
     
        public ProjectileAnimationEventArgs SetLerpFunction(Func<float, float, float, float, float> pLerpFunction)
        {
            LerpFunction = pLerpFunction;
            return this;
        }   
        
        public ProjectileAnimationEventArgs SetNumberToReadQueue(int pNumberToReadQueue)
        {
            NumberToReadQueue = pNumberToReadQueue;
            return this;
        }   
        
        public ProjectileAnimationEventArgs SetVariableFlag(bool p_VariableUseFlag)
        {
            VariableUseFlag = p_VariableUseFlag;
            return this;
        }   
        
        public ProjectileAnimationEventArgs SetProjectile(Projectile p_Projectile)
        {
            ProjectileToMove = p_Projectile;
            return this;
        }
        
        public ProjectileAnimationEventArgs SetDuration(float p_AnimationDuration)
        {
            AnimationDuration = p_AnimationDuration;
            return this;
        }
        
        public ProjectileAnimationEventArgs SetInitAction(Action<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction)
        {
            InitAction = p_InitAction;
            return this;
        }
        
        public ProjectileAnimationEventArgs SetMoveAction(Func<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs,Func<float, float, float, float, float>,Vector3> p_MoveAction)
        {
            MoveAction = p_MoveAction;
            return this;
        }
        
        public ProjectileAnimationEventArgs SetExpiredAction(Action<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction)
        {
            ExpireAction = p_ExpiredAction;
            return this;
        }

        public ProjectileAnimationEventArgs SetActive()
        {
            isValid = true;
            return this;
        }

        #endregion
    }
    
    #endregion

}