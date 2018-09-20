using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Effects;

using Random = UnityEngine.Random;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(HumanBoneAnimator))]
[RequireComponent(typeof(CharacterController))]
public abstract class Unit : FormattedMonoBehaviour, IBoneAnimatorCallback
{
    
    #region <Consts>

    protected const float UpdateRunningTime = 0.5f;
    private const float ForceImpactThreshold = 0.04f;
    private const float GravityForce = 9.8f;
    private const float EnergyConsume = 5.0f;
    private const int DefaultTension = 5;

    #endregion </Consts>
    
    #region <Fields>

    // Serialized Field
    public int MaxHp;
    public int Power;
    public int DecayTime;
    public float AttackRange;
    public float MovementSpeed;
    public float RotateSpeed;
    public ParticleSystem[] ParticleSetWhenDecay;
    public WeaponType Weapontype;
    public TextureType Armortype;
    [SerializeField] public Transform[] AttachPoint;
    [SerializeField] protected UnitSpellCollider[] UnitSpellColliderGroup;    
    [Range(.01f, float.MaxValue)] [SerializeField] protected float Mass;

    // NonSerialized Field
    protected CharacterController Controller;
    protected float RunningTime;
    protected float RunningPower;
    protected float AngleToDestination;
    protected int DecayTimeLeft;
    protected int TriggerId;
    private float _castSpeed;
    
    // Deprecated : Trail Generator
    protected K514TrailGenerator[] UnitSpellTrailEffectGroup;    

    // Dynamic Material Property
    [NonSerialized]public K514MaterialApplier MaterialApplier;
    
    // Unit index Effect Object
    private K514UnitFocusCircle _mFocusCircle;
    
    /* NavMesh Agent */
    private bool IsNavAgent;
    
    // Deprecated : Unit Control Trigger
    [NonSerialized] public int NotMoveTrigger, NotRotateTrigger, NotReleaseTension;

    // Animation Sequence Property    
    [NonSerialized] public bool AnimationSequenceWaitingKey;
    protected List<KeyValuePair<string, int>> innerAnimationSequenceSet;

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
    }

    // @K514 : need to environment sfx e.g. drawaing sword, trigging bow
    public enum WeaponType
    {
        Blade,
        Bow,
        Count
    }
    
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
        MaterialApplier = gameObject.AddComponent<K514MaterialApplier>();
        CrowdControlGroup = new List<CrowdControl>();
        ForceVector = Vector3.zero;
        TriggerId = 0;
        Mass = 1f / Mass;
        CastSpeed = 1.0f;
    }

    private void OnEnable()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            IsNavAgent = true;
        else
            IsNavAgent = false;
        ResetSpellColliderActive();
    }

    protected virtual void OnDisable()
    {
        if (State != UnitState.Dead) MaterialApplier.RevertTrigger();
        ResetSpellColliderActive();
    }

    protected virtual void FixedUpdate()
    {
        if (State != UnitState.Dead)
            UpdateMove();
        UpdateForce();
    }
    
    #endregion </Unity/Callbacks>

    #region <Callbacks>

    /// <summary>
    /// Count for the left time for the death.
    /// </summary>
    /// <see cref="RhythmManager"/>
    public virtual void OnHeartBeat()
    {       
        if(NotReleaseTension == 0) Tension = Math.Max(0, Tension - 1);

        // <Carey>: Exception Handling for a node removal issue with this top-down iterator.
        for (var crowdControlIndex = CrowdControlGroup.Count - 1; crowdControlIndex >= 0; --crowdControlIndex)
            CrowdControlGroup[crowdControlIndex].OnHeartBeat();
        
        if (State == UnitState.Dead)
            DecayTimeLeft = Math.Max(0, DecayTimeLeft - 1);

        if (DecayTimeLeft <= 0)
        {
            for (var i = 0; i < ParticleSetWhenDecay.Length; i++)
            {
                ParticleSetWhenDecay[i].loop = true;
            }
        }
    }
    
    protected virtual void OnDeath()
    {
        ResetSpellColliderActive();
        
        // <Carey>: Exception Handling for a node removal issue with this top-down iterator.
        for (var crowdControlIndex = CrowdControlGroup.Count - 1; crowdControlIndex >= 0; --crowdControlIndex)
            CrowdControlGroup[crowdControlIndex].OnTerminate();
        CrowdControlGroup.Clear();
        MaterialApplier.RevertTrigger();
        MaterialApplier.SetDissolveMaterial(K514MaterialStorage.MAT_STATE.kBurned,3f);

        for (var i = 0; i < ParticleSetWhenDecay.Length; i++)
        {
            ParticleSetWhenDecay[i].loop = false;
        }

        State = UnitState.Dead;
        UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Dead);
    }
    
    public virtual void OnExitedEffectPeriod(int triggerId)
    {
        ResetSpellColliderActive();  
    }

    public void OnExitedHitMotion()
    {
        MaterialApplier.RevertTrigger();
    }

    public abstract void OnObjectTriggerEnterUnit(Unit collidedUnit);
    public abstract void OnObjectTriggerExitUnit(Unit collidedUnit);
    public abstract void OnExitedCastAnimation();
    public abstract void OnIdleRelax();
    public abstract void OnEnteredEffectPeriod(int triggerId);

    public override void OnCreated()
    {
        Hp = MaxHp;
        State = UnitState.Lives;
        DecayTimeLeft = DecayTime;
        Tension = 0;
        CrowdControlGroup.Clear();
        MaterialApplier.RevertTrigger();
    }

    public override void OnRemoved()
    {
    }

    #endregion </Callbacks>  

    #region <Properties>
        
    public Vector3 ForceVector { get; protected set; }      
    public UnitState State { get; protected set; }
    public List<CrowdControl> CrowdControlGroup { get; protected set; }
    public float Speed { get; private set; }
    public int Tension { get; protected set; }
    public int Hp { get; protected set; }
    public HumanBoneAnimator UnitBoneAnimator { get; private set; }
    public UnitSpellCollider[] GetUnitSpellColliderGroup { get { return UnitSpellColliderGroup; } }
    
    public float CastSpeed
    {
        get { return _castSpeed;}
        set
        {            
            _castSpeed = value;
            UnitBoneAnimator.UnityAnimator.SetFloat("CastSpeed", value);
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
    
    #endregion </Properties>
    
    #region <Methods>

    public virtual void Move(Vector3 forceVector)
    {
        SetAngleToDestination(forceVector);
                
        RunningTime = UpdateRunningTime;
        RunningPower = forceVector.sqrMagnitude;
    }
    
    public void AddForce(Vector3 force)
    {
        ForceVector += force * Mass;
    }
    
    public virtual void Hurt(Unit caster, int damage, TextureType type, Vector3 forceDirection, 
        Action<Unit, Unit, Vector3> action = null, bool isCancelCast = true)
    {
        if (State != UnitState.Lives) return;
        
        if (isCancelCast)
            ResetSpellColliderActive();

        // TODO<Carey>: if (Verifying Of The Condition Of Hurt)
        if (true)
        {
            Hp -= damage;
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
            
            if (Hp <= 0)
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

    public virtual void ResetSpellColliderActive()
    {
        foreach (var unitSpellCollider in UnitSpellColliderGroup)
        {
            unitSpellCollider.enabled = false;
        }
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

    public void UpdateTension()
    {
        Tension = DefaultTension;
    }

    private void UpdateMove()
    {
        Speed = Time.fixedDeltaTime * RunningPower * RunningTime * MovementSpeed;
        
        if (!Controller.isGrounded)
        {
            // <TODO:Carey> Reflect the gravity acceleration.
            var gravityDirection = Vector3.down * GravityForce * Time.fixedDeltaTime;
            Controller.Move(gravityDirection);
        }

        if (!(RunningTime > Mathf.Epsilon) 
            || State == UnitState.Dead) return;
//            || (UnitBoneAnimator.CurrentState )
//             == BoneAnimator.AnimationState.Move && UnitBoneAnimator.UnityAnimator.IsInTransition(0))) 
        
        RunningTime -= Time.fixedDeltaTime;


        if (IsNavAgent) return;

        // Take care about the rotation of character.
        if (NotRotateTrigger<1)
        {
            var angle = Mathf.LerpAngle(_Transform.eulerAngles.y, AngleToDestination,
                Time.fixedDeltaTime * RotateSpeed);
            _Transform.eulerAngles = Vector3.up * angle;
        }

        // Adjust the movement speed to forward.
        var direction = _Transform.TransformDirection(Vector3.forward);
        UnitMove(direction * Speed);
    }

    private void UpdateForce()
    {
        if (ForceVector.sqrMagnitude <= ForceImpactThreshold) return;
        
        UnitMove(ForceVector * Time.fixedDeltaTime);
        ForceVector = Vector3.Lerp(ForceVector, Vector3.zero, EnergyConsume * Time.fixedDeltaTime);
    }

    protected virtual void UnitMove(Vector3 forceVector)
    {
        Controller.Move(forceVector);
    }        

    #endregion </Methods>    

    #region <Coroutines>
    
    protected Action<CustomEventArgs.CommomActionArgs> AnimationSequenceHandler = args =>
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
                unit.UnitBoneAnimator.SetCast(animationToPlay.Key, animationToPlay.Value, unit.CastSpeed);
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