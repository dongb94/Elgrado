using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(HumanBoneAnimator))]
[RequireComponent(typeof(CharacterController))]
public abstract class Unit : PreProcessedMonoBehaviour, IBoneAnimatorCallback
{
    
    #region <Consts>

    public const float FrictionFactor = 5.0f;
    protected const float UpdateRunningTime = 0.5f;
    private const float ForceImpactThreshold = 0.04f;
    private const float GravityForce = .49f;
    private const int DefaultTension = 5;

    #endregion </Consts>
    
    #region <Fields>

    /* Enum */
    public TextureType WeaponType;
    public TextureType ArmorType;
    public UnitState State { get; protected set; }

    /* status : hp, mass */
    [Range(1, 50000)] public int MaximumHealthPoint;    
    [Range(.01f, float.MaxValue)] public float Mass; 
    public int CurrentHealthPoint
    {
        get { return _healthPoint; }
        protected set
        {            
            _healthPoint = value;
            OnHealthPointAdjust();
        }
    }
    public bool Invincible { get; protected set; }
    private int _healthPoint;
    
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

    /* Dynamic Material Property */
    [NonSerialized]public K514MaterialApplier MaterialApplier;

    /* Animation Sequence Property */
    [NonSerialized] public bool AnimationSequenceWaitingKey;
    protected List<KeyValuePair<string, int>> innerAnimationSequenceSet;
    
    /* CC */
    public List<CrowdControl> CrowdControlGroup { get; protected set; }
    
    /* Deprecated : Trail Generator */
    protected K514TrailGenerator[] UnitSpellTrailEffectGroup;    

    /* Spell Hyper Parameter */
    [NonSerialized]public SpellHyperParameter[] hyperParameterSet;

    /* NavMesh Agent */
    protected NavMeshAgent _navMeshAgent;
    
    /* etcetera */
    [SerializeField] public Transform[] AttachPoint;
    public CharacterController Controller { get; protected set; }
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

    
    public enum UnitEventType
    {
        Initialize,
        SetTrigger,
        
        Begin,        
        Standby,
        Cue,Cue2,Cue3,Cue4,
        Exit,
        End,  
        
        CleanUp,
        
        OnHeartBeat,
        OnFixedUpdate,
        OnRelax,
        OnTransitionToOtherCast,
        
        Count
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
    
    public enum HyperParameterOfSpell
    {
        NormalAttack01, NormalAttack02, NormalAttack03, Spell01, Spell02, Spell03, Spell04, Spell05, Spell06, Spell07, Spell08, Spell09, Spell10, End
    }

    #endregion </Enums>

    #region <Unity/Callbacks>

    /// <summary>
    /// Caching the components.
    /// </summary>
    protected virtual void Awake()
    {  
        Controller = GetComponent<CharacterController>();
        UnitBoneAnimator = GetComponent<HumanBoneAnimator>();
        UnitSpellTrailEffectGroup = GetComponents<K514TrailGenerator>();
        MaterialApplier = GetComponent<K514MaterialApplier>();
        CrowdControlGroup = new List<CrowdControl>();
        _navMeshAgent  = GetComponent<NavMeshAgent>();

        ForceVector = Vector3.zero;
        CurrentHealthPoint = MaximumHealthPoint;
        Mass = 1f / Mass;
        CastSpeed = 1.0f;
        hyperParameterSet = new SpellHyperParameter[(int) HyperParameterOfSpell.End];
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
        
        // <Carey>: Exception Handling for a node removal issue with this top-down iterator.
        for (var crowdControlIndex = CrowdControlGroup.Count - 1; crowdControlIndex >= 0; --crowdControlIndex)
            CrowdControlGroup[crowdControlIndex].OnTerminate();
        CrowdControlGroup.Clear();
        MaterialApplier.RevertTrigger();
        MaterialApplier.SetDissolveMaterial(K514MaterialStorage.MAT_STATE.kBurned,3f);
        
        State = UnitState.Dead;
        UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Dead);    
        
        // <K514>: Dissable Particle system to Default Setting
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
    public abstract void OnCastAnimationCue2();
    public abstract void OnCastAnimationCue3();
    public abstract void OnCastAnimationCue4();

    /// <summary>
    /// Callback from <see cref="Animator"/> and <seealso cref="Animation"/>.
    /// </summary>
    public abstract void OnCastAnimationExit();
    
    /// <summary>
    /// Callback from <see cref="BoneAnimator"/>.
    /// </summary>
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
        Invincible = false;
        RevertLoopOptionOfAttachedParticleSet(true);
    }

    public override void OnRemoved()
    {
    }

    public virtual void OnHealthPointAdjust()
    {
        
    }

    #endregion </Callbacks>  

    #region <Properties>   
    
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
    
    protected virtual void UnitMove(Vector3 forceVector)
    {
        if (Controller != null && Controller.enabled)
        {
            if (FixedFocusManager.GetInstance.IsValid &&
                FixedFocusManager.GetInstance.IsOverFrontier(GetPosition + 5.14f * forceVector))
            {
                return;
            }
            Controller.Move(forceVector);
        }
    }
    
    public void AddForce(Vector3 force, bool hasIgnoreFriction = false, bool hasIgnoreMass = false)
    {
        ForceVector += force / (hasIgnoreMass ? 1f : Mass) * (hasIgnoreFriction ? FrictionFactor : 1f);
    }
    
    public virtual void Hurt(Unit caster, int damage, TextureType type, Vector3 forceDirection, 
        Action<Unit, Unit, Vector3> action = null)
    {
        if (UnitFilter.Check(this, UnitFilter.Condition.IsDead) | UnitFilter.Check(this, UnitFilter.Condition.IsInvincible)) return;      

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
    
    public void GenerateCircle(PreProcessedMonoBehaviour pCirclePrefab)
    {
        _mFocusCircle = ObjectManager.GetInstance.GetObject<K514UnitFocusCircle>(ObjectManager.PoolTag.UnitCircle, pCirclePrefab).SetTarget(this);
    }    
    
    public void RemoveCircle()
    {
        if(_mFocusCircle!=null) ObjectManager.RemoveObject(_mFocusCircle);
    }
    
    public virtual void ResetFromCast()
    {                       
        UnitBoneAnimator.SetTrigger(RunningTime > Mathf.Epsilon
            ? BoneAnimator.AnimationState.Move
            : BoneAnimator.AnimationState.Idle);
    }
    
    public float SetAngleToDestination(Vector3 forceVector)

    {
        AngleToDestination = Mathf.Atan2(forceVector.x, forceVector.z) * Mathf.Rad2Deg;

        return AngleToDestination;
    }             
    
    public Vector3 GetNormDirectionToMove(Unit towardUnit)
    {
        return GetNormDirectionToMove(towardUnit.Transform.position);        
    }
    
    public Vector3 GetNormDirectionToMove(Vector3 towardPoint)
    {
        var directionVector = towardPoint - Transform.position;        
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
        return GetNormDirectionCandidateRandomAttachPoint(Transform,p_Opponent);
    }
    
    public Vector3 GetRandomAttachPosition()
    {
        var l_Result = AttachPoint[Random.Range(0, (int) AttachPointType.Count)];
        return l_Result == null ? Transform.position + Vector3.up * 1f : l_Result.position;
    }
    
    public Transform GetRandomAttachTransform()
    {
        var l_Result = AttachPoint[Random.Range(0, (int) AttachPointType.Count)];
        return l_Result == null ? Transform  : l_Result;
    }

    public void UpdateTension()
    {
        Tension = DefaultTension;
    }

    public void ChaseOrRush(Unit target, float chaseRate, float rushRate)
    {
        if (target == null)
            AddForce(Transform.TransformDirection(Vector3.forward) * rushRate, hasIgnoreFriction: true);
        else
        {
            Transform.eulerAngles = Vector3.up * SetAngleToDestination(target.GetPosition - GetPosition);
            AddForce(GetNormDirectionToMove(target) * MathVector.SqrDistance(GetPosition, target.GetPosition) *
                     chaseRate, hasIgnoreFriction: true, hasIgnoreMass: true);
        }
    }
    
    public virtual void Move(Vector3 forceVector)
    {
        SetAngleToDestination(forceVector);
        RunningTime = UpdateRunningTime;
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

        // Take care about the rotation of character.
        var angle = Mathf.LerpAngle(Transform.eulerAngles.y, AngleToDestination,
            Time.fixedDeltaTime * RotateSpeed);
        Transform.eulerAngles = Vector3.up * angle;

        // Adjust the movement speed to forward.
        var direction = Transform.TransformDirection(Vector3.forward);
        UnitMove(direction * Speed);
    }

    private void UpdateForce()
    {
        if (ForceVector.sqrMagnitude <= ForceImpactThreshold) return;
        
        UnitMove(ForceVector * Time.fixedDeltaTime);
        ForceVector = Vector3.Lerp(ForceVector, Vector3.zero, FrictionFactor * Time.fixedDeltaTime);
    }

    private void RevertLoopOptionOfAttachedParticleSet(bool p_Flag)
    {
        for (var i = 0; i < ParticleSetWhenDecay.Length; i++)
        {
            ParticleSetWhenDecay[i].loop = p_Flag;
        }
    }

    public Unit SetInvincible(bool p_Flag)
    {
        Invincible = p_Flag;
        return this;
    }

    #endregion </Methods>    

    #region <Structs>

    public struct SpellHyperParameter
    {
        public int CoolDown;
        public int Damage;
        public float Range;
        public int Motion;
        public Vector3 ColliderHalfExtends;

        public Vector3 GetHalfExtends()
        {
            return ColliderHalfExtends == Vector3.zero ? Vector3.one * 0.003f : ColliderHalfExtends;
        }
    }

    #endregion
    
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