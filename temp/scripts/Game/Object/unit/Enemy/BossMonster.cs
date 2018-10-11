using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class BossMonster : Enemy
{

    private Phase _currentPhase;
    private List<Phase> _phaseGroup;

    #region <Unity/Callback>

    protected override void Awake()
    {
        base.Awake();
        _phaseGroup = new List<Phase>();
    }

    #endregion </Unity/Callback>
    
    #region <Callbacks>
 
    public override void OnHealthPointAdjust()
    {
        base.OnHealthPointAdjust();       
        _currentPhase = GetCurrentPhase;
        if (_currentPhase == null) return;
        
        PatternGroup = _currentPhase.PatternGroup;
        Debug.Log(PatternGroup[0].actionQueue.Count);
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

    protected void AddPhase(Phase patterns)
    {
        _phaseGroup.Add(patterns);
    }

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
