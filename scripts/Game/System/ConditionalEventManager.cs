
using System;
using System.Collections.Generic;

public class ConditionalEventManager : Singleton<ConditionalEventManager>
{
    
    /// <summary>
    /// TODO change <(enum?),Action<conditionalEventArgs>> later
    /// </summary>
    private Dictionary<string,Action<ConditionalEventArgs>> _inputActionGroup;

    private ConditionalEventArgs _conditionalEventArgs;
    
    private Action<ConditionalEventArgs> _onNormalAttackAction;
    private Action<ConditionalEventArgs> _onNormalAttackHitAction;
    private Action<ConditionalEventArgs> _onChargeAttackAction;
    private Action<ConditionalEventArgs> _onActiveSkillAction;
    private Action<ConditionalEventArgs> _onDashAction;
    private Action<ConditionalEventArgs> _onKillEnemyAction;
    private Action<ConditionalEventArgs> _onHurtAction;
    private Action<ConditionalEventArgs> _onFiveSecondAction;
    private Action<ConditionalEventArgs> _onHealthPointFullAction;
    private Action<ConditionalEventArgs> _onHealthPointUnderFiftyAction;
    private Action<ConditionalEventArgs> _onActionPointFullAction;

    private int _actionFlag;
    
    public enum ActionFlag
    {
        OnNormalAttack = 1<<0,
        OnNormalAttackHit = 1<<1,
        OnChargeAttack = 1<<2,
        OnActiveSkill = 1<<3,
        OnDash = 1<<4,
        OnKillEnemy = 1<<5,
        OnHurt = 1<<6,
        OnFiveSecond = 1<<7,
        OnHealthPointFull = 1<<8,
        OnHealthPointUnderFifty = 1<<9,
        OnActionPointFull = 1<<10,
        
        StopAll = 1<<30
        
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        _actionFlag = Int32.MaxValue;
        
        _inputActionGroup = new Dictionary<string, Action<ConditionalEventArgs>>();
        _conditionalEventArgs = new ConditionalEventArgs();

    }

    public void SetActionFlag(ActionFlag flag, bool on)
    {
        if (on && ((int) flag & _actionFlag) == 0)
            _actionFlag |= (int) flag;
        else if (!on && ((int) flag & _actionFlag) != 0)
            _actionFlag -= (int) flag;
    }

    public void OnNormalAttackAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnNormalAttack) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onNormalAttackAction?.Invoke(_conditionalEventArgs);
    }
    /// TODO add callback to script
    public void OnNormalAttackHitAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnNormalAttackHit) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onNormalAttackHitAction?.Invoke(_conditionalEventArgs);
    }

    public void OnChargeAttackAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnChargeAttack) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onChargeAttackAction?.Invoke(_conditionalEventArgs);
    }
    /// TODO add callback to script
    public void OnActiveSkillAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnActiveSkill) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onActiveSkillAction?.Invoke(_conditionalEventArgs);
    }
    /// TODO add callback to script
    public void OnDashAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnDash) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onDashAction?.Invoke(_conditionalEventArgs);
    }
    
    public void OnKillEnemyAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnKillEnemy) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onKillEnemyAction?.Invoke(_conditionalEventArgs);
    }
    /// TODO add callback to script
    public void OnHurtAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnHurt) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onHurtAction?.Invoke(_conditionalEventArgs);
    }
    /// TODO add callback to script
    public void OnFiveSecondAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnFiveSecond) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onFiveSecondAction?.Invoke(_conditionalEventArgs);
    }
    /// TODO add callback to script
    public void OnHealthPointFullAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnHealthPointFull) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onHealthPointFullAction?.Invoke(_conditionalEventArgs);
    }
    /// TODO add callback to script
    public void OnHealthPointUnderFiftyAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnHealthPointUnderFifty) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onHealthPointUnderFiftyAction?.Invoke(_conditionalEventArgs);
    }
    /// TODO add callback to script
    public void OnActionPointFullAction()
    {
        if ((_actionFlag & (int) ActionFlag.OnActionPointFull) == 0
            || (_actionFlag & (int) ActionFlag.StopAll) == 0) return;

        _onActionPointFullAction?.Invoke(_conditionalEventArgs);
    }
    
    public void AddConditionalAction(ActionFlag kind, Action<ConditionalEventArgs> action, string actionName, CustomActionArgs args)
    {
        _inputActionGroup.Add(actionName, action);
        _conditionalEventArgs.Args.Add(actionName, args);
        switch (kind)
        {
            case ActionFlag.OnNormalAttack :
                _onNormalAttackAction+=action;
                break;
            case ActionFlag.OnNormalAttackHit:
                _onNormalAttackHitAction+=action;
                break;
            case ActionFlag.OnChargeAttack:
                _onChargeAttackAction+=action;
                break;
            case ActionFlag.OnActiveSkill:
                _onActiveSkillAction+=action;
                break;
            case ActionFlag.OnDash:
                _onDashAction+=action;
                break;
            case ActionFlag.OnKillEnemy:
                _onKillEnemyAction+=action;
                break;
            case ActionFlag.OnHurt:
                _onHurtAction+=action;
                break;
            case ActionFlag.OnFiveSecond:
                _onFiveSecondAction+=action;
                break;
            case ActionFlag.OnHealthPointFull:
                _onHealthPointFullAction+=action;
                break;
            case ActionFlag.OnHealthPointUnderFifty:
                _onHealthPointUnderFiftyAction+=action;
                break;
            case ActionFlag.OnActionPointFull:
                _onActionPointFullAction+=action;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, " Action Name : " + actionName);
        }
    }
    
    public void RemoveConditionalAction(ActionFlag kind, string actionName)
    {
        var action = _inputActionGroup[actionName];
        _inputActionGroup.Remove(actionName);
        _conditionalEventArgs.Args.Remove(actionName);
        switch (kind)
        {
            case ActionFlag.OnNormalAttack :
                _onNormalAttackAction -= action;
                break;
            case ActionFlag.OnNormalAttackHit:
                _onNormalAttackHitAction -= action;
                break;
            case ActionFlag.OnChargeAttack:
                _onChargeAttackAction -= action;
                break;
            case ActionFlag.OnActiveSkill:
                _onActiveSkillAction -= action;
                break;
            case ActionFlag.OnDash:
                _onDashAction -= action;
                break;
            case ActionFlag.OnKillEnemy:
                _onKillEnemyAction -= action;
                break;
            case ActionFlag.OnHurt:
                _onHurtAction -= action;
                break;
            case ActionFlag.OnFiveSecond:
                _onFiveSecondAction -= action;
                break;
            case ActionFlag.OnHealthPointFull:
                _onHealthPointFullAction -= action;
                break;
            case ActionFlag.OnHealthPointUnderFifty:
                _onHealthPointUnderFiftyAction -= action;
                break;
            case ActionFlag.OnActionPointFull:
                _onActionPointFullAction -= action;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, " Action Name : " + actionName);
        }
    }
}