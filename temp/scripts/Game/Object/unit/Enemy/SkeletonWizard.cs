using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SkeletonWizard : BossMonster
{
    #region <Enums>
    
    public enum SpellMotionName
    {
        ShortEmission, ScytheSwing, LeftHandSwing, LongEmission, Jumonji, GrapAndThrowing, LargeEmission, DiveAndHunt, Avoision, Kamae, HeadDown
    }

    public enum SpellPatternName
    {
        ChillingSlash, End
    }
    
    #endregion </Enums>    
    
    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();
        hyperParameterSet[(int) HyperParameterOfSpell.Spell01] = new SpellHyperParameter
        {
            CoolDown = 3, Damage = 1, Range = 2.0f, ColliderHalfExtends = Vector3.one * 0.2f,
            Motion = (int) SpellMotionName.ScytheSwing
        };

        //phases
        var phase1 = new Phase(100);
        var phase2 = new Phase(80);
        
        //patterns
        var slash = new Pattern(1);
        slash.actionQueue.AddLast(ChillingSlash());
        slash.actionQueue.AddLast(ChillingSlash());
        slash.actionQueue.AddLast(ChillingSlash());
        slash.patternCooldown = 1;
        
        var doubleSlash = new Pattern(1);
        doubleSlash.actionQueue.AddLast(ChillingSlash());
        doubleSlash.actionQueue.AddLast(ChillingSlash());
        
        phase1.PatternGroup.Add(slash);
        phase2.PatternGroup.Add(doubleSlash);
        
        AddPhase(phase1);
        AddPhase(phase2);
    }

    #endregion </Unity/Callbacks>    
    
    #region <Methods/Spell_Initialize>

    #region <ChillingSlash>
        private Action<UnitEventArgs>[] ChillingSlash()
        {
            #region <SweepShot/Init>
                var eventGroup = new Action<UnitEventArgs>[(int) UnitEventType.Count];
            #endregion

            #region <SweepShot/Begin>

            eventGroup[(int) UnitEventType.Begin] = eventArgs =>
            {
                var caster = (Computer)eventArgs.Caster;
                var focus = caster.Focus;
                caster.ChaseOrRush(focus, .0f, .0f);
                caster.UnitBoneAnimator.SetCast("Spell", caster.hyperParameterSet[(int)HyperParameterOfSpell.Spell01].Motion);
            };
            
            #endregion
            
            #region <SweepShot/Cue>
                eventGroup[(int) UnitEventType.Cue] = eventArgs =>
                {
                };
            #endregion
            
            #region <SweepShot/Terminate>
                eventGroup[(int) UnitEventType.End] = other =>
                {
                    var caster = (Computer)other.Caster;          
                    caster.ResetFromCast();
                    caster.CurrentAttackCooldown = caster.AttackCooldownLeft =
                        caster.hyperParameterSet[(int) HyperParameterOfSpell.Spell01].CoolDown;
                };
            #endregion
            
            return eventGroup;
        }
    #endregion </ChillingSlash>

    #endregion
}