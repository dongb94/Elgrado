using System;
using System.Collections.Generic;
using System.Linq;
using DungeonArchitect;
using UnityEngine;
using Random = UnityEngine.Random;

public class SkeletonWizard : BossMonster
{
    #region <Fields>

    private Projectile boneWall;
    
    #endregion </Fields>
    
    #region <Enums>
    
    public enum SpellMotionName
    {
        ShortEmission, ScytheSwing, LeftHandSwing, LongEmission, Jumonji, GrapAndThrowing, LargeEmission, BirthAction, Die, Ready, Stun
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

        #region <hyperParameterSet>
        hyperParameterSet[(int) HyperParameterOfSpell.Spell01] = new SpellHyperParameter
        {
            Damage = 1, Range = 2.0f, ColliderHalfExtends = Vector3.one * 0.2f,
            Motion = (int) SpellMotionName.ScytheSwing
        }; // Spell 1 ChillingSlash
        hyperParameterSet[(int) HyperParameterOfSpell.Spell02] = new SpellHyperParameter
        {
            Damage = 2, Range = 3.5f, ColliderHalfExtends = Vector3.one * 1.2f,
            Motion = (int) SpellMotionName.ShortEmission
        }; // Spell 2 BloodCutter
        hyperParameterSet[(int) HyperParameterOfSpell.Spell03] = new SpellHyperParameter
        {
            Damage = 3, Range = 8.0f, ColliderHalfExtends = Vector3.one * 1.2f,
            Motion = (int) SpellMotionName.BirthAction
        }; // Spell 3 ShadowDive
        hyperParameterSet[(int) HyperParameterOfSpell.Spell04] = new SpellHyperParameter
        {
            Range = 8.0f, ColliderHalfExtends = Vector3.one * 4f,
            Motion = (int) SpellMotionName.Jumonji
        }; // Spell 4 BoneWall
        hyperParameterSet[(int) HyperParameterOfSpell.Spell05] = new SpellHyperParameter
        {
            Damage = 3, Range = 8.0f, ColliderHalfExtends = Vector3.one * 2.2f,
            Motion = (int) SpellMotionName.LeftHandSwing
        }; // Spell 5 BoneMissile
        hyperParameterSet[(int) HyperParameterOfSpell.Spell06] = new SpellHyperParameter
        {
            Damage = 5, Range = 8.0f, 
            Motion = (int) SpellMotionName.LargeEmission
        }; // Spell 6 BoneExplosion
        #endregion </hyperParameterSet>

        //phases
        var phase1 = new Phase(100);
        var phase2 = new Phase(80);
        
        //patterns
        var slash = new Pattern(1);
        slash.ActionQueue.AddLast(ChillingSlash());
        slash.ActionQueue.AddLast(SetDelay());
        slash.ActionQueue.AddLast(ChillingSlash());
        slash.ActionQueue.AddLast(SetCooldown(1));
        
        var cutter = new Pattern(5);
        cutter.ActionQueue.AddLast(BloodCutter());
        cutter.ActionQueue.AddLast(SetDelay());
        cutter.ActionQueue.AddLast(BloodCutter());
        cutter.ActionQueue.AddLast(SetDelay());
        cutter.ActionQueue.AddLast(BloodCutter());
        cutter.ActionQueue.AddLast(SetCooldown(1));
        cutter.SetPatternCooldown(5);
        
        var dive = new Pattern(10);
        dive.ActionQueue.AddLast(ShadowDive());
        dive.ActionQueue.AddLast(SetCooldown(1));
        dive.SetPatternCooldown(5);
        
        var doubleSlash = new Pattern(1);
        doubleSlash.ActionQueue.AddLast(ChillingSlash());
        doubleSlash.ActionQueue.AddLast(SetDelay());
        doubleSlash.ActionQueue.AddLast(ChillingSlash());
        doubleSlash.ActionQueue.AddLast(SetDelay());
        
        var boneWall = new Pattern(1);
        boneWall.ActionQueue.AddLast(BoneWall());
        boneWall.ActionQueue.AddLast(SetCooldown(10));
        boneWall.SetPatternCooldown(10);
        
        var boneMissile = new Pattern(1);
        boneMissile.ActionQueue.AddLast(BoneMissile());
        boneMissile.ActionQueue.AddLast(SetDelay(1));
        boneMissile.ActionQueue.AddLast(BoneMissile());
        boneMissile.ActionQueue.AddLast(SetDelay(1));
        boneMissile.ActionQueue.AddLast(BoneMissile());
        boneMissile.ActionQueue.AddLast(SetCooldown(3));
        
        var boneExplosion = new Pattern(1);
        boneExplosion.ActionQueue.AddLast(BoneExplosion());
        boneExplosion.ActionQueue.AddLast(SetCooldown(1));
        
        phase1.PatternGroup.Add(slash);
        phase1.PatternGroup.Add(cutter);
        phase1.PatternGroup.Add(dive);
        phase1.PatternGroup.Add(boneWall);
        phase1.PatternGroup.Add(boneExplosion);
        //phase1.PatternGroup.Add(boneMissile);
        phase2.PatternGroup.Add(doubleSlash);
        
        AddPhase(phase1);
        AddPhase(phase2);
    }

    #endregion </Unity/Callbacks>    
    
    #region <Methods/Spell_Initialize>
    //spell 1 ChillingSlash
    #region <ChillingSlash>
        private Action<UnitEventArgs>[] ChillingSlash()
        {
            #region <ChillingSlash/Init>
                var eventGroup = new Action<UnitEventArgs>[(int) UnitEventType.Count];
            #endregion

            #region <ChillingSlash/Begin>

            eventGroup[(int) UnitEventType.Begin] = eventArgs =>
            {
                var caster = (Computer)eventArgs.Caster;
                var focus = caster.Focus;
                caster.ChaseOrRush(focus, .2f, 2.0f);
                caster.UnitBoneAnimator.SetCast("Spell", caster.hyperParameterSet[(int)HyperParameterOfSpell.Spell01].Motion);
            };
            
            #endregion
            
            #region <ChillingSlash/Exit>
                eventGroup[(int) UnitEventType.Exit] = eventArgs =>
                {
                    var caster = (Computer)eventArgs.Caster;
                    var list = new PreProcessedMonoBehaviour[64].ToList();
                    UnitFilter.GetUnitAtLocation(caster.GetPosition, 5f, caster, UnitFilter.Condition.IsNegative, list);
                    var numberOfUnit = UnitFilter.GetUnitInCircularSector(caster, 90f, FilteredObjectGroup, list);
                    
                    for(var i=0 ; i<numberOfUnit; i++) 
                    {
                        var target = (Unit)FilteredObjectGroup[i];
                        if(UnitFilter.Check(caster,target, UnitFilter.Condition.IsOrCondition | UnitFilter.Condition.IsDead 
                                                                                              | UnitFilter.Condition.IsInvincible)) continue;
                        if(target != null)
                            target.Hurt(caster, hyperParameterSet[(int)HyperParameterOfSpell.Spell01].Damage, TextureType.Magic);
                    }
                };
            #endregion
            
            #region <ChillingSlash/Terminate>
                eventGroup[(int) UnitEventType.End] = other =>
                {
                    var caster = (Computer)other.Caster;          
                    caster.ResetFromCast();
                };
            #endregion
            
            return eventGroup;
        }
    #endregion </ChillingSlash>
    //spell 2 BloodCutter
    #region <BloodCutter>
    private Action<UnitEventArgs>[] BloodCutter()
    {
        #region <BloodCutter/Init>
        var eventGroup = new Action<UnitEventArgs>[(int) UnitEventType.Count];
        #endregion

        #region <BloodCutter/Begin>

        eventGroup[(int) UnitEventType.Begin] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            var focus = caster.Focus;
            caster.ChaseOrRush(focus, .0f, .0f);
            caster.UnitBoneAnimator.SetCast("Spell", caster.hyperParameterSet[(int)HyperParameterOfSpell.Spell02].Motion);
        };
            
        #endregion
            
        #region <BloodCutter/Cue>
        eventGroup[(int) UnitEventType.Cue] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            var focus = caster.Focus;
            eventArgs.SetCastPosition(focus.Transform.position);
            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Mark, focus.Transform.position)
                .SetLifeSpan(.5f)
                .SetScale(1f)
                .SetTrigger();
        };
        #endregion

        #region <BloodCutter/Exit>
        eventGroup[(int)UnitEventType.Exit] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            UnitFilter.GetUnitAtLocation(eventArgs.CastPosition, 3.5f, caster, UnitFilter.Condition.IsNegative, FilteredObjectGroup);

            VfxManager.GetInstance.CreateVfx(VfxManager.Type.BloodCutter, eventArgs.CastPosition)
                .SetLifeSpan(1f)
                .SetScale(1f)
                .SetTrigger();
            foreach(var unit in FilteredObjectGroup)
            {
                var target = (Unit)unit;
                if(UnitFilter.Check(caster,target, UnitFilter.Condition.IsOrCondition | UnitFilter.Condition.IsDead 
                                                                                      | UnitFilter.Condition.IsInvincible)) continue;
                if(target != null)
                    target.Hurt(caster, hyperParameterSet[(int)HyperParameterOfSpell.Spell02].Damage, TextureType.Magic);
            }
        };
        #endregion

        #region <BloodCutter/Terminate>
        eventGroup[(int) UnitEventType.End] = other =>
        {
            var caster = (Computer)other.Caster;          
            caster.ResetFromCast();
        };
        #endregion
            
        return eventGroup;
    }
    #endregion </BloodCutter>
    //spell 3 ShadowDive
    #region <ShadowDive>
    private Action<UnitEventArgs>[] ShadowDive()
    {
        #region <ShadowDive/Init>
        var eventGroup = new Action<UnitEventArgs>[(int) UnitEventType.Count];
        #endregion

        #region <ShadowDive/Begin>

        eventGroup[(int) UnitEventType.Begin] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            var focus = caster.Focus;
            caster.UnitBoneAnimator.SetCast("Spell", caster.hyperParameterSet[(int)HyperParameterOfSpell.Spell03].Motion);
        };
            
        #endregion
            
        #region <ShadowDive/Standby>
        eventGroup[(int) UnitEventType.Standby] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            var focus = caster.Focus;
            caster.ChaseOrRush(focus, 0.5f, .0f);
            
        };
        #endregion

        #region <ShadowDive/Cue>
        eventGroup[(int)UnitEventType.Cue] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            UnitFilter.GetUnitAtLocation(caster.GetPosition, 8.0f, caster, UnitFilter.Condition.IsNegative,
                FilteredObjectGroup);

            VfxManager.GetInstance.CreateVfx(VfxManager.Type.BloodCutter, caster.GetPosition)
                .SetLifeSpan(1f)
                .SetScale(2f)
                .SetTrigger();
            foreach(var unit in FilteredObjectGroup)
            {
                var target = (Unit)unit;
                if(UnitFilter.Check(caster,target, UnitFilter.Condition.IsOrCondition | UnitFilter.Condition.IsDead | UnitFilter.Condition.IsInvincible)) continue;
                if(target != null)
                    target.Hurt(caster, hyperParameterSet[(int)HyperParameterOfSpell.Spell03].Damage, TextureType.Magic);
            }
        };
        #endregion

        #region <ShadowDive/Terminate>
        eventGroup[(int) UnitEventType.End] = other =>
        {
            var caster = (Computer)other.Caster;          
            caster.ResetFromCast();
        };
        #endregion
            
        return eventGroup;
    }
    #endregion </ShadowDive>
    //spell 4 BoneWall
    #region <BoneWall>
    private Action<UnitEventArgs>[] BoneWall()
    {
        #region <BoneWall/Init>
        var eventGroup = new Action<UnitEventArgs>[(int) UnitEventType.Count];
        #endregion

        #region <BoneWall/Begin>

        eventGroup[(int) UnitEventType.Begin] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            var focus = caster.Focus;
            caster.UnitBoneAnimator.SetCast("Spell", caster.hyperParameterSet[(int)HyperParameterOfSpell.Spell04].Motion);
        };
            
        #endregion
            
        #region <BoneWall/Cue>
        eventGroup[(int)UnitEventType.Cue] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            var rotate = Vector3.up * 8f;
            
            boneWall = ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.BoneWall, caster, 10.0f, 
                    caster.GetPosition + Vector3.up)
                .SetVelocity(Vector3.zero)
                .SetDirection()
                .SetFixedUpdateAction(args =>
                {
                    ///TODO position don't change error
                    var proj = (Projectile) args.MorphObject;
                    //proj.transform.position = caster.transform.position;

                    var rotateMulti = Vector3.Lerp(rotate, rotate , Time.deltaTime);

                    proj.Transform.rotation *= Quaternion.Euler(rotateMulti);
                })
                .SetTrigger(true);
                    
            boneWall.Transform.SetParent(caster.Transform);
            
            caster.AddCrowdControl(Buff.CreateBuff(caster, Buff._ParentType.Invincible, Buff._Type.BoneWall, 5, true)
                .SetAction(Buff.EventType.OnBirth, buffArgs =>
                {
                    caster.SetInvincible(true);
                })
//                .SetAction(Buff.EventType.OnFixedUpdate, buffArgs =>
//                    {
//                        var colliderGroup = new Collider[20];
//                        Physics.OverlapSphereNonAlloc(caster.GetPosition, 3.0f, colliderGroup, 1<<12);
//                        if(!colliderGroup.Any()) return;
//                        foreach (var col in colliderGroup)
//                        {
//                            var proj = col.GetComponent<Projectile>();
//                            proj.SetCaster(caster);
//                            proj.Remove();
//                        }
//                    })
                .SetAction(Buff.EventType.OnTerminate, buffArgs =>
                {
                    caster.SetInvincible(false);
                    boneWall.Remove();
                })
            );
        };
        #endregion

        #region <BoneWall/Terminate>
        eventGroup[(int) UnitEventType.End] = other =>
        {
            var caster = (Computer)other.Caster;          
            caster.ResetFromCast();
        };
        #endregion
            
        return eventGroup;
    }
    #endregion </BoneWall>
    //spell 5 BoneMissile
    #region <BoneMissile>
    private Action<UnitEventArgs>[] BoneMissile()
    {
        #region <BoneMissile/Init>
        var eventGroup = new Action<UnitEventArgs>[(int) UnitEventType.Count];
        #endregion

        #region <BoneMissile/Begin>

        eventGroup[(int) UnitEventType.Begin] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            var focus = caster.Focus;
            caster.UnitBoneAnimator.SetCast("Spell", caster.hyperParameterSet[(int)HyperParameterOfSpell.Spell05].Motion);
        };
            
        #endregion
            
        #region <BoneMissile/Cue>
        eventGroup[(int)UnitEventType.Cue] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            var direction = (caster.Focus.GetPosition-caster.GetPosition).normalized;
            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.BoneMissile, caster, 2.0f, caster.GetPosition+Vector3.up*1)
                .SetVelocity(caster.Transform.forward * 30f)
                .SetDirection()
                .SetMaxColliderNumber(99)
                .SetProjectileType(Projectile.ProjectileType.Box)
                .SetColliderBox(hyperParameterSet[(int) HyperParameterOfSpell.Spell05].ColliderHalfExtends)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, hyperParameterSet[(int) HyperParameterOfSpell.Spell05].Damage, TextureType.Medium,
                            (bdd, target, forceVector) =>
                            {
                                VfxManager.GetInstance.CreateVfx(VfxManager.Type.Explode, target.GetPosition)
                                    .SetLifeSpan(1f)
                                    .SetScale(2f)
                                    .SetTrigger();
                                target.AddForce(forceVector * 10.0f);
                            });
                    }
                })
                .SetTrigger(true);
        };
        #endregion

        #region <BoneMissile/Terminate>
        eventGroup[(int) UnitEventType.End] = other =>
        {
            var caster = (Computer)other.Caster;          
            caster.ResetFromCast();
        };
        #endregion
            
        return eventGroup;
    }
    #endregion </BoneMissile> 
    //spell 6 BoneExplosion
    #region <BoneExplosion>
    private Action<UnitEventArgs>[] BoneExplosion()
    {
        #region <BoneExplosion/Init>
        var eventGroup = new Action<UnitEventArgs>[(int) UnitEventType.Count];
        #endregion

        #region <BoneExplosion/Begin>

        eventGroup[(int) UnitEventType.Begin] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            if (!boneWall.IsActive)
            {
                caster.ResetFromCast();
                return;
            }
            var focus = caster.Focus;
            caster.UnitBoneAnimator.SetCast("Spell", caster.hyperParameterSet[(int)HyperParameterOfSpell.Spell06].Motion);
        };
            
        #endregion
            
        #region <BoneExplosion/Cue>
        eventGroup[(int)UnitEventType.Cue] = eventArgs =>
        {
            var caster = (Computer)eventArgs.Caster;
            boneWall.Remove();
            
            VfxManager.GetInstance.CreateVfx(VfxManager.Type.BoneExplosion, caster.GetPosition)
                .SetLifeSpan(1f)
                .SetScale(1f)
                .SetTrigger();

            UnitFilter.GetUnitAtLocation(caster.GetPosition,
                caster.hyperParameterSet[(int) HyperParameterOfSpell.Spell06].Range, caster,
                UnitFilter.Condition.IsAlive, FilteredObjectGroup);
            foreach (var obj in FilteredObjectGroup)
            {
                var target = (Unit) obj;
                if(UnitFilter.Check(caster, target, UnitFilter.Condition.IsOrCondition | UnitFilter.Condition.IsDead | UnitFilter.Condition.IsInvincible)) continue;
                if(target != null)
                    target.Hurt(caster, hyperParameterSet[(int)HyperParameterOfSpell.Spell06].Damage, TextureType.Magic);
            }

        };
        #endregion

        #region <BoneExplosion/Terminate>
        eventGroup[(int) UnitEventType.End] = other =>
        {
            var caster = (Computer)other.Caster;          
            caster.ResetFromCast();
        };
        #endregion
            
        return eventGroup;
    }
    #endregion </BoneWall>
    #endregion
}