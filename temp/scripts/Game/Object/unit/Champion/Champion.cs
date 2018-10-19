using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Champion : Unit
{        

    #region <Consts>

    protected static void DefaultReset(UnitEventArgs eventArgs)
    {
        var caster = (Champion) eventArgs.Caster;
        caster.OnCastAnimationCleanUp();   
    }
    
    #endregion </Consts>
    
    #region <Fields>

    public List<List<Action<UnitEventArgs>[]>> ActionGroupRoot { get; private set; }
    public Dictionary<List<Action<UnitEventArgs>[]>, ActionStatus> ActionStatusGroup { get; private set; }
    public Dictionary<List<Action<UnitEventArgs>[]>, UnitEventArgs> ActionArgs { get; private set; }
    
    
    public int CurrentActionPoint { get; private set; }
    [SerializeField] private int _maximumActionPoint;

    private List<Action<UnitEventArgs>[]> _currentActionGroup;
    private ActionStatus _currentActionStatus;
    private UnitEventArgs _currentActionArgs;
    private UnitEventType _currentSequence;
    
    #endregion
    
    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();       
        
        ActionGroupRoot = new List<List<Action<UnitEventArgs>[]>>();
        ActionStatusGroup = new Dictionary<List<Action<UnitEventArgs>[]>, ActionStatus>();
        ActionArgs = new Dictionary<List<Action<UnitEventArgs>[]>, UnitEventArgs>();
        
        for (var actionButtonTriggerIndex = 0;
            actionButtonTriggerIndex < (int) ActionButtonTrigger.Type.Count;
            ++actionButtonTriggerIndex)
        {
            var lastCreatedActionGroup = new List<Action<UnitEventArgs>[]>();
            
            ActionGroupRoot.Add(lastCreatedActionGroup);
            ActionStatusGroup.Add(lastCreatedActionGroup, new ActionStatus());
            ActionArgs.Add(lastCreatedActionGroup, new UnitEventArgs());
        }
        CurrentActionPoint = _maximumActionPoint;

    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (CurrentAction != null && CurrentAction[(int) UnitEventType.OnFixedUpdate] != null)
            CurrentAction[(int) UnitEventType.OnFixedUpdate](CurrentActionArgs);
    }

    private void OnAnimatorMove()
    {
        if (FixedFocusManager.GetInstance.IsValid &&
            FixedFocusManager.GetInstance.IsOverFrontier(GetPosition + 5.14f * UnitBoneAnimator.UnityAnimator.deltaPosition))
        {
            return;
        }
        
        if (UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Idle ||
            UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Move)
        {
            if (Speed < Mathf.Epsilon)
            {
                return;
            }
        }
        Speed = 0f;
        transform.position += UnitBoneAnimator.UnityAnimator.deltaPosition;
        transform.forward = UnitBoneAnimator.UnityAnimator.deltaRotation * transform.forward;
    }

    #endregion

    #region <Callbacks>

    public override void OnCreated()
    {
        base.OnCreated();
        if (!HUDManager.GetInstance.JoystickController.IsEventHoldOn)
            RunningTime = .0f;
    }

    public override void OnIdleRelax()
    {
        if (CurrentAction != null && CurrentAction[(int) UnitEventType.OnRelax] != null)
            CurrentAction[(int) UnitEventType.OnRelax](CurrentActionArgs);
    }

    public override void OnDeath()
    {
        CurrentHealthPoint = MaximumHealthPoint;
//        SoundManager.GetInstance
//            .CastSfx(SoundManager.AudioMixerType.VOICE, ChampionType, K514SfxStorage.ActivityType.Dead).SetTrigger();
    }
    
    public void OnActionTrigger(ActionButtonTrigger actionButtonTriggerCaster, ActionButtonTrigger.Type actionType)
    {
        var actionTypeId = (int) actionType;
        var actionGroup = ActionGroupRoot[actionTypeId];       
        
        if (ActionStatusGroup[actionGroup].CurrentCooldown > 0) return;
        if (ActionStatusGroup[actionGroup].MaximumStack > 0 && ActionStatusGroup[actionGroup].CurrentStack == 0 && !ActionStatusGroup[actionGroup].isNotRefill) return;
        if (CurrentActionArgs != null && CurrentActionArgs.TransitionRestrictTrigger) return;
        if (CurrentActionGroup != null && CurrentActionGroup != actionGroup) OnTransitionToOtherCast();       
        
        CurrentActionGroup = actionGroup;
        CurrentActionArgs.SetActionTrigger(actionButtonTriggerCaster).SetCaster(this);
        // when trigger pressed, set triggerState bool
        CurrentActionArgs.SetIsTriggerSet(true);
        
        ActionTrigger(UnitEventType.SetTrigger);
    }

    public override void OnCastAnimationStandby()
    {
        if(CheckNextEvent(UnitEventType.Standby)) ActionTrigger(UnitEventType.Standby);
    }

    public override void OnCastAnimationCue()
    {
        if(CheckNextEvent(UnitEventType.Cue)) ActionTrigger(UnitEventType.Cue);
    }

    public override void OnCastAnimationCue2()
    {        
        if(CheckNextEvent(UnitEventType.Cue2)) ActionTrigger(UnitEventType.Cue2);
    }
    
    public override void OnCastAnimationCue3()
    {        
        if(CheckNextEvent(UnitEventType.Cue3)) ActionTrigger(UnitEventType.Cue3);
    }
    
    public override void OnCastAnimationCue4()
    {        
        if(CheckNextEvent(UnitEventType.Cue4)) ActionTrigger(UnitEventType.Cue4);
    }
    
    // this is eventkey call back, not script
    public override void OnCastAnimationExit()
    {                
        if(CheckNextEvent(UnitEventType.Exit)) ActionTrigger(UnitEventType.Exit);
    }

    // when cast animation end, this is script control
    public override void OnCastAnimationEnd()
    {        
        ActionTrigger(UnitEventType.End);
    }

    // when transition Cast -> Other, always invoked once at one animation sequence
    public override void OnCastAnimationCleanUp()
    {
        if (ActionTrigger(UnitEventType.CleanUp))
            ResetFromCast();
    }

    // when transition Cast -> Another Cast
    public void OnTransitionToOtherCast()
    {        
        ActionTrigger(UnitEventType.OnTransitionToOtherCast);
        OnCastAnimationCleanUp();
    }
    
    // when Action Trigger Released
    public void OnActionTriggerEnd(ActionButtonTrigger.Type p_TriggerType)
    {
        var actionTypeId = (int) p_TriggerType;
        var actionGroup = ActionGroupRoot[actionTypeId];
        var actionUnitEventArgs = ActionArgs[actionGroup];
        
        if (CurrentActionArgs != null && CurrentActionArgs.ActionButtonTrigger.ActionButtonType == p_TriggerType 
         && CurrentActionArgs.IsTriggerSet && !CurrentActionArgs.ActionTriggerReleaseEventDeferredFlag)
        {
            if (!CurrentActionArgs.IsActionTriggerReleaseEventDeferred) ActionTrigger(UnitEventType.OnActionTriggerEnd);
            else CurrentActionArgs.SetActionTriggerReleaseEventDeferredFlag(true);
        }
        
        
        actionUnitEventArgs.SetIsTriggerSet(false);
    }

    public override void OnHeartBeat()
    {
        base.OnHeartBeat();
        
        foreach (var actionStatusKeyValuePair in ActionStatusGroup)
        {
            var actionStatus = actionStatusKeyValuePair.Value;      
            
            actionStatus.CurrentCooldown = Math.Max(0, actionStatus.CurrentCooldown - 1);
            
            actionStatus.CurrentStackCooldown = Math.Max(0, actionStatus.CurrentStackCooldown - 1);
            if (actionStatus.CurrentStackCooldown == 0 && 
                actionStatus.CurrentStack < actionStatus.MaximumStack && !actionStatus.isNotRefill)
            {
                actionStatus.CurrentStackCooldown = actionStatus.MaximumStackCooldown;
                actionStatus.CurrentStack++;
            }
        }

        if (CurrentAction != null && CurrentAction[(int) UnitEventType.OnHeartBeat] != null)
            CurrentAction[(int) UnitEventType.OnHeartBeat](CurrentActionArgs);
    }
    
    #endregion </Callbacks>
    
    #region <Properties>
    
    public Action<UnitEventArgs>[] CurrentAction => _currentActionGroup?[CurrentActionStatus.CurrentChain];

    public List<Action<UnitEventArgs>[]> CurrentActionGroup
    {
        get => _currentActionGroup;
        private set
        {                        
            _currentActionGroup = value;

            if (value != null)
            {
                _currentActionArgs = ActionArgs[value];
                _currentActionStatus = ActionStatusGroup[value];
            }
            else
            {
                _currentActionArgs = null;
                _currentActionStatus = null;
                HUDManager.GetInstance.State = HUDManager.HUDState.Playing;
            }
        }

    }
    public ActionStatus CurrentActionStatus
    {
        get { return _currentActionStatus; }
        private set { _currentActionStatus = value; }
    }
    public UnitEventArgs CurrentActionArgs
    {
        get { return _currentActionArgs; }
        private set { _currentActionArgs = value; }
    }
    
    public float ActionPointRate
    {
        get { return (float) CurrentActionPoint / _maximumActionPoint;  }
    }
    
    #endregion </Properties>
    
    #region <Methods>

    public bool CheckAndInvokeDeferredActionTriggerReleased(bool p_InvokeActionKey = false)
    {
        if (CurrentActionArgs.ActionTriggerReleaseEventDeferredFlag)
        {
            if (p_InvokeActionKey)
            {
                CurrentActionArgs.SetActionTriggerReleaseEventDeferredFlag(false);
                ActionTrigger(UnitEventType.OnActionTriggerEnd);
            }
            return true;
        }
        return false;
    }

    public override void Move(Vector3 forceVector)
    {
        if (  UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Hit
              ||  UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Cast)
        {
            RunningTime = UpdateRunningTime * 0.1f;
            return;
        }

        forceVector.z = forceVector.y;
        forceVector.y = 0;
        base.Move(forceVector);
    }
    
    public override void Hurt(Unit caster, int damage, TextureType type, Vector3 forceDirection, 
        Action<Unit, Unit, Vector3> action = null)
    {        
        base.Hurt(caster, damage, type, forceDirection, action);
        if( CurrentActionArgs !=null ) CurrentActionArgs.SetTransitionRestrict(false);
        ResetFromCast();
        
        if (UnitFilter.Check(this, UnitFilter.Condition.IsAlive | UnitFilter.Condition.IsVulnerable))
        {
//            SoundManager.GetInstance
//                .CastSfx(SoundManager.AudioMixerType.VOICE, ChampionType, K514SfxStorage.ActivityType.Hitted).SetTrigger();
            if (forceDirection.sqrMagnitude > Mathf.Epsilon)
            {
                UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Hit);
                UpdateTension();
            }
        }

        HUDManager.GetInstance.UpdateChampionStateView();
        CameraManager.GetInstance.SetVibrateFx(damage, 0.15f);
    }

    public void CleanUp()
    {
        ForceVector = Vector3.zero;                
        RunningTime = .0f;
    }
   
    public override void ResetFromCast()
    {
        if (CurrentActionArgs != null) CurrentActionArgs.SetActionTriggerReleaseEventDeferredFlag(false).SetIsTriggerSet(false);
        _currentSequence = UnitEventType.None;
        CurrentActionGroup = null;
        base.ResetFromCast();        
    }        

    /// <summary>
    /// Actually trigger the action based on EventInfo.
    /// </summary>
    /// <param name="unitEventType">Used to what to do trigger the event.</param>
    /// <returns>Returns about is the action triggered.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Not defined action.</exception>
    public bool ActionTrigger(UnitEventType unitEventType)
    {
        if (CurrentAction == null) return false;
        
        if (CurrentAction[(int) unitEventType] == null) return true;                
        CurrentAction[(int) unitEventType](CurrentActionArgs);

        if (CurrentAction == null) return true;
        
        switch (unitEventType)
        {
            case UnitEventType.Initialize:
                break;
            case UnitEventType.SetTrigger:
                break;
            case UnitEventType.Begin:
                break;
            case UnitEventType.Standby:
                break;
            case UnitEventType.Cue:
                break;
            case UnitEventType.Cue2:
                break;
            case UnitEventType.Cue3:
                break;
            case UnitEventType.Cue4:
                break;
            case UnitEventType.Exit:
                break;
            case UnitEventType.End:
                break;
            case UnitEventType.CleanUp:                
                break;
            case UnitEventType.OnHeartBeat:
                break;
            case UnitEventType.OnFixedUpdate:
                break;
            case UnitEventType.OnRelax:
                break;
            case UnitEventType.OnTransitionToOtherCast:
                break;
            case UnitEventType.OnActionTriggerEnd:
                break;
            case UnitEventType.None:
            case UnitEventType.Count:
            default:
                throw new ArgumentOutOfRangeException("UnitEventType", unitEventType, null);
        }

        return true;
    }

    public bool CheckNextEvent(UnitEventType toTransition)
    {
        if (CurrentAction == null) return false;
        
        if (_currentSequence == toTransition)
        {
            Debug.Log("toTransition Reflected : " + toTransition);
            return false;
        }
        _currentSequence = toTransition;
        
        var isFlagged = toTransition == NextEvent || NextEvent == UnitEventType.None;
        
        if (isFlagged)
        {
            Debug.Log("toTransition : " + toTransition + "  /  NextEvent : " + NextEvent + "  /  Event Hash : " + CurrentAction.GetHashCode());
            NextEvent = UnitEventType.None;
        }

        return isFlagged;
    }

    public Computer GetClosestEnemy(float radius)
    {
        UnitFilter.GetUnitAtLocation(GetPosition, radius, this,
            UnitFilter.Condition.IsNegative | UnitFilter.Condition.IsVulnerable | UnitFilter.Condition.HasFaceToFace | UnitFilter.Condition.IsAlive);
        return UnitFilter.GetClosestEnemy(UnitFilter.GetLastFilteredGroup);
    }

    public Computer DetectAndChaseEnemyInRange(float radius, float chaseRate, float rushRate)
    {
        var focusEnemy = GetClosestEnemy(radius);

        ChaseOrRush(focusEnemy, chaseRate, rushRate);

        return focusEnemy;
    }

    #endregion
    
    #region <Classes>
    
    public class ActionStatus
    {        
        public CommomActionArgs EventArgs;
        
        private int _maximumCooldown, _currentCooldown;
        private int _maximumStackCooldown, _currentStackCooldown;
        private int _maximumStack, _currentStack;
        private int _maximumChain, _currentChain;
        public bool isNotRefill;
        
        public int MaximumCooldown
        {
            get { return _maximumCooldown;}
            private set
            {
                _maximumCooldown = value;
                HUDManager.GetInstance.OnActionStatusUpdate();
            }
        }

        public int MaximumStackCooldown
        {
            get { return _maximumStackCooldown; }
            private set
            {
                _maximumStackCooldown = value;
                HUDManager.GetInstance.OnActionStatusUpdate();
            }
        }

        public int MaximumStack
        {
            get { return _maximumStack; }
            private set 
            {
                _maximumStack = value;
                HUDManager.GetInstance.OnActionStatusUpdate(); 
            }
        }

        public int MaximumChain
        {
            get { return _maximumChain; }
            private set
            {
                _maximumChain = value;             
            }
        }
        
        public int CurrentCooldown
        {
            get { return _currentCooldown; }
            set
            {
                _currentCooldown = value;
                HUDManager.GetInstance.OnActionStatusUpdate();
            }
        }

        public int CurrentStackCooldown
        {
            get { return _currentStackCooldown; }
            set
            {
                _currentStackCooldown = value;
                HUDManager.GetInstance.OnActionStatusUpdate();
            }
        }

        public int CurrentStack
        {
            get { return _currentStack; }
            set
            {
                _currentStack = value;
                HUDManager.GetInstance.OnActionStatusUpdate();
            }
        }        

        public int CurrentChain
        {
            get { return _currentChain; }
            set
            {
                _currentChain = value;
                if (_currentChain >= MaximumChain) _currentChain = 0;
            }
        }

        public ActionStatus SetCooldown(int cooldown)
        {
            MaximumCooldown = cooldown;
            CurrentCooldown = cooldown;
                        
            return this;
        }

        public ActionStatus SetStackCooldown(int cooldown)
        {
            _maximumStackCooldown = cooldown;
            _currentStackCooldown = cooldown;

            return this;
        }

        public ActionStatus SetStack(int stack)
        {
            _maximumStack = stack;
            _currentStack = stack;
            return this;
        }

        public ActionStatus SetChain(int chain)
        {
            MaximumChain = chain;
            return this;
        }
    }

    #endregion </Classes>
    
}