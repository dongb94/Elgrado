using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class BossMonster : Enemy
{

    private Phase _currentPhase;
    private List<Phase> _phaseGroup;
    protected Action<UnitEventArgs>[] CooltimeSetActionGroup;
    private int _coolDown;

    #region <Unity/Callback>

    protected override void Awake()
    {
        base.Awake();
        _phaseGroup = new List<Phase>();
        CooltimeSetActionGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];
        _coolDown = 0;
    }

    #endregion </Unity/Callback>

    #region <CustomEvent>

    public override void OnHeartBeat()
    {
        base.OnHeartBeat();
        
        if (ReservationActionList.Count == 0)
            _coolDown = Math.Max(0, _coolDown - 1);
    }
    
    #endregion

    #region <Callbacks>
 
    public override void OnHealthPointAdjust()
    {
        base.OnHealthPointAdjust();       
        _currentPhase = GetCurrentPhase;
        if (_currentPhase == null) return;
        
        PatternGroup = _currentPhase.PatternGroup;
    }

    #endregion </Callbacks>

    #region <Properties>

    public Phase CurrentPhase
    {
        get { return _currentPhase; }
    }
        
    private int HundredFoldedCurrentHealthPoint { get { return CurrentHealthPoint * 100; } }
    
    private Phase GetCurrentPhase
    {
        get
        {
            if (_phaseGroup == null) return null;
            
            return _phaseGroup.Where(phaseCandidate =>
                    phaseCandidate.TriggerHealthPointRate * MaximumHealthPoint >= HundredFoldedCurrentHealthPoint)
                .OrderBy(satisfiedCandidate => satisfiedCandidate.TriggerHealthPointRate).First();;
        }
    }
    

    #endregion </Properties>

    #region <Methods>
    
    protected void AddPhase(Phase patterns)
    {
        _phaseGroup.Add(patterns);
    }

    public override void TryPushNextPattern()
    {
        if (_coolDown == 0 || ReservationActionList.Count != 0)
        {
            PushNextPattern();
        }
    }
    
    // cooldown when reservation list size is 0
    public Action<UnitEventArgs>[] SetCooldown(int cooldown=0)
    {
        CooltimeSetActionGroup[(int) UnitEventType.Begin] = (args) =>
        {
            _coolDown = Math.Max(0,cooldown);
        };
        return CooltimeSetActionGroup;
    }
    
    #endregion </Methods>
    
    #region <Structs>

    public class Phase
    {
        public int TriggerHealthPointRate { get; private set; }
        public List<Pattern> PatternGroup { get; private set; }

        public Phase(int triggerHpRate, List<Pattern> pattern = null)
        {
            Assert.IsTrue(triggerHpRate <= 100 && triggerHpRate > 0);
            
            TriggerHealthPointRate = triggerHpRate;
            PatternGroup = pattern ?? new List<Pattern>();
        }
    }
    
    #endregion </Structs>

}
