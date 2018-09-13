using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;

public abstract class Champion : Unit
{

    #region <Fields>

    public int MaxSp;
    [NonSerialized] public int Sp;    
    public Action<CustomEventArgs.CommomActionArgs>[] TriggerActionEvent;
    public Action<CustomEventArgs.CommomActionArgs>[][] NormalActionsEventGroup { get; protected set; }
    public Action<CustomEventArgs.CommomActionArgs>[] PrimaryActionEventGroup { get; protected set; }
    public Action<CustomEventArgs.CommomActionArgs>[] SecondaryActionEventGroup { get; protected set; }
    [NonSerialized]public int NormalActionSequence;
    public bool HasLockedTransition { get; protected set; }
    
    /*extend*/
    [NonSerialized] public float FillAmount;
    [NonSerialized] public K514SfxStorage.ChampionType ChampionType;

    private CustomEventArgs.CommomActionArgs CurrentEventState;

    #endregion

    #region <Enums>

    protected enum EventInfo
    {
        CancelCheck,
        Clicked,
        Birth,
        Enter,
        Collide,
        Exit,
        Terminate,
        Released,

        Count
    }

    #endregion

    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();

        Controller = GetComponent<CharacterController>();
        Sp = MaxSp;
    }

    protected void OnEnable()
    {
        HasLockedTransition = false;
        NotMoveTrigger = NotRotateTrigger = NotReleaseTension = 0;
        MaterialApplier.RevertTrigger();
        if (!HUDManager.GetInstance.JoystickController.IsEventHoldOn)
            RunningTime = .0f;
    }

    #endregion

    #region <Callbacks>

    public override void OnIdleRelax()
    {
        NormalActionSequence = 0;
    }

    protected override void OnDeath()
    {
        Hp = MaxHp;
        SoundManager.GetInstance
            .CastSfx(SoundManager.AudioMixerType.VOICE, ChampionType, K514SfxStorage.ActivityType.Dead).SetTrigger();
    }

    #region <TriggerEvent>
    public virtual void OnTriggerClicked()
    {
        if(TriggerActionEvent[(int) EventInfo.Clicked] != null) TriggerActionEvent[(int) EventInfo.Clicked](new CustomEventArgs.CommomActionArgs().SetCaster(this));
    }
    
    public virtual void OnTriggerReleased()
    {
        if(TriggerActionEvent[(int) EventInfo.Released] != null) TriggerActionEvent[(int) EventInfo.Released](new CustomEventArgs.CommomActionArgs().SetCaster(this));
    }
        
    public virtual void OnCancelChackInvoked()
    {
        if(TriggerActionEvent[(int) EventInfo.CancelCheck] != null) TriggerActionEvent[(int) EventInfo.CancelCheck](new CustomEventArgs.CommomActionArgs().SetCaster(this));
    }
    
    #endregion

    #region <AnimationEvent>
    public override void OnExitedCastAnimation()
    {
        if(TriggerActionEvent[(int) EventInfo.Terminate] != null) TriggerActionEvent[(int) EventInfo.Terminate](CurrentEventState);
    }

    public override void OnEnteredEffectPeriod(int triggerId)
    {
        if(TriggerActionEvent[(int) EventInfo.Enter] != null) TriggerActionEvent[(int) EventInfo.Enter](CurrentEventState);
    }

    public override void OnExitedEffectPeriod(int triggerId)
    {
        if(TriggerActionEvent[(int) EventInfo.Exit] != null) TriggerActionEvent[(int) EventInfo.Exit](CurrentEventState);
    }
    #endregion    
    
    #region <ColliderEvent>
    public override void OnObjectTriggerEnterUnit(Unit collidedUnit)
    {
        if(TriggerActionEvent[(int) EventInfo.Collide] != null) TriggerActionEvent[(int) EventInfo.Collide](new CustomEventArgs.CommomActionArgs().SetCaster(this).SetCandidate(collidedUnit));
    }
    
    public override void OnObjectTriggerExitUnit(Unit collidedUnit){}
    #endregion

    #endregion

    #region <Methods>
    
    public override void Move(Vector3 forceVector)
    {
        if (  UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Hit
            ||  UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Cast  || NotMoveTrigger > 0)
        {
            RunningTime = UpdateRunningTime * 0.1f;
            return;
        }

        forceVector.z = forceVector.y;
        forceVector.y = 0;
        base.Move(forceVector);
    }
    
    public override void Hurt(Unit caster, int damage, TextureType type, Vector3 forceDirection, 
        Action<Unit, Unit, Vector3> action = null, bool isCancelCast = true)
    {        
        base.Hurt(caster, damage, type, forceDirection, action, isCancelCast);

        if (Filter.IsAlive(this))
        {
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, ChampionType, K514SfxStorage.ActivityType.Hitted).SetTrigger();
            UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Hit);
            UpdateTension();
        }

        HUDManager.GetInstance.UpdateChampionScout();
        CameraManager.GetInstance.SetVibrateFx(damage, 0.15f);
    }

    public void CleanUp()
    {
        ForceVector = Vector3.zero;                
        RunningTime = .0f;
    }

    public Enemy DetectAndChaseEnemyInRange(float radius, float chaseRate, float rushRate)
    {
        var focusEnemy = DetectEnemyInRange(radius);

        if (focusEnemy != null)
        {
            SetAngleToDestination(GetNormDirectionToMove(focusEnemy));
            _Transform.eulerAngles = Vector3.up * AngleToDestination;
            AddForce(GetNormDirectionToMove(focusEnemy) * focusEnemy.DistanceTowardPlayer * chaseRate);
            
            return focusEnemy;
        }
        
        if (rushRate > Mathf.Epsilon)
        {            
            AddForce(_Transform.TransformDirection(Vector3.forward * rushRate));
        }

        return focusEnemy;
    }
    
    public void ResetFromCast()
    {
        if (RunningTime > Mathf.Epsilon)
            UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Move);
        else        
            UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Idle);
            
    }

    public void NormalAction(float pFillAmount = 0f, bool isTriggerEvent = false)
    {
        TriggerActionEvent = NormalActionsEventGroup[NormalActionSequence];
        if (TriggerActionEvent[(int) EventInfo.Birth] != null && !isTriggerEvent)
        {
            FillAmount = pFillAmount;
            TriggerActionEvent[(int) EventInfo.Birth](
                CurrentEventState = new CustomEventArgs.CommomActionArgs()
                    .SetCaster(this)
                    .SetActionTrigger(HUDManager.GetInstance.
                        ActionTriggerGroup[(int) HUDManager.ActionTriggerType.NormalAction]));
        }
    }

    public void PrimaryAction(float pFillAmount = 0f, bool isTriggerEvent = false)
    {
        TriggerActionEvent = PrimaryActionEventGroup;
        if (TriggerActionEvent[(int) EventInfo.Birth] != null && !isTriggerEvent)
        {
            FillAmount = pFillAmount;
            TriggerActionEvent[(int) EventInfo.Birth](
                CurrentEventState = new CustomEventArgs.CommomActionArgs()
                    .SetCaster(this)
                    .SetActionTrigger(HUDManager.GetInstance.
                        ActionTriggerGroup[(int) HUDManager.ActionTriggerType.LeftAction]));
        }
    }

    public void SecondaryAction(float pFillAmount = 0f, bool isTriggerEvent = false)
    {
        TriggerActionEvent = SecondaryActionEventGroup;
        if (TriggerActionEvent[(int) EventInfo.Birth] != null && !isTriggerEvent)
        {
            FillAmount = pFillAmount;
            TriggerActionEvent[(int) EventInfo.Birth](
                CurrentEventState = new CustomEventArgs.CommomActionArgs()
                    .SetCaster(this)
                    .SetActionTrigger(HUDManager.GetInstance.
                        ActionTriggerGroup[(int) HUDManager.ActionTriggerType.RightAction]));
        }
    }
    
    // transparent deprecated
    protected override void UnitMove(Vector3 forceVector)
    {
        base.UnitMove(forceVector);
        //TransparentManager.GetInstance.Trigger();
    }

    public virtual void ProcessNormalAttackSequence()
    {
        NormalActionSequence = (NormalActionSequence + 1) % NormalActionsEventGroup.Length;
    }
    
    protected virtual Enemy DetectEnemyInRange(float radius)
    {        
        var focusEnemyGroup = Filter.GetEnemyGroupInRadius(radius).ToArray();
        return focusEnemyGroup.Length > 0 ? focusEnemyGroup.OrderBy(enemy => enemy.DistanceTowardPlayer).ElementAt(0) : null;
    }

    public void SetCurrentEventState(CustomEventArgs.CommomActionArgs State)
    {
        CurrentEventState = State;
    }


    #endregion
    
}