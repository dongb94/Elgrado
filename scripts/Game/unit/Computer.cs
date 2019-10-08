using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public abstract class Computer : Unit
{
    #region <Consts>

    protected const float IgnoreFocusUpdateTime = 1.5f;
    protected const float SqrCampReachConsiderateFactor = 1.5f * 1.5f;
    protected const int ObstacleTerrainColliderLayerMask = 1 << 1 | 1 << 13;
    protected const float InnerAttackRangeRate = 0.8f;
    
    #endregion </Consts>   
    
    #region <Fields>

    /* flag */
    public bool IsHadInitialAttackCooldown;
    public bool IsCanceledAttackWhenItHurt;
    public bool IsHealthBarVisibleOnlyHunt;
    public bool IsHealthBarVisibleWholeTime;

    /* attack activity */
    public float AttackRange;
    protected float AttackInnerRange;

    /* focus activity */
    public float FocusRadius;
    public float ReturnToCasterRadius;
    public float RestrictReturnToCasterRadius;
    protected float _ignoreFocusTime;
    protected Unit CurrentPriorAggroUnit;
    protected Unit CurrentAggroUnit;
    protected Vector3 inner_CampingPosition;
    protected bool FixLookForwardFlag;

    /* move activity */
    [NonSerialized] public Activity UnitActivity = Activity.Rest;

    /* health bar */
    protected UIEnemyStateView _uiEnemyStateView;

    /* spell */
    public Action<UnitEventArgs>[] TriggerActionEvent;
    [NonSerialized] public UnitEventArgs _params;
    protected List<Pattern> PatternGroup;
    protected Pattern CurrentPattern;
    protected LinkedList<Action<UnitEventArgs>[]> ReservationActionList;
    protected int _maxFrequency;
    public int Delay;
    public int MaxDelay;

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
            TryPushNextPattern();
        }
        PatternGroup.ForEach(pattern =>
        {
            pattern.PatternCooldown = Math.Max(0, pattern.PatternCooldown - 1);
        });
        
        Delay = Math.Max(0, Delay - 1);
    }

    #endregion

    #region <InstantiateEvent>

    public override void OnCreated()
    {
        base.OnCreated();
        _isCheckInvincible = false;
        _isValid = true;
        _uiEnemyStateView = null;
        CurrentPriorAggroUnit = null;
        CurrentAggroUnit = null;
        UnitActivity = Activity.Rest;
        UnitBoneAnimator.Initialize();
        _navMeshAgent.enabled = true;
        MaxLifeSpanCount = inner_LifespanCount = 0;
        ReservationActionList.Clear();
        SpellTransactionFlag = false;
        CurrentPattern = null;
        FixLookForwardFlag = false;
    }
    
    public override void OnDeath()
    {
        base.OnDeath();
        _isValid = false;
        _navMeshAgent.enabled = false;
    }

    #endregion    

    #endregion
    
    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();
        PatternGroup = new List<Pattern>();
        ReservationActionList = new LinkedList<Action<UnitEventArgs>[]>();
        _params = new UnitEventArgs();
        _ignoreFocusTime = .0f;
        ReturnToCasterRadius *= ReturnToCasterRadius;
        RestrictReturnToCasterRadius *= RestrictReturnToCasterRadius;
        AttackRange *= AttackRange;
        AttackInnerRange = AttackRange * InnerAttackRangeRate * InnerAttackRangeRate; 
        _navMeshAgent.speed = MovementSpeed; // set the maximum speed of navMeshAgent
        _maxFrequency = 0;
        Delay = 0;
    }
    
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        if (!_isValid            
            || State == UnitState.Dead
            || UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Hit
            || UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Cast) return;
        UpdateFocus();
        TryNextAction();
        
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
    
    public float SqrDistanceTowardBase => (CampingPosition - Transform.position).sqrMagnitude;

    public float SqrDistanceTowardBase2D => Vector3.Scale(new Vector3(1f,0f,1f), CampingPosition - Transform.position).sqrMagnitude;
    
    protected bool IsReadyToAttack => Delay <= 0 && UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Cast;

    protected bool IsInAttackRange => SqrDistanceTowardTarget < AttackRange;
    
    protected bool IsInAttackInnerRange => SqrDistanceTowardTarget < AttackInnerRange;
    
    public bool IsAnyObstacleBetweenFocus
    {
        get
        {
            var rayDistance = Mathf.Sqrt(SqrDistanceTowardTarget);
            if (CurrentAggroUnit == null) return true;
            return Physics.Raycast(AttachPoint[(int) AttachPointType.HeadTop_End].position,
                GetNormDirectionCandidateRandomAttachPoint(AttachPoint[(int) AttachPointType.HeadTop_End].position,
                    CurrentAggroUnit), rayDistance, ObstacleTerrainColliderLayerMask);
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
            if (FixLookForwardFlag) return null;
            if (CurrentPriorAggroUnit != null && CurrentPriorAggroUnit.State != UnitState.Dead) return CurrentPriorAggroUnit;
            CurrentPriorAggroUnit = null;
            if (CurrentAggroUnit != null)
            {
                if( CurrentAggroUnit.State != UnitState.Dead && (CurrentAggroUnit.GetPosition - GetPosition).sqrMagnitude < FocusRadius ) return CurrentAggroUnit;
            }
            CurrentAggroUnit = null;
            
            int filteredObjectNumber;
            var filterMask = _isCheckInvincible ? UnitFilter.Condition.IsNegative | UnitFilter.Condition.IsAlive : UnitFilter.Condition
                .IsNegative | UnitFilter.Condition.IsAlive | UnitFilter.Condition.IsVulnerable;
            filteredObjectNumber = UnitFilter.GetUnitAtLocation(GetPosition, FocusRadius, this, filterMask, FilteredObjectGroup);
            
            if (filteredObjectNumber == 0) return null;

            CurrentAggroUnit = (Unit)FilteredObjectGroup[Random.Range(0, filteredObjectNumber)];
            return CurrentAggroUnit;
        }
    }
    
    #endregion </Properties>
    
    #region <UpdateBehaviours/Methods>
    
    protected virtual void Hunt()
    {
        // When it's not in release condition,
        if (Focus != null)
        {
            if (IsAnyObstacleBetweenFocus) return;
            // Is satisfied with the attack range for trying this attack?

            UpdateTension();
            
            if (IsInAttackInnerRange)
            {
                SetAngleToDestination(GetNormDirectionToMove(Focus));
                Transform.eulerAngles = Vector3.up * AngleToDestination;
                _navMeshAgent.enabled = false;
                RunningTime = 0f;
            }else if (IsInAttackRange && _navMeshAgent.enabled)
            {
                SetAngleToDestination(GetNormDirectionToMove(Focus));
                Transform.eulerAngles = Vector3.up * AngleToDestination;                
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
            if(!FixLookForwardFlag) SwitchActivity(Activity.Returntocamp);
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
        if (SqrDistanceTowardBase2D <= SqrCampReachConsiderateFactor)
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
        if (forceVector.sqrMagnitude < Mathf.Epsilon) return;
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
        if(CurrentPriorAggroUnit == null || CurrentPriorAggroUnit.State == UnitState.Dead) CurrentPriorAggroUnit = caster;
        SwitchActivity(Activity.Hunt);

        if (State == UnitState.Dead)
        {
            forceDirection *= (Math.Abs(CurrentHealthPoint) + 1) * 3;
            AddForce(forceDirection);
        }                
        
        if (_uiEnemyStateView != null)        
            _uiEnemyStateView.UpdateState();
    }
    
    public virtual void TryPushNextPattern()
    {
        PushNextPattern();
    }
    
    public virtual void PushNextPattern()
    {
        if (SpellTransactionFlag) return;
        _navMeshAgent.enabled = false;
        if (ReservationActionList.Count != 0) return;
        PatternGroupInitialize();
        var selectedSkill = Random.Range(0, _maxFrequency);
        var currentSkillFrequency = 0;
        foreach (var pattern in PatternGroup)
        {
            if(pattern.PatternCooldown != 0) continue;
            currentSkillFrequency += pattern.Frequency;
            if (currentSkillFrequency <= selectedSkill) continue;
            CurrentPattern = pattern;
            foreach (var node in pattern.ActionQueue)
            {
                ReservationActionList.AddLast(node);
            }
        }
    }

    public virtual void TryNextAction()
    {
        if (!IsReadyToAttack || ReservationActionList.Count == 0) return;
        TriggerActionEvent = ReservationActionList.First.Value;
        ReservationActionList.RemoveFirst();
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
                
                _ignoreFocusTime = IgnoreFocusUpdateTime;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(activity), activity, null);
        }       
        UnitActivity = activity;
    }
    
    protected void UpdateFocus()
    {
        if (Focus == null)
        {
            if (!(SqrDistanceTowardBase > ReturnToCasterRadius)) return;
            if(CurrentPriorAggroUnit == null) SwitchActivity(Activity.Returntocamp);
        }
        else
        {
            if (SqrDistanceTowardBase > RestrictReturnToCasterRadius)
            {
                CurrentPriorAggroUnit = null;
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

    protected void PatternGroupInitialize()
    {
        _maxFrequency = 0;
        foreach (var pattern in PatternGroup)
        {
            if (pattern.PatternCooldown != 0) continue;
            _maxFrequency += pattern.Frequency;
        }
    }
    
    public override void ResetFromCast()
    {       
        base.ResetFromCast();
        SpellTransactionFlag = false;
        _navMeshAgent.enabled = true;
        _params.ResetBuilder();
    }

    public Computer SetCheckInvincible(bool p_Flag)
    {
        _isCheckInvincible = p_Flag;

        return this;
    }
    
    // cooldown between actions
    public Action<UnitEventArgs>[] SetDelay(int delay=0)
    {
        var delaySetActionGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];
        delaySetActionGroup[(int) UnitEventType.Begin] = (args) =>
        {
            MaxDelay = Delay = Math.Max(0,delay);
        };
        return delaySetActionGroup;
    }

    public void FixCurrentForward()
    {
        FixLookForwardFlag = true;
    }

    #endregion </Methods>

    #region <Struct>
    
    public class Pattern
    {
        public readonly LinkedList<Action<UnitEventArgs>[]> ActionQueue;
        public int Frequency;
        private readonly Action<UnitEventArgs>[] _patternCooldownSetActionGroup;
        public int PatternCooldown;        //cooldown for each pattern

        public Pattern(int frequency = 1)
        {
            ActionQueue = new LinkedList<Action<UnitEventArgs>[]>();
            _patternCooldownSetActionGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];
            Frequency = frequency;
        }

        public Pattern SetNextPattern(Action<UnitEventArgs>[] p_NextPattern)
        {
            ActionQueue.AddLast(p_NextPattern);

            return this;
        }

        public Pattern SetPatternCooldown(int cooldown, bool relative = false)
        {
            _patternCooldownSetActionGroup[(int) UnitEventType.Begin] = (args) =>
            {
                PatternCooldown = Math.Max(0, relative?
                    PatternCooldown + cooldown
                    : cooldown);
            };
            ActionQueue.AddLast(_patternCooldownSetActionGroup);

            return this;
        }
    }
    
    #endregion </Structs>
    
}