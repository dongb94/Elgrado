using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(HumanBoneAnimator))]
[RequireComponent(typeof(CharacterController))]
public abstract class Unit : FormattedMonoBehaviour, IBoneAnimatorCallback
{
    
    #region <Consts>

    protected const float UpdateRunningTime = 0.5f;
    private const float ForceImpactThreshold = 0.04f;
    private const float GravityForce = .49f;
    private const float EnergyConsume = 5.0f;
    private const int DefaultTension = 5;

    #endregion </Consts>
    
    #region <Fields>

    /* Enum */
    public TextureType WeaponType;
    public TextureType ArmorType;
    public UnitState State { get; protected set; }

    /* status : hp, mass */
    [Range(1, 50000)] public int MaximumHealthPoint;    
    public int CurrentHealthPoint { get; protected set; }
    [Range(.01f, float.MaxValue)] [SerializeField] protected float Mass; 
    
    /* Move Speed */
    public float MovementSpeed;
    public float RotateSpeed;
    public float Speed { get; private set; }
    public Vector3 ForceVector { get; protected set; }
    protected float RunningTime;
    protected float RunningPower;
    protected float AngleToDestination;
    private float _aeroAccumulator;
    
    /* Animation Speed */
    private float _castSpeed;
    public float CastSpeed
    {
        get { return _castSpeed;}
        set
        {            
            _castSpeed = value;
            UnitBoneAnimator.UnityAnimator.SetFloat("CastSpeed", value);
        }
    }
    
    /* Time unit relate*/
    public int Tension { get; protected set; }
    public int DecayTime;    
    protected int DecayTimeLeft;
    public ParticleSystem[] ParticleSetWhenDecay;

    /* Unit Index circle */
    private K514UnitFocusCircle _mFocusCircle;
    
    /* NavMesh Agent */
    protected NavMeshAgent _navMeshAgent;
        
    /* Dynamic Material Property */
    [NonSerialized]public K514MaterialApplier MaterialApplier;

    /* Animation Sequence Property */
    [NonSerialized] public bool AnimationSequenceWaitingKey;
    protected List<KeyValuePair<string, int>> innerAnimationSequenceSet;
    
    /* CC */
    public List<CrowdControl> CrowdControlGroup { get; protected set; }
    
    /* Deprecated : Trail Generator */
    protected K514TrailGenerator[] UnitSpellTrailEffectGroup;    

    /* etcetera */
    [SerializeField] public Transform[] AttachPoint;
    protected CharacterController Controller;
    public HumanBoneAnimator UnitBoneAnimator { get; private set; }
    
    #endregion </Fields>
    
    #region <Enums>
    
    public enum UnitState
    {
        Lives,
        Dead,
    }
    
    public enum AttachPointType
    {
        HeadTop_End,
        LeftForeArm,
        RightForeArm,
        LeftHandIndex1,
        RightHandIndex1,
        Spine1,
        Motion,
        Count,
        Random
    }

    // @K514 : need to environment sfx e.g. drawaing sword, trigging bow
//    public enum ArmsType
//    {
//        Blade,
//        Bow,
//        Count
//    }
    
    // @K514 : attacker defender interact relevant 
    public enum TextureType
    {
        None,        // flesh                      :        naked arms
        Light,       // leather,fabric armor       :        piercing arms   
        Medium,      // bone,wooden armor          :        slashing arms
        Heavy,       // iron, giant armor          :        bashing, heavy arms
        Magic,       // magic armor                :        magic arms
        Count
    }

    #endregion </Enums>

    #region <Unity/Callbacks>

    /// <summary>
    /// Caching the components.
    /// </summary>
    protected virtual void Awake()
    {  
        UnitBoneAnimator = GetComponent<HumanBoneAnimator>();
        Controller = GetComponent<CharacterController>();
        UnitSpellTrailEffectGroup = GetComponents<K514TrailGenerator>();
        MaterialApplier = GetComponent<K514MaterialApplier>();
        CrowdControlGroup = new List<CrowdControl>();
        _navMeshAgent  = GetComponent<NavMeshAgent>();

        ForceVector = Vector3.zero;
        CurrentHealthPoint = MaximumHealthPoint;
        Mass = 1f / Mass;
        CastSpeed = 1.0f;
    }

    protected virtual void OnDisable()
    {
        if (State != UnitState.Dead) MaterialApplier.RevertTrigger();
    }

    protected virtual void FixedUpdate()
    {
        if (State != UnitState.Dead)
            UpdateMove();
        UpdateForce();
    }
    
    #endregion </Unity/Callbacks>

    #region <Callbacks>

    public virtual void OnHeartBeat()
    {
        Tension = Math.Max(0, Tension - 1);

        // <Carey>: Exception Handling for a node removal issue with this top-down iterator.
        for (var crowdControlIndex = CrowdControlGroup.Count - 1; crowdControlIndex >= 0; --crowdControlIndex)
            CrowdControlGroup[crowdControlIndex].OnHeartBeat();

        if (State == UnitState.Dead)
        {
            DecayTimeLeft = Math.Max(0, DecayTimeLeft - 1);
            // <K514>: Revert Particle system to Default Setting
            RevertLoopOptionOfAttachedParticleSet(false);
        }
    }
    
    protected virtual void OnDeath()
    {        
        // <K514>: disable collider not to block other unit when died
        Controller.enabled = false;
        if(_navMeshAgent != null) _navMeshAgent.enabled = false;
        
        // <Carey>: Exception Handling for a node removal issue with this top-down iterator.
        for (var crowdControlIndex = CrowdControlGroup.Count - 1; crowdControlIndex >= 0; --crowdControlIndex)
            CrowdControlGroup[crowdControlIndex].OnTerminate();
        CrowdControlGroup.Clear();
        MaterialApplier.RevertTrigger();
        MaterialApplier.SetDissolveMaterial(K514MaterialStorage.MAT_STATE.kBurned,3f);
        
        State = UnitState.Dead;
        UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Dead);    
        
        // <K514>: Dissable Particle system to Default Setting
        RevertLoopOptionOfAttachedParticleSet(true);
    }    

    public void OnHitMotionExit()
    {
        MaterialApplier.RevertTrigger();
    }

    public abstract void OnIdleRelax();
    
    /// <summary>
    /// Callback from <see cref="Animator"/> and <seealso cref="Animation"/>.
    /// </summary>
    public abstract void OnCastAnimationStandby();
    
    /// <summary>
    /// Callback from <see cref="Animator"/> and <seealso cref="Animation"/>.
    /// </summary>
    public abstract void OnCastAnimationCue();
    
    /// <summary>
    /// Callback from <see cref="Animator"/> and <seealso cref="Animation"/>.
    /// </summary>
    public abstract void OnCastAnimationExit();
        
    public abstract void OnCastAnimationEnd();
        
    public abstract void OnCastAnimationCleanUp();

    public override void OnCreated()
    {
        CurrentHealthPoint = MaximumHealthPoint;
        State = UnitState.Lives;
        DecayTimeLeft = DecayTime;
        Tension = 0;
        CrowdControlGroup.Clear();
        MaterialApplier.RevertTrigger();
        Controller.enabled = true;
    }

    public override void OnRemoved()
    {
    }

    #endregion </Callbacks>  

    #region <Properties>
    
    public Vector3 GetUnitPosition
    {
        get { return _Transform.position; }
    }

    public Vector3 GetUnitOrthographicPosition
    {
        get
        {
            var unitPosition = GetUnitPosition;
            unitPosition.y = 0;
            
            return unitPosition;
        }
    }
    
    public List<KeyValuePair<string, int>> AnimationSequenceSet
    {
        get
        {
            if(innerAnimationSequenceSet == null) innerAnimationSequenceSet = new List<KeyValuePair<string, int>>();
            return innerAnimationSequenceSet;
        }
    }
    
    #endregion </Properties>
    
    #region <Methods>

    public virtual void Move(Vector3 forceVector)
    {
        SetAngleToDestination(forceVector);
        RunningTime = UpdateRunningTime;
    }
    
    public void AddForce(Vector3 force)
    {
        ForceVector += force * Mass;
    }
    
    public virtual void Hurt(Unit caster, int damage, TextureType type, Vector3 forceDirection, 
        Action<Unit, Unit, Vector3> action = null)
    {
        if (State != UnitState.Lives) return;      

        // TODO<Carey>: if (Verifying Of The Condition Of Hurt)
        if (true)
        {
            CurrentHealthPoint -= damage;
            MaterialApplier.SetTrigger(K514MaterialStorage.MAT_STATE.kHitted);
            
            if (action == null)
            {    
                /* case */
                /*
                switch (type)
                {
                    case TextureType.None:
                        switch (type)
                        {
                            case TextureType.None:
                                break;
                            case TextureType.Light:
                                break;
                            case TextureType.Medium:
                                break;
                            case TextureType.Heavy:
                                break;
                            case TextureType.Universal:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("type", type, null);
                        }
                        break;
                    case TextureType.Light:
                        break;
                    case TextureType.Medium:
                        break;
                    case TextureType.Heavy:
                        break;
                    case TextureType.Universal:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("type", type, null);
                }
                */
            }
            else
            {
                action(caster, this, forceDirection);
            }
            
            if (CurrentHealthPoint <= 0)
            {
                OnDeath();
            }
        }                                      
        
//        forceDirection.Normalize();
//        AddForce(forceDirection * damage);
        
//
//        var lFxTypeIndex = caster.FxAttackType.Length > (int) TriggerId ?  
//            caster.FxAttackType[TriggerId] : K514VfxManager.ParticleType.PSpark;
//        var lVfx = K514VfxManager.GetInstance.CastVfx(lFxTypeIndex,CalculateHittedOffset(pStrikeTo));
//        if (pVfxChainProperty != null) pVfxChainProperty(lVfx);
//        lVfx.SetTrigger();
    }

    public virtual void AddCrowdControl(CrowdControl crowdControl)
    {
        crowdControl.OnBirth();
    }
    
    public void GenerateCircle(FormattedMonoBehaviour pCirclePrefab)
    {
        _mFocusCircle = ObjectManager.GetInstance.GetObject<K514UnitFocusCircle>(ObjectManager.PoolTag.UnitCircle, pCirclePrefab).SetTarget(this);
    }    
    
    public void RemoveCircle()
    {
        if(_mFocusCircle!=null) ObjectManager.RemoveObject(_mFocusCircle);
    }
    
    protected void SetAngleToDestination(Vector3 forceVector)
    {
        AngleToDestination = Mathf.Atan2(forceVector.x, forceVector.z) * Mathf.Rad2Deg;
    }             
    
    public Vector3 GetNormDirectionToMove(Unit towardUnit)
    {
        return GetNormDirectionToMove(towardUnit._Transform.position);        
    }
    
    public Vector3 GetNormDirectionToMove(Vector3 towardPoint)
    {
        var directionVector = towardPoint - _Transform.position;        
        return directionVector.normalized;
    }

    public Vector3 GetNormDirectionCandidateRandomAttachPoint(Vector3 p_Center, Unit p_Opponent)
    {
        return (p_Opponent.AttachPoint[Random.Range(0,(int)AttachPointType.Count)].position - p_Center).normalized;
    }
    
    public Vector3 GetNormDirectionCandidateRandomAttachPoint(Transform p_Center, Unit p_Opponent)
    {
        return GetNormDirectionCandidateRandomAttachPoint(p_Center.position,p_Opponent);
    }
    
    public Vector3 GetNormDirectionCandidateRandomAttachPoint(AttachPointType p_Center, Unit p_Opponent)
    {
        return GetNormDirectionCandidateRandomAttachPoint(AttachPoint[(int)p_Center].position,p_Opponent);
    }
    
    public Vector3 GetNormDirectionCandidateRandomAttachPoint(Unit p_Opponent)
    {
        return GetNormDirectionCandidateRandomAttachPoint(_Transform,p_Opponent);
    }
    
    public Vector3 GetRandomAttachPosition()
    {
        var l_Result = AttachPoint[Random.Range(0, (int) AttachPointType.Count)];
        return l_Result == null ? _Transform.position + Vector3.up * 1f : l_Result.position;
    }
    
    public Transform GetRandomAttachTransform()
    {
        var l_Result = AttachPoint[Random.Range(0, (int) AttachPointType.Count)];
        return l_Result == null ? _Transform  : l_Result;
    }
    
    public void NavMeshMoveApply(Vector3 p_TargetPosition)
    {
        if (_navMeshAgent != null && !_navMeshAgent.enabled) return;
        _navMeshAgent.SetDestination(p_TargetPosition);
        Move(p_TargetPosition);
    }
    
    public void NavMeshStop()
    {
        _navMeshAgent.ResetPath();
    }

    public void UpdateTension()
    {
        Tension = DefaultTension;
    }

    private void UpdateMove()
    {
        Speed = Time.fixedDeltaTime * RunningTime * MovementSpeed;
        
        if (!Controller.isGrounded)
        {
            // <TODO:Carey> Reflect the gravity acceleration.
            _aeroAccumulator = Mathf.Min(5.0f, _aeroAccumulator + Time.fixedDeltaTime);
            var gravityDirection = Vector3.down * GravityForce * _aeroAccumulator;
            Controller.Move(gravityDirection);
        }
        else
        {
            _aeroAccumulator = .0f;
        }

        if (!(RunningTime > Mathf.Epsilon) 
            || State == UnitState.Dead) return;
//            || (UnitBoneAnimator.CurrentState )
//             == BoneAnimator.AnimationState.Move && UnitBoneAnimator.UnityAnimator.IsInTransition(0))) 
        
        RunningTime -= Time.fixedDeltaTime;

        if (_navMeshAgent == null)
        {
            // Take care about the rotation of character.
            var angle = Mathf.LerpAngle(_Transform.eulerAngles.y, AngleToDestination,
                Time.fixedDeltaTime * RotateSpeed);
            _Transform.eulerAngles = Vector3.up * angle;

            // Adjust the movement speed to forward.
            var direction = _Transform.TransformDirection(Vector3.forward);
            UnitMove(direction * Speed);
        }
    }

    private void UpdateForce()
    {
        if (ForceVector.sqrMagnitude <= ForceImpactThreshold) return;
        
        UnitMove(ForceVector * Time.fixedDeltaTime);
        ForceVector = Vector3.Lerp(ForceVector, Vector3.zero, EnergyConsume * Time.fixedDeltaTime);
    }

    protected virtual void UnitMove(Vector3 forceVector)
    {
        if (Controller.enabled)
        {
            Controller.Move(forceVector);
        }
        if (_navMeshAgent != null && _navMeshAgent.enabled)
        {
            _navMeshAgent.ResetPath();
            _navMeshAgent.Move(forceVector);
        }
    }

    private void RevertLoopOptionOfAttachedParticleSet(bool p_Flag)
    {
        for (var i = 0; i < ParticleSetWhenDecay.Length; i++)
        {
            ParticleSetWhenDecay[i].loop = p_Flag;
        }
    }

    #endregion </Methods>    

    #region <Coroutines>
    
    protected Action<CommomActionArgs> AnimationSequenceHandler = args =>
    {
        var lContent = ObjectManager.GetInstance.GetObject<K514PooledCoroutine>(
            ObjectManager.PoolTag.Coroutine,
            K514PrefabStorage.GetInstance.GetPrefab(K514PrefabStorage.PrefabType.PooledCoroutine)
        );       
        
        lContent
            ._mParams.SetMorphable(args.MorphObject);
        
        lContent
            // init
            .SetAction(K514PooledCoroutine.ActionType.Init, ano =>
            {
                var unit = (Unit)ano.MorphObject;
                unit.AnimationSequenceWaitingKey = false;
            })
            // cond
            .SetAction(K514PooledCoroutine.ActionType.EndTrigger,ano =>
            {
                var unit = (Unit)ano.MorphObject;
                return unit.AnimationSequenceSet.Count < 1;
            })
            // act
            .SetAction(K514PooledCoroutine.ActionType.Activity, ano =>
            {

                var unit = (Unit)ano.MorphObject;
                var animationToPlay = unit.AnimationSequenceSet.ElementAt(0);
                unit.UnitBoneAnimator.SetCast(animationToPlay.Key, animationToPlay.Value);
            })
            .SetAction(K514PooledCoroutine.ActionType.BusyWaitingTrigger,ano =>
            {
                var unit = (Unit)ano.MorphObject;
                if (unit.AnimationSequenceWaitingKey)
                {
                    unit.AnimationSequenceWaitingKey = false;
                    unit.AnimationSequenceSet.RemoveAt(0);
                    return true;
                }
                return false;
            })
            // interval
            //.SetInterval(0.1f, K514PooledCoroutine.DelayType.Interval)
            // activate
            .SetTrigger();

    };

    #endregion
    
    #region <CustomEditor>

    public string FindAttatchPoint()
    {
        var childrenTransformGroup = GetComponentsInChildren<Transform>();
        var lFlags = K514MathManager.PowByte(2, (uint)AttachPointType.Count) - 1;
        var lAttatchPoint = new Transform[(int) AttachPointType.Count];
                        
        foreach (var i in childrenTransformGroup)
        {
            if (lFlags == 0) break;
            for (var j = 0; j < lAttatchPoint.Length; j++)
            {
                var lShift = 1 << j;
                if ((lFlags & lShift) == lShift && i.name.Contains(((AttachPointType) j).ToString()))
                {
                    lAttatchPoint[j] = i;
                    lFlags ^= lShift;
                }
            }
        }
        AttachPoint = lAttatchPoint;
        return lFlags == 0 ? "Find Complete Successfully" : "Find End Incompletely, Move mouse";
    }

    #endregion </CustomEditor>
    
}