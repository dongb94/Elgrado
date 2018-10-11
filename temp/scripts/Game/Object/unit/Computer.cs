using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public abstract class Computer : Unit
{
    #region <Consts>

    protected const float IgnoreFocusUpdateTime = 1.5f;
    protected const float SqrCampReachConsiderateFactor = 1.5f * 1.5f;
    protected const int ObstacleTerrainColliderLayerMask = 1 << 1 | 1 << 13;
    protected const float InnerAttackRangeRate = 0.4f;
    
    #endregion </Consts>   
    
    #region <Fields>

    /* flag */
    public bool IsHadInitialAttackCooldown;
    public bool IsCanceledAttackWhenItHurt;
    public bool IsHealthBarVisibleOnlyHunt;
    public bool IsHealthBarVisibleWholeTime;

    /* attack activity */
    public int AttackPower;
    public float AttackRange;
    public int PreAttackCooldown;
    protected float AttackInnerRange;

    /* focus activity */
    public float FocusRadius;
    public float ReturnToCasterRadius;
    public float RestrictReturnToCasterRadius;
    protected float _ignoreFocusTime;
    protected Unit LastHurtingThisUnit;
    protected Unit Target;
    protected Vector3 inner_CampingPosition;

    /* move activity */
    [NonSerialized] public Activity UnitActivity = Activity.Rest;

    /* health bar */
    protected UIEnemyStateView _uiEnemyStateView;
    protected Quaternion _rotation;

    /* ally */
    protected Ally _Ally;
    
    /* spell */
    [NonSerialized] public int CurrentAttackCooldown;
    [NonSerialized] public int AttackCooldownLeft;    
    [NonSerialized] public int Damage;    
    public Action<UnitEventArgs>[] TriggerActionEvent;
    [NonSerialized] public UnitEventArgs _params;
    protected LinkedList<Action<UnitEventArgs>[]> _patternList;
    protected List<Pattern> PatternGroup;
    private int MaxFrequency;

    /* life span */
    [NonSerialized] public int MaxLifeSpanCount;
    protected bool _isValid;
    protected int inner_LifespanCount;
    
    /* Spell Transaction */
    [NonSerialized] public bool SpellTransactionFlag;
    
    /* Check Invincible */
    private bool _isCheckInvincible;
    
    #endregion </Fields>

    #region <Enums>

    public enum Ally
    {
        Player, Enemy, Neutral
    }

    public enum Activity
    {
        Hunt,
        Rest,
        Returntocamp
    }
    
    #endregion </Enums>

    #region <Callbacks>

    #region <AnimationEvent>
    
    public override void OnCastAnimationStandby()
    {
        if(TriggerActionEvent[(int) UnitEventType.Standby] != null) TriggerActionEvent[(int) UnitEventType.Standby].Invoke(_params.SetCaster(this));
    }

    public override void OnCastAnimationCue()
    {
        if(TriggerActionEvent[(int) UnitEventType.Cue] != null) TriggerActionEvent[(int) UnitEventType.Cue].Invoke(_params.SetCaster(this));
    }
    
    public override void OnCastAnimationCue2()
    {
        if(TriggerActionEvent[(int) UnitEventType.Cue2] != null) TriggerActionEvent[(int) UnitEventType.Cue2].Invoke(_params.SetCaster(this));
    }
        
    public override void OnCastAnimationCue3()
    {
        if(TriggerActionEvent[(int) UnitEventType.Cue3] != null) TriggerActionEvent[(int) UnitEventType.Cue3].Invoke(_params.SetCaster(this));
    }
        
    public override void OnCastAnimationCue4()
    {
        if(TriggerActionEvent[(int) UnitEventType.Cue4] != null) TriggerActionEvent[(int) UnitEventType.Cue4].Invoke(_params.SetCaster(this));
    }

    public override void OnCastAnimationExit()
    {
        if(TriggerActionEvent[(int) UnitEventType.Exit] != null) TriggerActionEvent[(int) UnitEventType.Exit].Invoke(_params.SetCaster(this));
    }

    public override void OnCastAnimationEnd()
    {
        if(TriggerActionEvent[(int) UnitEventType.End] != null) TriggerActionEvent[(int) UnitEventType.End].Invoke(_params.SetCaster(this));
    }

    public override void OnCastAnimationCleanUp(){}      

    #endregion
    
    #region <CustomEvent>
    
    public override void OnIdleRelax()
    {
        // not use
    }

    public override void OnHeartBeat()
    {
        base.OnHeartBeat();

        if (IsHealthBarVisibleWholeTime)
        {
            if (_uiEnemyStateView == null)
            {
                _uiEnemyStateView = ObjectManager.GetInstance.GetObject<UIEnemyStateView>(ObjectManager.PoolTag.General,
                    HUDManager.GetInstance.EnemyHealthBarPrefab, null, GameManager.GetInstance.UIRootTransform);
                if (MaxLifeSpanCount != 0) _uiEnemyStateView.SetType(UIEnemyStateView.GaugeType.LifeCount);
                _uiEnemyStateView.SetTrigger(this);
            }
            _uiEnemyStateView.UpdateState();
        }
    
        if (State == UnitState.Dead)
        {
            if (DecayTimeLeft <= 0)
            {
                MaterialApplier.RevertTrigger();
                ObjectManager.RemoveObject(this);
            }
            return;
        }

        if (UnitActivity == Activity.Hunt && IsInAttackRange)
        {
            TryAttack();
        }
    }
    
    #endregion

    #region <InstantiateEvent>

    public override void OnCreated()
    {
        base.OnCreated();
        _isCheckInvincible = false;
        _isValid = true;
        _uiEnemyStateView = null;
        LastHurtingThisUnit = null;
        Target = null;
        UnitActivity = Activity.Rest;
        UnitBoneAnimator.Initialize();
        _navMeshAgent.enabled = true;
        Damage = 0;
        MaxLifeSpanCount = inner_LifespanCount = 0;
        _patternList.Clear();
        CurrentAttackCooldown = AttackCooldownLeft = 0;
        SpellTransactionFlag = false;
    }
    
    protected override void OnDeath()
    {
        base.OnDeath();
        _isValid = false;
        // disalble navMeshAgent to not move when unit died
        if (_navMeshAgent != null) _navMeshAgent.enabled = false;
    }

    #endregion    

    #endregion
    
    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();
        PatternGroup = new List<Pattern>();
        _patternList = new LinkedList<Action<UnitEventArgs>[]>();
        _params = new UnitEventArgs();
        _ignoreFocusTime = .0f;
        ReturnToCasterRadius *= ReturnToCasterRadius;
        RestrictReturnToCasterRadius *= RestrictReturnToCasterRadius;
        AttackRange *= AttackRange;
        AttackInnerRange = AttackRange * InnerAttackRangeRate; 
        _navMeshAgent.speed = MovementSpeed; // set the maximum speed of navMeshAgent
        MaxFrequency = 0;
    }
    
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        if (!_isValid            
            || State == UnitState.Dead
            || UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Hit
            || UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Cast) return;
        UpdateFocus();
        
        switch (UnitActivity)
        {
            case Activity.Hunt:
                Hunt();
                break;
            case Activity.Rest:
                Rest();
                break;
            case Activity.Returntocamp:
                ReturnToCamp();
                break;
        }
        
        if (UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Move && UnitActivity != Activity.Returntocamp) return;

        switch (UnitActivity)
        {
            case Activity.Hunt:
                if (!IsInAttackRange)
                {
                    NavMeshMoveApply(Focus.transform.position);
                }
                break;
            case Activity.Returntocamp:
                NavMeshMoveApply(CampingPosition);
                break;
        }
    }

    #endregion </Unity/Callbacks>    

    #region <Properties>

    /* abstract per sub class */
    public abstract Vector3 CampingPosition { get; }

    public float SqrDistanceTowardTarget {
        get
        {
            var l_Target = Focus;
            return l_Target == null ? 0f : (l_Target.GetOrthographicPosition - GetOrthographicPosition).sqrMagnitude;
        }
    }
    
    public float SqrDistanceTowardBase {
        get { return (CampingPosition - Transform.position).sqrMagnitude; }
    }
    
    protected bool IsReadyToAttack
    {
        get { return AttackCooldownLeft <= 0 && UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Cast; }
    }
    
    protected bool IsInAttackInnerRange
    {
        // AttackRange is Squared value
        get { return SqrDistanceTowardTarget < AttackInnerRange; }
    }
    
    protected bool IsInAttackRange
    {
        // AttackRange is Squared value
        get { return SqrDistanceTowardTarget < AttackRange; }
    }
    
    public bool IsAnyObstacleBetweenFocus
    {
        get
        {
            var RayDistance = Mathf.Sqrt(SqrDistanceTowardTarget);
            if (Target == null) return true;
            return Physics.Raycast(AttachPoint[(int)AttachPointType.HeadTop_End].position, GetNormDirectionCandidateRandomAttachPoint(AttachPoint[(int)AttachPointType.HeadTop_End].position,Target), RayDistance, ObstacleTerrainColliderLayerMask);
        }
    }
    
    public int LifespanCount
    {
        get
        {
            return inner_LifespanCount;
        }
        set
        {
            inner_LifespanCount = value;
            if(inner_LifespanCount < 1 && State != UnitState.Dead) OnDeath();
        }
    }
    
    public Unit Focus
    {
        get
        {
            if (LastHurtingThisUnit != null && LastHurtingThisUnit.State != UnitState.Dead) return LastHurtingThisUnit;
            LastHurtingThisUnit = null;
            if (Target != null)
            {
                if( Target.State != UnitState.Dead && (Target.GetPosition - GetPosition).sqrMagnitude < FocusRadius ) return Target;
            }
            Target = null;
            
            int filteredObjectNumber;
            var filterMask = _isCheckInvincible ? UnitFilter.Condition.IsNegative | UnitFilter.Condition.IsAlive : UnitFilter.Condition
                .IsNegative | UnitFilter.Condition.IsAlive | UnitFilter.Condition.IsVulnerable;
            filteredObjectNumber = UnitFilter.GetUnitAtLocation(GetPosition, FocusRadius, this, filterMask, FilteredObjectGroup);
            
            if (filteredObjectNumber == 0) return null;
            Target = (Unit)FilteredObjectGroup[Random.Range(0, filteredObjectNumber)];
            return Target;
        }
    }
    
    #endregion </Properties>
    
    #region <UpdateBehaviours/Methods>
    
    protected void Hunt()
    {
        // When it's not in release condition,
        if (Focus != null)
        {
            if (IsAnyObstacleBetweenFocus) return;
            // Is satisfied with the attack range for trying this attack?

            if (IsInAttackInnerRange)
            {
                SetAngleToDestination(GetNormDirectionToMove(Focus));
                Transform.eulerAngles = Vector3.up * AngleToDestination;
                _navMeshAgent.enabled = false;
            }
            // If not satisfied the above condition,
            else if(!IsInAttackRange)
            {
                SwitchActivity(Activity.Hunt);
                NavMeshMoveApply(Focus.Transform.position);
            }
        }
        else
        {
            SwitchActivity(Activity.Returntocamp);
        }
    }
    
    protected void Rest()
    {
        if (Tension > 0)
            --Tension;
    }

    protected void ReturnToCamp()
    {                      
        // Sync the height coordinate.                
        if (SqrDistanceTowardBase <= SqrCampReachConsiderateFactor)
        {
            SwitchActivity(Activity.Rest);
        }        
    }        
    
    #endregion </UpdateBehaviours/Methods>
    
    #region <Methods>
        
    public void NavMeshMoveApply(Vector3 p_TargetPosition)
    {
        if (_navMeshAgent == null) return;
        _navMeshAgent.enabled = true;
        _navMeshAgent.SetDestination(p_TargetPosition);
        Move(p_TargetPosition);
    }
    
    public void NavMeshStop()
    {
        _navMeshAgent.ResetPath();
    }
    
    protected override void UnitMove(Vector3 forceVector)
    {
        if (_navMeshAgent != null && _navMeshAgent.enabled)
        {
            NavMeshStop();
            _navMeshAgent.Move(forceVector);
        }
        else
        {
            base.UnitMove(forceVector);
        }
    }
    
    public override void Move(Vector3 forceVector)
    {
        base.Move(GetNormDirectionToMove(forceVector));
        if (UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Move)
            UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Move);
    }

    public override void Hurt(Unit caster, int damage, TextureType type, Vector3 forceDirection, 
        Action<Unit, Unit, Vector3> action = null)
    {                
        base.Hurt(caster, damage, type, forceDirection, action);                
        ResetFromCast();
        
        if (State == UnitState.Lives)
        {
            UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Hit);
        }

        // @Temp: when it attacked in out of range, it would be tracking on you until you're in dead.
        if(LastHurtingThisUnit == null || !LastHurtingThisUnit.ActiveSelf) LastHurtingThisUnit = caster;
        SwitchActivity(Activity.Hunt);

        if (State == UnitState.Dead)
        {
            forceDirection *= (Math.Abs(CurrentHealthPoint) + 1) * 3;
            AddForce(forceDirection);
        }                
        
        if (_uiEnemyStateView != null)        
            _uiEnemyStateView.UpdateState();

        if (IsCanceledAttackWhenItHurt)
        {
            SetAttackCooldown(PreAttackCooldown);
        }
    }
    
    protected void TryAttack()
    {
        if (IsReadyToAttack)
        {
            AttackTrigger();
        }
        else
        {
            if (UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Cast 
                && UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Hit)
                SetAttackCooldown(AttackCooldownLeft - 1);            
        }
    }
    
    public virtual void AttackTrigger()
    {
        if (SpellTransactionFlag) return;
        _navMeshAgent.enabled = false;
        if (_patternList.Count == 0)
        {
            var selectedSkill = Random.Range(0, MaxFrequency);
            var currentSkillFrequency = 0;
            foreach (var skill in PatternGroup)
            {
                currentSkillFrequency += skill.frequency;
                if (currentSkillFrequency >= selectedSkill)
                {
                    foreach (var node in skill.actionQueue)
                    {
                        _patternList.AddLast(node);
                    }
                    _patternList.Last.Value[(int) UnitEventType.End]+= eventArgs =>
                    {
                        AttackCooldownLeft += skill.patternCooldown;
                        if (IsHealthBarVisibleOnlyHunt && !IsHealthBarVisibleWholeTime && _uiEnemyStateView != null) _uiEnemyStateView.UpdateState();
                    };    
                    break;
                }
            }
        }
        TriggerActionEvent = _patternList.First.Value;
        _patternList.RemoveFirst();
        if(TriggerActionEvent[(int) UnitEventType.Begin] != null) TriggerActionEvent[(int) UnitEventType.Begin].Invoke(_params.SetCaster(this));
    }

    protected void SwitchActivity(Activity activity)
    {
        if (UnitActivity == activity && State == UnitState.Lives) return;
                
        switch (activity)
        {
            case Activity.Hunt:
                
                if (IsHealthBarVisibleOnlyHunt && !IsHealthBarVisibleWholeTime)
                {
                    if (_uiEnemyStateView == null)
                    {
                        _uiEnemyStateView = ObjectManager.GetInstance.GetObject<UIEnemyStateView>(ObjectManager.PoolTag.General,
                            HUDManager.GetInstance.EnemyHealthBarPrefab, null, GameManager.GetInstance.UIRootTransform);
                        if (MaxLifeSpanCount != 0) _uiEnemyStateView.SetType(UIEnemyStateView.GaugeType.LifeCount);
                        _uiEnemyStateView.SetTrigger(this);
                    }
                }

                if (!IsAnyObstacleBetweenFocus)
                {
                    UpdateTension();
                }

                break;
            
            case Activity.Rest:
                if (IsHealthBarVisibleOnlyHunt && !IsHealthBarVisibleWholeTime){
                    if (_uiEnemyStateView != null)
                    {
                        ObjectManager.RemoveObject(_uiEnemyStateView);
                        _uiEnemyStateView = null;
                    }
                }
                
                if (IsHadInitialAttackCooldown && AttackCooldownLeft < PreAttackCooldown)
                    SetAttackCooldown(PreAttackCooldown);
                UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Idle);
                _navMeshAgent.enabled = false;
                break;
                
            case Activity.Returntocamp:
                if (IsHealthBarVisibleOnlyHunt && !IsHealthBarVisibleWholeTime){
                    if (_uiEnemyStateView != null)
                    {
                        ObjectManager.RemoveObject(_uiEnemyStateView);
                        _uiEnemyStateView = null;
                    }
                }
                
                if (IsHadInitialAttackCooldown && AttackCooldownLeft < PreAttackCooldown)
                    SetAttackCooldown(PreAttackCooldown);
                _ignoreFocusTime = IgnoreFocusUpdateTime;
                break;
        }       
        UnitActivity = activity;
    }
    
    protected void UpdateFocus()
    {
        if (Focus == null)
        {
            if (SqrDistanceTowardBase > ReturnToCasterRadius)
            {
                if(LastHurtingThisUnit == null) SwitchActivity(Activity.Returntocamp);
            }
        }
        else
        {
            if (SqrDistanceTowardBase > RestrictReturnToCasterRadius)
            {
                LastHurtingThisUnit = null;
                SwitchActivity(Activity.Returntocamp);
                return;
            }    
            
            if (_ignoreFocusTime > Mathf.Epsilon)
            {
                _ignoreFocusTime -= Time.fixedDeltaTime;
                return;
            }
            
            SwitchActivity(Activity.Hunt);
        }
    }

    protected void SetAttackCooldown(int value)
    {
        AttackCooldownLeft = Math.Max(0, value);
        if (IsHealthBarVisibleOnlyHunt && !IsHealthBarVisibleWholeTime && _uiEnemyStateView != null) _uiEnemyStateView.UpdateState();
    }

    protected void SpellInitialize()
    {
        foreach (var pattern in PatternGroup)
        {
            MaxFrequency += pattern.frequency;
        }
    }
    
    public override void ResetFromCast()
    {       
        base.ResetFromCast();
        SpellTransactionFlag = false;
        _navMeshAgent.enabled = true;
    }

    public Computer SetCheckInvincible(bool p_Flag)
    {
        _isCheckInvincible = p_Flag;

        return this;
    }

    #endregion </Methods>

    #region <Struct>
    
    public struct Pattern
    {
        public LinkedList<Action<UnitEventArgs>[]> actionQueue;
        public int frequency;
        public int patternCooldown;

        public Pattern(int frequency = 1, int cooldown = 0)
        {
            actionQueue = new LinkedList<Action<UnitEventArgs>[]>();
            this.frequency = frequency;
            patternCooldown = cooldown;
        }
    }
    
    #endregion </Structs>
    
}