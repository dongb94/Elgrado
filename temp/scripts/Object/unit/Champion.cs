using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Champion : Unit
{        

    #region <Consts>

    protected static void DefaultReset(UnitEventArgs eventArgs)
    {
        var caster = (Champion) eventArgs.Caster;
        caster.ResetFromCast();        
    }
    
    #endregion </Consts>
    
    #region <Fields>

    public List<List<Action<UnitEventArgs>[]>> ActionGroupRoot { get; private set; }
    public Dictionary<List<Action<UnitEventArgs>[]>, ActionStatus> ActionStatusGroup { get; private set; }
    public Dictionary<List<Action<UnitEventArgs>[]>, UnitEventArgs> ActionArgs { get; private set; }
    [NonSerialized]public SpellHyperParameter[] hyperParameterSet;

    public int CurrentActionPoint { get; private set; }
    [SerializeField] private int _maximumActionPoint;

    private List<Action<UnitEventArgs>[]> _currentActionGroup;
    private ActionStatus _currentActionStatus;
    private UnitEventArgs _currentActionArgs;
    
    #endregion

    #region <Enums>
    
    public enum HyperParameterOfSpell
    {
        NormalAttack01, NormalAttack02, NormalAttack03, Spell01, Spell02, Spell03, Spell04, Spell05, Spell06, Spell07, Spell08, Spell09, Spell10, End
    }
    
    #endregion </Enums>

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
        
        hyperParameterSet = new SpellHyperParameter[(int) HyperParameterOfSpell.End];
    }

    protected void OnEnable()
    {                
        MaterialApplier.RevertTrigger();
        if (!HUDManager.GetInstance.JoystickController.IsEventHoldOn)
            RunningTime = .0f;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (CurrentAction != null && CurrentAction[(int) UnitEventType.OnFixedUpdate] != null)
            CurrentAction[(int) UnitEventType.OnFixedUpdate](CurrentActionArgs);
    }

    #endregion

    #region <Callbacks>
    
    public override void OnIdleRelax()
    {
        if (CurrentAction != null)
            CurrentAction[(int) UnitEventType.OnRelax](CurrentActionArgs);
    }

    protected override void OnDeath()
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
        if (ActionStatusGroup[actionGroup].MaximumStack > 0 && ActionStatusGroup[actionGroup].CurrentStack == 0) return;
        if (_currentActionArgs != null && _currentActionArgs.TransitionRestrictTrigger) return;
        if (CurrentActionGroup != null && CurrentActionGroup != actionGroup) OnTransitionToOtherCast();

        CurrentActionGroup = actionGroup;
        CurrentActionArgs.SetActionTrigger(actionButtonTriggerCaster).SetCaster(this);
        
        ActionTrigger(UnitEventType.SetTrigger);
    }

    public override void OnCastAnimationStandby()
    {
        ActionTrigger(UnitEventType.Standby);
    }

    public override void OnCastAnimationCue()
    {        
        ActionTrigger(UnitEventType.Cue);
    }

    public override void OnCastAnimationCue2()
    {        
        ActionTrigger(UnitEventType.Cue2);
    }
    
    public override void OnCastAnimationCue3()
    {        
        ActionTrigger(UnitEventType.Cue3);
    }
    
    public override void OnCastAnimationCue4()
    {        
        ActionTrigger(UnitEventType.Cue4);
    }

    
    public override void OnCastAnimationExit()
    {                
        ActionTrigger(UnitEventType.Exit);
    }

    public override void OnCastAnimationEnd()
    {        
        ActionTrigger(UnitEventType.End);
    }

    public void OnCastAnimationCleanUp()
    {                
        ActionTrigger(UnitEventType.CleanUp);        
    }

    public void OnTransitionToOtherCast()
    {        
        ActionTrigger(UnitEventType.OnTransitionToOtherCast);
        OnCastAnimationCleanUp();
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
                actionStatus.CurrentStack < actionStatus.MaximumStack)
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
    
    public Action<UnitEventArgs>[] CurrentAction
    {
        get { return _currentActionGroup != null ? 
            _currentActionGroup[CurrentActionStatus.CurrentChain] : null; }
    }

    public List<Action<UnitEventArgs>[]> CurrentActionGroup
    {
        get { return _currentActionGroup; }
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

        if (Filter.IsAlive(this))
        {
//            SoundManager.GetInstance
//                .CastSfx(SoundManager.AudioMixerType.VOICE, ChampionType, K514SfxStorage.ActivityType.Hitted).SetTrigger();
            UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Hit);
            UpdateTension();
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
        if (CurrentAction == null || CurrentAction[(int) unitEventType] == null) return false;        
        
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
            case UnitEventType.Exit:
                break;
            case UnitEventType.End:
                break;
            case UnitEventType.CleanUp:
                ResetFromCast();
                break;
            case UnitEventType.OnHeartBeat:
                break;
            case UnitEventType.OnFixedUpdate:
                break;
            case UnitEventType.OnRelax:
                break;
            case UnitEventType.OnTransitionToOtherCast:
                break;
            case UnitEventType.Count:
            default:
                throw new ArgumentOutOfRangeException("UnitEventType", unitEventType, null);
        }

        return true;
    }   

    #endregion

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
    
    #region <Classes>
    
    public class ActionStatus
    {        
        public CommomActionArgs EventArgs;
        
        private int _maximumCooldown, _currentCooldown;
        private int _maximumStackCooldown, _currentStackCooldown;
        private int _maximumStack, _currentStack;
        private int _maximumChain, _currentChain;

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