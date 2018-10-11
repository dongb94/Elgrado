using System;
using System.Collections.Generic;

public class CrowdControl
{
    #region <Fields>
    
    public string Description { get; private set; }
    public bool IsPositive { get; private set; }
    public int Lifespan { get; private set; }
    public CrowdControlType Type { get; private set; }
    public Unit Owner { get; private set; }
    private readonly Action<CrowdControlArgs>[] _crowdControlActionGroup;
    private readonly CrowdControlArgs[] _actionArgsGroup;
    private readonly bool[] _optionGroup;
    
    #endregion </Fields>

    #region <Enums>
    
    public enum CrowdControlType
    {
        Stun,
        AcceleratorForCast,
        AcceleratorForMoveSpeed,
        
        Count
    }

    public enum EventType
    {
        OnBirth,
        OnHeartBeat,
        OnTerminate,

        OnFixedUpdate,
        
        Count        
    }

    public enum Option
    {
        OverrideAbsolute,
        OverrideRace,
        OverrideAccumulate,
    
        Count
    }
    
    #endregion </Enums>

    #region <Constructor>
    
    public CrowdControl(Unit owner, CrowdControlType type, string description, int lifespan, bool isPositive)
    {
        Owner = owner;
        Description = description;
        IsPositive = isPositive;
        Lifespan = lifespan;
        Type = type;
        _optionGroup = new bool[(int) Option.Count];
        _crowdControlActionGroup = new Action<CrowdControlArgs>[(int) EventType.Count];
        _actionArgsGroup = new CrowdControlArgs[(int) EventType.Count];
    }
    
    #endregion </Constructor>
    
    #region <Callbacks>

    public void OnBirth()
    {
        if (UnitFilter.Check(Owner, UnitFilter.Condition.IsDead)) return;       
        
        if (_optionGroup[(int) Option.OverrideAbsolute]
            || _optionGroup[(int) Option.OverrideRace]
            || _optionGroup[(int) Option.OverrideAccumulate])
        {
            var filter = new List<CrowdControl>();
            
            foreach (var crowdControl in Owner.CrowdControlGroup)
            {
                if (crowdControl.Type == Type)
                {
                    filter.Add(crowdControl);
                }
            }

            if (_optionGroup[(int) Option.OverrideAbsolute])
            {
                filter.ForEach(filteredCrowdControl => filteredCrowdControl.OnTerminate());
            }
            else if (_optionGroup[(int) Option.OverrideRace])
            {
                foreach (var filteredCrowdControl in filter)
                {
                    if (filteredCrowdControl.Lifespan > Lifespan) return;
                    
                    Owner.CrowdControlGroup.Remove(filteredCrowdControl);
                }               
            }
            else if (_optionGroup[(int) Option.OverrideAccumulate])
            {
                foreach (var filteredCrowdControl in filter)
                {
                    Lifespan += filteredCrowdControl.Lifespan;                    
                    filteredCrowdControl.OnTerminate();
                }                                
            }
        }
        
        Owner.CrowdControlGroup.Add(this);
        
        var action = _crowdControlActionGroup[(int) EventType.OnBirth];

        if (action == null) return;
        
        action(_actionArgsGroup[(int) EventType.OnBirth]);
    }

    ///Called by
    ///<exception cref = "Unit.FixedUpdate">
    public void FixedUpdate()
    {
        var action = _crowdControlActionGroup[(int)EventType.OnFixedUpdate];

        if (action == null) return;

        action(_actionArgsGroup[(int)EventType.OnFixedUpdate]);
    }

    public void OnHeartBeat()
    {
        var action = _crowdControlActionGroup[(int) EventType.OnHeartBeat];

        if (Lifespan <= 0)
        {
            OnTerminate();
            return;
        }

        --Lifespan;
        
        if (action == null) return;
        
        action(_actionArgsGroup[(int) EventType.OnHeartBeat]);                
    }

    public void OnTerminate()
    {
        Owner.CrowdControlGroup.Remove(this);       
        
        var action = _crowdControlActionGroup[(int) EventType.OnTerminate];
        
        if (action == null) return;
        
        action(_actionArgsGroup[(int) EventType.OnTerminate]);
    }
    
    #endregion </Callbacks>
    
    #region <Setters/Methods>

    public CrowdControl SetAction(EventType eventType, CrowdControlArgs actionArgs,
        Action<CrowdControlArgs> action)
    {
        _crowdControlActionGroup[(int) eventType] += action;
        _actionArgsGroup[(int) eventType] = actionArgs;
        
        return this;
    }

    public CrowdControl SetOption(Option option, bool value)
    {
        _optionGroup[(int) option] = value;

        return this;
    }
    
    #endregion </Setters/Methods>
    
}