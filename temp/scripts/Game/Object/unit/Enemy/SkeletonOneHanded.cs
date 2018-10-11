using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SkeletonOneHanded : Enemy
{
    #region <Enums>
    
    public enum SpellMotionName
    {
        Normal01, Normal02, Normal03, Normal04, Normal05, Normal06, Normal07
    }

    #endregion </Enums>    
    
    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();
        hyperParameterSet[(int) HyperParameterOfSpell.Spell01] = new SpellHyperParameter { CoolDown = 3, Damage = 1 , Range = 2.0f , ColliderHalfExtends = Vector3.one * 0.2f , Motion = (int)SpellMotionName.Normal01 };

        
        Pattern pattern1 = new Pattern(1);
        pattern1.actionQueue.AddLast(ChillingSlash());
        pattern1.actionQueue.AddLast(ChillingSlash());
        pattern1.frequency = 20;
        pattern1.patternCooldown = 2;

        PatternGroup.Add(pattern1);
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
                caster.ChaseOrRush(caster.Focus, .0f, .0f);
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
    #endregion

    #endregion
}