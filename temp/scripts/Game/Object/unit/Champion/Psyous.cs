using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;


public class Psyous : Champion
{
    #region <Enums>
    private enum Info
    {
        speed,
        damage,
        max_collider,
        life_time,
        cooltime
    }

    #endregion </Enums>

    #region <Consts>

    // cached collider size
    private static readonly Vector3 FireBall_Range = new Vector3(2.0f, 0.0f, 0.0f);  // (horizon wide, height, vertical wide) Sphere type
    private static readonly Vector3 ForceHammer_Range = new Vector3(2.5f, 0.0f, 0.0f);  // (horizon wide, height, vertical wide) Sphere type
    private static readonly Vector3 MagmaBall_Range = new Vector3(0.5f, 2.0f, 0.015f);  // (horizon wide, height, vertical wide) Box type
    private static readonly Vector3 FireRain_Range = new Vector3(3.0f, 0.0f, 0.0f);  // (horizon wide, height, vertical wide) Sphere type
    private static readonly Vector3 MagicHand_Range = new Vector3(3.0f, 2.0f, 1.0f);  // (horizon wide, height, vertical wide) Box type
    private static readonly Vector3 BurningSheild_Range = new Vector3(3.0f, 2.0f, 1.0f);  // (horizon wide, height, vertical wide) Box type
    private static readonly Vector3 Blaze_Range = new Vector3(3.0f, 0.0f, 0.0f);  // (horizon wide, height, vertical wide) Sphere type
    private static readonly Vector3 FireArrow_Range = new Vector3(0.5f, 2.0f, 0.015f);  // (horizon wide, height, vertical wide) Box type

    /// cached skill information                             [speed | (int)damage | (int)max_collider | life_time | (int)cooltime]
    private static readonly float[] NormalAction_Info =     { 30.0f,    3.0f,           1f,             1.0f,           1.0f };
    private static readonly float[] FireBall_Info =         { 20.0f,    5.0f,           5f,             5.0f,           1.0f };
    private static readonly float[] ForceHammer_Info =      { 0.0f,     10.0f,          10f,            5.0f,           1.0f };
    private static readonly float[] MagmaBall_Info =        { 5.0f,     1.0f,           5f,             5.0f,           1.0f };
    private static readonly float[] FireRain_Info =         { 0.0f,     1.0f,           10f,            3.0f,           1.0f };
    private static readonly float[] MagicHand_Info =        { 20.0f,    2.0f,           10f,            5.0f,           1.0f };
    private static readonly float[] BurningSheild_Info =    { 8.0f,     2.0f,           10f,            2.5f,           1.0f };
    private static readonly float[] Blaze_Info =            { 3.0f,     5.0f,           10f,            3.0f,           1.0f };
    private static readonly float[] FireArrow_Info =        { 30.0f,    5.0f,           1f,             1.5f,           5.0f };

    private static readonly float Blink_Range = 2.5f;
    private static readonly float Binding_Range = 5.0f;

    #endregion </Consts>

    #region <Feild>
    private bool isMagicHandGrab = false;
    private Projectile grabProjectile;
    #endregion

    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();

        ActionGroupRoot[(int)ActionButtonTrigger.Type.Normal].Add(NormalAction01());
        ActionGroupRoot[(int)ActionButtonTrigger.Type.Normal].Add(NormalAction02());
        ActionGroupRoot[(int)ActionButtonTrigger.Type.Normal].Add(NormalAction03());

        ActionGroupRoot[(int)ActionButtonTrigger.Type.Primary].Add(Spell01());
        ActionGroupRoot[(int)ActionButtonTrigger.Type.Secondary].Add(Spell10());

        for (var index=0; index < ActionGroupRoot.Count; index++)
        {
            var ActionGroup = ActionGroupRoot[index];
            var args = ActionArgs[ActionGroup].SetCaster(this).SetFactor(index);
            if(ActionGroup[0][(int)UnitEventType.Initialize]!=null)
                ActionGroup[0][(int)UnitEventType.Initialize](args);
        }
    }

    #endregion </Unity/Callbacks>

    #region <NormalAction/Methods>

    private Action<UnitEventArgs>[] NormalAction01()
    {

        #region <NormalAction/Methods/Init>
        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];
        #endregion

        #region <NormalAction/Methods/SetTrigger>
        eventGroup[(int)UnitEventType.SetTrigger] =
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];
        eventGroup[(int)UnitEventType.SetTrigger] += eventArgs =>
        {
            var caster = (Champion)eventArgs.Caster;
            caster.CurrentActionStatus.SetChain(3);
        };
        #endregion

        #region <NormalAction/Methods/Begin>   
        eventGroup[(int)UnitEventType.Begin] = (eventArgs) =>
        {
            var caster = (Champion)eventArgs.Caster;
            caster.UpdateTension();
            eventArgs.SetCandidate(caster.DetectAndChaseEnemyInRange(8.5f, 0f, 0f));
            caster.UnitBoneAnimator.SetCast("Normal-Action", 0);

        };
        #endregion

        #region <NormalAction/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;

            var triggerPosition = caster.AttachPoint[(int)AttachPointType.RightHandIndex1].position;
            var direction = other.Candidate == null ?
                    caster.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            VfxManager.GetInstance.CreateVfx(VfxManager.Type.ArrowCylinder, triggerPosition)
                .SetLifeSpan(.3f)
                .SetForward(caster.Transform.forward)
                .SetScale(1f)
                .SetTrigger();

            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousMagicMissile, caster, NormalAction_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * NormalAction_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)NormalAction_Info[(int)Info.max_collider])
                .SetRemoveDelay(0.1f)
                .SetProjectileType(Projectile.ProjectileType.Point)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)NormalAction_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetTrigger(true);

            caster.CurrentActionStatus.CurrentChain++;
        };
        #endregion

        #region <NormalAction/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } //Normal Action 01

    private Action<UnitEventArgs>[] NormalAction02()
    {

        #region <NormalAction/Methods/Init>
        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];
        #endregion

        #region <NormalAction/Methods/SetTrigger>
        eventGroup[(int)UnitEventType.SetTrigger] =
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];
        #endregion

        #region <NormalAction/Methods/Begin>   
        eventGroup[(int)UnitEventType.Begin] = (eventArgs) =>
        {
            var lChampion = (Champion)eventArgs.Caster;
            lChampion.UpdateTension();
            eventArgs.SetCandidate(lChampion.DetectAndChaseEnemyInRange(8.5f, 0f, 0f));
            lChampion.UnitBoneAnimator.SetCast("Normal-Action", 1);

        };
        #endregion

        #region <NormalAction/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;

            var triggerPosition = caster.AttachPoint[(int)AttachPointType.RightHandIndex1].position;
            var direction = other.Candidate == null ?
                    caster.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousMagicMissile, caster, NormalAction_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * NormalAction_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)NormalAction_Info[(int)Info.max_collider])
                .SetRemoveDelay(0.1f)
                .SetProjectileType((int)Projectile.ProjectileType.Point)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)NormalAction_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetTrigger(true);

            caster.CurrentActionStatus.CurrentChain++;
        };
        #endregion

        #region <NormalAction/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } //Normal Action 02

    private Action<UnitEventArgs>[] NormalAction03()
    {
        #region <NormalAction/Methods/Init>
        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];
        #endregion

        #region <NormalAction/Methods/SetTrigger>
        eventGroup[(int)UnitEventType.SetTrigger] =
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];
        #endregion

        #region <NormalAction/Methods/Begin>   
        eventGroup[(int)UnitEventType.Begin] = (eventArgs) =>
        {
            var lChampion = (Champion)eventArgs.Caster;
            lChampion.UpdateTension();
            eventArgs.SetCandidate(lChampion.DetectAndChaseEnemyInRange(8.5f, 0f, 0f));
            lChampion.UnitBoneAnimator.SetCast("Normal-Action", 2);

        };
        #endregion

        #region <NormalAction/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;

            var triggerPosition = caster.AttachPoint[(int)AttachPointType.LeftHandIndex1].position;
            var direction = other.Candidate == null ?
                    caster.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousMagicMissile, caster, NormalAction_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * NormalAction_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)NormalAction_Info[(int)Info.max_collider])
                .SetRemoveDelay(0.1f)
                .SetProjectileType((int)Projectile.ProjectileType.Point)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)NormalAction_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetTrigger(true);

            caster.CurrentActionStatus.CurrentChain++;
        };
        #endregion

        #region <NormalAction/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } //Normal Action 03

    #endregion </NormalAction/Methods>

    #region <Spell/Methods>      

    private Action<UnitEventArgs>[] Spell01()
    {
        #region <Spell01/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell01/Methods/SetTrigger>


        eventGroup[(int)UnitEventType.SetTrigger] =
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];


        #endregion

        #region <Spell01/Methods/Begin>

        eventGroup[(int)UnitEventType.Begin] = (eventArgs) =>
        {
            var caster = (Champion)eventArgs.Caster;
            caster.UpdateTension();
            eventArgs.SetCandidate(caster.DetectAndChaseEnemyInRange(8.5f, 0f, 0f));
            caster.UnitBoneAnimator.SetCast("Spell", 0);
        };

        #endregion

        #region <Spell01/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;

            var triggerPosition = caster.AttachPoint[(int)AttachPointType.LeftHandIndex1].position;
            var direction = other.Candidate == null ?
                    caster.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Fire, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();
            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Ember, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();

            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousFireBall, caster, FireBall_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * FireBall_Info[(int)Info.speed])
                .SetMaxColliderNumber(1)
                .SetDirection()
                .SetRemoveDelay(0.5f)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.Boom, caster, FireBall_Info[(int)Info.life_time], proj.Transform.position)
                        .SetMaxColliderNumber((int)FireBall_Info[(int)Info.max_collider])
                        .SetRemoveDelay(1.0f)
                        .SetProjectileType(Projectile.ProjectileType.Sphere)
                        .SetColliderBox(FireBall_Range)
                        .SetCollideUnitAction(eventArgs =>
                        {
                            var Explotion_proj = (Projectile)args.MorphObject;
                            foreach (var collidedUnit in Explotion_proj.CollidedUnitGroup)
                            {
                                collidedUnit.Hurt(Explotion_proj.Caster, (int)FireBall_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                                    (trigger, subject, forceDirection) =>
                                    {
                                        subject.AddForce(forceDirection * 10.0f);
                                    });
                            }
                            Explotion_proj.Remove();
                        })
                        .SetTrigger(true);
                    proj.Remove();
                })
                .SetTrigger(true);

        };
        #endregion

        #region <Spell01/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // Fire_Ball

    private Action<UnitEventArgs>[] Spell02()
    {
        #region <Spell02/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell02/Methods/SetTrigger>

        eventGroup[(int)UnitEventType.SetTrigger] = (eventArgs) =>
        {
            eventArgs.SetFactor(ForceHammer_Range.magnitude);
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.TargetToLocation](eventArgs);
        };

        #endregion

        #region <Spell02/Methods/Begin>
        eventGroup[(int)UnitEventType.Begin] = other =>
            {
                var caster = (Champion)other.Caster;
                caster.UpdateTension();
                caster.UnitBoneAnimator.SetCast("Spell", 1);
            };
        #endregion

        #region <Spell02/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;
            var triggerPosition = other.CastPosition;

            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Blast, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(0.3f)
                    .SetTrigger();

            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousForceHammer, caster, ForceHammer_Info[(int)Info.life_time], triggerPosition)
                .SetMaxColliderNumber((int)ForceHammer_Info[(int)Info.max_collider])
                .SetRemoveDelay(2.0f)
                .SetProjectileType(Projectile.ProjectileType.Sphere)
                .SetColliderBox(ForceHammer_Range)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)ForceHammer_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetTrigger(true);

        };
        #endregion

        #region <Spell02/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // Force_Hammer

    private Action<UnitEventArgs>[] Spell03()
    {
        #region <Spell03/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell03/Methods/SetTrigger>


        eventGroup[(int)UnitEventType.SetTrigger] = ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];


        #endregion

        #region <Spell03/Methods/Begin>

        eventGroup[(int)UnitEventType.Begin] = other =>
          {
              var caster = (Champion)other.Caster;
              caster.UpdateTension();
              other.SetCandidate(caster.DetectAndChaseEnemyInRange(8.5f, 0f, 0f));
              caster.UnitBoneAnimator.SetCast("Spell", 0);
          };

        #endregion

        #region <Spell03/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;

            var triggerPosition = caster.AttachPoint[(int)AttachPointType.LeftHandIndex1].position;
            var direction = other.Candidate == null ?
                    caster.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;
            var fallDirection = direction + new Vector3(0, -4f, 0);

            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Fire, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();
            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Ember, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();

            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousMagmaBall, caster, MagmaBall_Info[(int)Info.life_time], triggerPosition - (fallDirection * 5))
                .SetVelocity(fallDirection * 20)
                .SetDirection()
                .SetCollidedObstacleAction(arg =>
                {
                    var proj = (Projectile)arg.MorphObject;
                    ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousMagmaBall, caster, MagmaBall_Info[(int)Info.life_time], proj.Transform.position)
                        .SetVelocity(direction * MagmaBall_Info[(int)Info.speed])
                        .SetDirection()
                        .SetMaxColliderNumber((int)MagmaBall_Info[(int)Info.max_collider])
                        .SetProjectileType(Projectile.ProjectileType.Box)
                        .SetColliderBox(MagmaBall_Range)
                        .SetNumberOfHit(15)
                        .SetCollideUnitAction(args =>
                        {
                            var subProj = (Projectile)args.MorphObject;
                            foreach (var collidedUnit in subProj.CollidedUnitGroup)
                            {
                                collidedUnit.Hurt(subProj.Caster, (int)MagmaBall_Info[(int)Info.damage], TextureType.Heavy, subProj.Direction,
                                    (trigger, subject, forceDirection) =>
                                    {
                                        subject.AddForce(forceDirection * 6.0f);
                                    });
                            }
                        })
                        .SetTrigger(true);
                    proj.Remove();
                })
                .SetTrigger(true);

        };
        #endregion

        #region <Spell03/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // Magma_Ball

    private Action<UnitEventArgs>[] Spell04()
    {
        #region <Spell04/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell04/Methods/SetTrigger>
        eventGroup[(int)UnitEventType.SetTrigger] = (eventArgs) =>
        {
            eventArgs.SetFactor(FireRain_Range.magnitude);
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.TargetToLocation](eventArgs);
        };
        #endregion

        #region <Spell04/Methods/Begin>
        eventGroup[(int)UnitEventType.Begin] = other =>
          {
              var caster = (Champion)other.Caster;
              caster.UpdateTension();
              caster.UnitBoneAnimator.SetCast("Spell", 1);
          };
        #endregion

        #region <Spell04/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;
            var triggerPosition = other.CastPosition;

            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousFireRain, caster, FireRain_Info[(int)Info.life_time], triggerPosition)
                .SetMaxColliderNumber((int)FireRain_Info[(int)Info.max_collider])
                .SetRemoveDelay(1.0f)
                .SetProjectileType(Projectile.ProjectileType.Sphere)
                .SetColliderBox(FireRain_Range)
                .SetIgnoreObstacle(true)
                .SetActiveHeartBeat(true)
                .SetNumberOfHit(3)
                .SetOnHeartBeatTension(3)
                .SetOnHeartBeatAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)FireRain_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                })
                .SetTrigger(true);


        };
        #endregion

        #region <Spell04/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // Fire_Rain
      //TODO vfx
      //TODO Animation and Camera Colided
    private Action<UnitEventArgs>[] Spell05()
    {
        #region <Spell05/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell05/Methods/SetTrigger>
        eventGroup[(int)UnitEventType.SetTrigger] = (eventArgs) =>
        {
            eventArgs.SetFactor(1f);
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.TargetToLocation](eventArgs);
        };
        #endregion

        #region <Spell05/Methods/Begin>
        eventGroup[(int)UnitEventType.Begin] = other =>
          {
              var caster = (Champion)other.Caster;
              caster.UpdateTension();
              caster.UnitBoneAnimator.SetCast("Spell", 0);
          };
        #endregion

        #region <Spell05/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;
            var triggerPosition = caster.Transform.position + ((other.CastPosition - caster.Transform.position).normalized * Blink_Range);
            /*
            RaycastHit hit;
            Physics.Raycast(triggerPosition + new Vector3(0, 5f, 0), -Transform.up, out hit);
            caster.Transform.position = hit.point;
            */

            var particle = VfxManager.GetInstance.CreateVfx(VfxManager.Type.Blink, caster.Transform.position)
                .SetLifeSpan(3f)
                .SetForward(other.CastPosition - caster.Transform.position)
                .SetScale(1f)
                .SetTrigger();

            caster.Transform.position = triggerPosition;
            particle.Transform.position = triggerPosition;

        };
        #endregion

        #region <Spell05/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // Blink
      //TODO vfx
    private Action<UnitEventArgs>[] Spell06()
    {
        #region <Spell06/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell06/Methods/SetTrigger>

        eventGroup[(int)UnitEventType.SetTrigger] = ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];

        #endregion

        #region <Spell06/Methods/Begin>
        eventGroup[(int)UnitEventType.Begin] = other =>
          {
              var caster = (Champion)other.Caster;
              caster.UpdateTension();
              caster.UnitBoneAnimator.SetCast("Spell", 0);
          };
        #endregion

        #region <Spell06/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;
            var triggerPosition = caster.Transform.position;

            var enemyIterator =                       
                UnitFilter.GetUnitAtLocation(caster.GetPosition, Binding_Range, caster,
                    UnitFilter.Condition.IsNegative, FilteredObjectGroup);
            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Smog, caster.Transform.position)
                .SetLifeSpan(3f)
                .SetScale(1f)
                .SetTrigger();

            while (enemyIterator > 0)
            {
                Enemy candidate = (Enemy)FilteredObjectGroup[--enemyIterator];

                var particle = VfxManager.GetInstance.CreateVfx(VfxManager.Type.Bind, candidate.Transform.position)
                                         .SetLifeSpan(4f)
                                         .SetScale(1f)
                                         .SetTrigger();

                candidate.Hurt(caster, 0, TextureType.Magic, Vector3.zero);                

                var cc = Buff.Stun(candidate, 5);
                cc.BuffArgs.SetCaster(caster).SetParticle(particle);

                candidate.AddCrowdControl(cc
                    .SetAction(Buff.EventType.OnHeartBeat,
                        (args) => { args.Owner.Hurt(args.Caster, 1, TextureType.Magic); }
                    )
                    .SetAction(Buff.EventType.OnFixedUpdate,
                        (args) => { args.Particle.Transform.position = args.Owner.GetPosition; }
                    ));
            }

        };
        #endregion

        #region <Spell06/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // Binding
      //TODO vfx
    private Action<UnitEventArgs>[] Spell07()
    {
        #region <Spell07/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell07/Methods/SetTrigger>

        eventGroup[(int)UnitEventType.SetTrigger] = ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];

        #endregion

        #region <Spell07/Methods/Begin>
        eventGroup[(int)UnitEventType.Begin] = other =>
          {
              var caster = (Champion)other.Caster;
              caster.UpdateTension();
              other.SetCandidate(caster.DetectAndChaseEnemyInRange(7.5f, 0.0f, .0f));
              if (other.Candidate != null)
              {
                  caster.UnitBoneAnimator.SetCast("Spell", 0);
              }
          };
        #endregion

        #region <Spell07/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;
            var target = (Enemy)other.Candidate;
            if (target == null) return;
            if (!isMagicHandGrab)
            {
                var triggerPosition = target.Transform.position;
                var grabPosition = caster.Transform.position - caster.Transform.forward - caster.Transform.right + caster.Transform.up;
                
                var grabVector = grabPosition - triggerPosition;

                var currentAction = CurrentActionStatus;

                target.Hurt(caster, 0, TextureType.Magic, Vector3.zero);
                target.AddCrowdControl(Buff.Stun(target, (int)MagicHand_Info[(int)Info.life_time]*2));
                ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousMagicHand, caster, 0.2f, triggerPosition)
                    .SetVelocity(grabVector * 5)
                    .SetProjectileType(Projectile.ProjectileType.Point)
                    .SetDirection()
                    .SetIgnoreUnit(true)
                    .SetIgnoreObstacle(true)
                    .SetTrigger(true)
                    .SetFixedUpdateAction((eventArgs) =>
                    {
                        var proj = (Projectile)eventArgs.MorphObject;
                        target.Transform.position = proj.Transform.position;
                    })
                    .SetExpiredAction(args =>
                    {
                        target.gameObject.tag = "Projectile";
                        target.gameObject.layer = 12;
                        grabProjectile = ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousMagicHand, caster, (int)MagicHand_Info[(int)Info.life_time], grabPosition)
                            .SetProjectileType(Projectile.ProjectileType.Box)
                            .SetMaxColliderNumber((int)MagicHand_Info[(int)Info.max_collider])
                            .SetColliderBox(MagicHand_Range)
                            .SetCollideUnitAction(collided =>
                            {
                                var proj = (Projectile)collided.MorphObject;
                                foreach (var collidedUnit in proj.CollidedUnitGroup)
                                {
                                    collidedUnit.Hurt(caster, (int)MagicHand_Info[(int)Info.damage], TextureType.Heavy, proj.Velocity.normalized,
                                    (actionCaster, candidate, forceVecter) =>
                                    {
                                        candidate.AddForce(forceVecter * 2.0f);
                                    });
                                    target.Hurt(caster, (int)MagicHand_Info[(int)Info.damage], TextureType.Heavy, Vector3.zero);
                                    if (target.CurrentHealthPoint <= 0)
                                    {
                                        target.gameObject.tag = "Enemy";
                                        target.gameObject.layer = 11;
                                        proj.Remove();
                                        if (target.CrowdControlGroup.Count !=0)
                                        {
                                            target.CrowdControlGroup.First().OnTerminate();
                                        }
                                        isMagicHandGrab = false;
                                        currentAction.SetCooldown((int)MagicHand_Info[(int)Info.cooltime]);
                                        grabProjectile = null;
                                    }
                                }
                            })
                            .SetFixedUpdateAction((eventArgs) =>
                            {
                                var proj = (Projectile)eventArgs.MorphObject;
                                proj.Transform.position = caster.Transform.position - caster.Transform.forward - caster.Transform.right + caster.Transform.up;
                                target.Transform.position = proj.Transform.position;
                            })
                            .SetIgnoreObstacle(true)
                            .SetExpiredAction(expired =>
                            {
                                target.gameObject.tag = "Enemy";
                                target.gameObject.layer = 11;
                                if (target.CrowdControlGroup.Count != 0)
                                {
                                    target.CrowdControlGroup.First().OnTerminate();
                                }
                                isMagicHandGrab = false;
                                currentAction.SetCooldown((int)MagicHand_Info[(int)Info.cooltime]);
                                grabProjectile = null;
                            })
                            .SetTrigger(true);
                    });
                
                isMagicHandGrab = true;
            }
            else
            {
                var triggerPosition = grabProjectile.Transform.position;
                var targetTransform = target.Transform;

                grabProjectile
                    .SetFixedUpdateAction((eventArgs) =>
                    {
                        var proj = (Projectile)eventArgs.MorphObject;
                        target.Transform.position = proj.Transform.position;
                    })
                    .SetEnqueuePoint(triggerPosition)
                    .SetEnqueuePoint(triggerPosition + targetTransform.right * 5)
                    .SetEnqueuePoint(targetTransform.position + targetTransform.right * 5)
                    .SetEnqueuePoint(targetTransform.position)
                    .SetEnqueuePoint(targetTransform.position)
                    .SetEnqueuePoint(targetTransform.position - targetTransform.right * 5)
                    .SetEnqueuePoint(triggerPosition - targetTransform.right * 5)
                    .SetEnqueuePoint(triggerPosition)
                    .SetBezier(4, 0.3f, p_ExpiredAction: (eventArgs, projectile) =>
                    {
                        var proj = (Projectile)eventArgs.MorphObject;
                        proj.Transform.position = caster.Transform.position - caster.Transform.forward - caster.Transform.right + caster.Transform.up;
                        target.Transform.position = proj.Transform.position;
                    })
                    .SetBezier(4, 0.3f)
                    .SetTrigger(true);

                caster.CurrentActionStatus.SetCooldown(1);
                grabProjectile.ExCollidedUnitGroup.Clear();
            }
        };
        #endregion

        #region <Spell07/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // Magic Hand
      //TODO vfx
    private Action<UnitEventArgs>[] Spell08()
    {
        #region <Spell08/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell08/Methods/SetTrigger>

        eventGroup[(int)UnitEventType.SetTrigger] = ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];

        #endregion

        #region <Spell08/Methods/Begin>

        eventGroup[(int)UnitEventType.Begin] = other =>
          {
              var caster = (Champion)other.Caster;
              caster.UpdateTension();
              other.SetCandidate(caster.DetectAndChaseEnemyInRange(8.5f, 0f, 0f));
              caster.UnitBoneAnimator.SetCast("Spell", 0);
          };

        #endregion

        #region <Spell08/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;

            var triggerPosition = caster.AttachPoint[(int)AttachPointType.LeftHandIndex1].position;
            var direction = other.Candidate == null ?
                    caster.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Fire, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();
            VfxManager.GetInstance.CreateVfx(VfxManager.Type.Ember, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();


            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousFireShield, caster, BurningSheild_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * BurningSheild_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)BurningSheild_Info[(int)Info.max_collider])
                .SetProjectileType(Projectile.ProjectileType.Box)
                .SetColliderBox(BurningSheild_Range)
                .SetNumberOfHit(3)
                .SetOnHeartBeatTension(3)
                .SetOnHeartBeatAction(args =>
                {
                    var subProj = (Projectile)args.MorphObject;

                    VfxManager.GetInstance.CreateVfx(VfxManager.Type.FireShield, subProj.Transform.position)
                        .SetLifeSpan(.3f)
                        .SetForward(subProj.Transform.forward)
                        .SetScale(1f)
                        .SetTrigger();
                    foreach (var collidedUnit in subProj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(subProj.Caster, (int)BurningSheild_Info[(int)Info.damage], TextureType.Heavy, subProj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                            });
                    }
                })
                .SetFixedUpdateAction(args =>
                {
                    var subProj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in subProj.CollidedUnitGroup)
                    {
                        collidedUnit.Transform.position += subProj.Transform.forward * 0.3f;
                    }
                })
                .SetActiveHeartBeat(true)
                .SetTrigger(true);
        };
        #endregion

        #region <Spell08/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // BurningSheild
    //TODO vfx
    private Action<UnitEventArgs>[] Spell09()
    {
        #region <Spell09/Methods/Init>

        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];

        #endregion

        #region <Spell09/Methods/SetTrigger>

        eventGroup[(int)UnitEventType.SetTrigger] = (eventArgs) =>
        {
            eventArgs.SetFactor(Blaze_Range.magnitude);
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.TargetToLocation](eventArgs);
        };

        #endregion

        #region <Spell09/Methods/Begin>
        eventGroup[(int)UnitEventType.Begin] = other =>
        {
            var caster = (Champion)other.Caster;
            caster.UpdateTension();
            other.SetCandidate(caster.DetectAndChaseEnemyInRange(7.5f, 0.0f, .0f));
            if (other.Candidate == null)
            {
                caster.OnCastAnimationEnd();
                return;
            }
            caster.UnitBoneAnimator.SetCast("Spell", 0);
        };
        #endregion

        #region <Spell09/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;
            var target = (Enemy)other.Candidate;
            if (target == null) return;


            var triggerPosition = target.Transform.position;
            var grabPosition = caster.Transform.position - caster.Transform.forward + caster.Transform.right + caster.Transform.up;

            var grabVector = grabPosition - triggerPosition;

            target.Hurt(caster, 0, TextureType.Magic, Vector3.zero);            
            target.AddCrowdControl(Buff.Stun(target, (int)Blaze_Info[(int)Info.life_time]+1));
            
            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousFireBall, caster, 0.10f, triggerPosition)
                .SetVelocity(grabVector * 6)
                .SetProjectileType(Projectile.ProjectileType.Point)
                .SetDirection()
                .SetIgnoreUnit(true)
                .SetIgnoreObstacle(true)
                .SetTrigger(true)
                .SetFixedUpdateAction((eventArgs) =>
                {
                    var proj = (Projectile)eventArgs.MorphObject;
                    target.Transform.position = proj.Transform.position;
                })
                .SetExpiredAction(args =>
                {
                    target.gameObject.tag = "Projectile";
                    target.gameObject.layer = 12;
                    grabPosition = ((Projectile)args.MorphObject).Transform.position;
                    ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousFireBall, caster, (int)Blaze_Info[(int)Info.life_time], grabPosition)
                        .SetCollidedObstacleAction(temp =>
                        {
                            var projectileUnit = (Projectile)temp.MorphObject;
                            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.Boom, caster, 0.2f, projectileUnit.Transform.position)
                                .SetRemoveDelay(2.0f)
                                .SetProjectileType(Projectile.ProjectileType.Sphere)
                                .SetMaxColliderNumber((int)Blaze_Info[(int)Info.max_collider])
                                .SetColliderBox(Blaze_Range)
                                .SetCollideUnitAction(collided =>
                                {
                                    var proj = (Projectile)collided.MorphObject;
                                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                                    {
                                        collidedUnit.Hurt(caster, (int)Blaze_Info[(int)Info.damage], TextureType.Heavy, proj.Velocity.normalized,
                                        (actionCaster, candidate, forceVecter) =>
                                        {
                                            candidate.AddForce(forceVecter * 2.0f);
                                        });
                                        target.Hurt(caster, (int)Blaze_Info[(int)Info.damage], TextureType.Heavy, Vector3.zero);
                                        
                                        proj.Remove();
                                    }
                                })
                                .SetTrigger(true);
                            projectileUnit.Remove();
                            target.gameObject.tag = "Enemy";
                            target.gameObject.layer = 11;
                            target.CrowdControlGroup.First().OnTerminate();
                        })
                        .SetFixedUpdateAction((eventArgs) =>
                        {
                            var proj = (Projectile)eventArgs.MorphObject;
                            target.Transform.position = proj.Transform.position;
                        })
                        .SetEnqueuePoint(grabPosition)
                        .SetEnqueuePoint(grabPosition + Vector3.up * 4)
                        .SetEnqueuePoint(other.CastPosition + Vector3.up * 4)
                        .SetEnqueuePoint(other.CastPosition)
                        .SetBezier(4, 0.6f)
                        .SetTrigger(true);
                });          
        };
        #endregion

        #region <Spell09/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // Blaze (Fire Buster)

    private Action<UnitEventArgs>[] Spell10()
    {
        #region <Spell10/Methods/Init>
        var eventGroup = new Action<UnitEventArgs>[(int)UnitEventType.Count];
        #endregion

        #region <Spell10/Methods/Initialize>
        eventGroup[(int)UnitEventType.Initialize] = eventArgs =>
        {
            ActionStatusGroup[ActionGroupRoot[eventArgs.IntFactor]].SetStack(5);
            ActionStatusGroup[ActionGroupRoot[eventArgs.IntFactor]].SetStackCooldown(5);
        };
        #endregion

        #region <Spell10/Methods/SetTrigger>
        eventGroup[(int)UnitEventType.SetTrigger] =
            ActionButtonTrigger.CastTypeSetter[(int)ActionButtonTrigger.CastType.Auto];
        #endregion

        #region <Spell10/Methods/Begin>   
        eventGroup[(int)UnitEventType.Begin] = (eventArgs) =>
        {
            var caster = (Champion)eventArgs.Caster;
            caster.UpdateTension();
            eventArgs.SetCandidate(caster.DetectAndChaseEnemyInRange(8.5f, 0f, 0f));
            caster.UnitBoneAnimator.SetCast("Spell", 0);

        };
        #endregion

        #region <Spell10/Methods/Cue>
        eventGroup[(int)UnitEventType.Cue] = (other) =>
        {
            var caster = (Champion)other.Caster;
            var triggerPosition = caster.AttachPoint[(int)AttachPointType.RightHandIndex1].position;
            var direction = other.Candidate == null ?
                    caster.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            VfxManager.GetInstance.CreateVfx(VfxManager.Type.ArrowCylinder, triggerPosition)
                .SetLifeSpan(.3f)
                .SetForward(caster.Transform.forward)
                .SetScale(1f)
                .SetTrigger();

            ProjectileFactory.GetInstance.CreateProjectile(ProjectileFactory.Type.PsyousFireArrow, caster, FireArrow_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * FireArrow_Info[(int)Info.speed])
                .SetDirection()
                .SetColliderBox(FireArrow_Range)
                .SetMaxColliderNumber((int)FireArrow_Info[(int)Info.max_collider])
                .SetRemoveDelay(0.1f)
                .SetProjectileType(Projectile.ProjectileType.Box)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)FireArrow_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetTrigger(true);

            caster.CurrentActionStatus.CurrentStack--;
            caster.CurrentActionStatus.SetCooldown(1);
        };
        #endregion

        #region <Spell10/Methods/End>
        eventGroup[(int)UnitEventType.End] = DefaultReset;
        #endregion

        return eventGroup;
    } // FireArrow


    #endregion </Spell/Methods>



    ////////////////////////// original code /////////////////////////////
    ///


    /*
    #region <Enums>
    private enum Info{
        speed,
        damage,
        max_collider,
        life_time
    }

    #endregion </Enums>

    #region <Consts>

    // cached collider size
    private static readonly Vector3 FireBall_Range = new Vector3(1.0f, 2.0f, 0.015f);  // (horizon wide, height, vertical wide) Box type
    private static readonly Vector3 ForceHammer_Range = new Vector3(2.5f, 0.0f, 0.0f);  // (horizon wide, height, vertical wide) Sphere type
    private static readonly Vector3 MagmaBall_Range = new Vector3(0.5f, 2.0f, 0.015f);  // (horizon wide, height, vertical wide) Box type
    private static readonly Vector3 FireRain_Range = new Vector3(3.0f, 0.0f, 0.0f);  // (horizon wide, height, vertical wide) Sphere type
    private static readonly Vector3 MagicHand_Range = new Vector3(3.0f, 2.0f, 1.0f);  // (horizon wide, height, vertical wide) Box type
    private static readonly Vector3 BurningSheild_Range = new Vector3(3.0f, 2.0f, 3.0f);  // (horizon wide, height, vertical wide) Box type

    /// cached skill information [speed, (int)damage, (int)max_collider, life_time]
    private static readonly float[] NormalAction_Info = { 30.0f, 3.0f, 1f, 0.5f };
    private static readonly float[] FireBall_Info =     { 20.0f, 5.0f, 5f, 5.0f };
    private static readonly float[] ForceHammer_Info =  { 0.0f, 10.0f, 10f, 1.0f };
    private static readonly float[] MagmaBall_Info =    { 5.0f, 1.0f, 5f, 5.0f };
    private static readonly float[] FireRain_Info =     { 0.0f, 1.0f, 10f, 5.0f };
    private static readonly float[] MagicHand_Info =    { 20.0f, 2.0f, 10f, 5.0f };
    private static readonly float[] BurningSheild_Info = { 3.0f, 2.0f, 10f, 5.0f };

    private static readonly float Blink_Range = 1.5f;
    private static readonly float Binding_Range = 5.0f;

    #endregion </Consts>

    #region <Feild>
    private bool isMagicHandGrab = false;
    private Projectile grabedUnit;
    #endregion

    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();

        ChampionType = K514SfxStorage.ChampionType.Psyous;

        NormalActionsEventGroup = new[]
        {
            NormalAction01(HUDManager.GetInstance.ActionTriggerGroup[(int)ActionTrigger.TriggerType.MainTrigger]),
            NormalAction02(HUDManager.GetInstance.ActionTriggerGroup[(int)ActionTrigger.TriggerType.MainTrigger]),
            NormalAction03(HUDManager.GetInstance.ActionTriggerGroup[(int)ActionTrigger.TriggerType.MainTrigger])
        };

        PrimaryActionEventGroup = Spell07(HUDManager.GetInstance.ActionTriggerGroup[(int)ActionTrigger.TriggerType.SubTriggerLeft]);
        SecondaryActionEventGroup = Spell03(HUDManager.GetInstance.ActionTriggerGroup[(int)ActionTrigger.TriggerType.SubTriggerRight]);
    }

    #endregion </Unity/Callbacks>

    #region <NormalAction/Methods>

    private Action<CustomEventArgs.CommomActionArgs>[] NormalAction01(ActionTrigger pTargetTrigger)
    {

        #region <NormalAction/Methods/Init>
        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];
        #endregion

        #region <NormalAction/Methods/Clicked>
        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
        };
        #endregion

        #region <NormalAction/Methods/Birth>   
        eventGroup[(int)EventInfo.Birth] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(8.5f, 0f, 0f)));
            UnitBoneAnimator.SetCast("Normal-Action", 0, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Attack).SetTrigger();
        };
        #endregion

        #region <NormalAction/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;

            var triggerPosition = lChampion.AttachPoint[(int) AttachPointType.RightHandIndex1].position;
            var direction = other.Candidate == null ?
                    lChampion.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousMagicMissile, lChampion, NormalAction_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * NormalAction_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)NormalAction_Info[(int)Info.max_collider])
                .SetRemoveDelay(0.1f)
                .SetProjectileType((int)Projectile.ProjectileType.Point)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)NormalAction_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetActive(true);

            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <NormalAction/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        #region <NormalAction/Methods/Released>
        eventGroup[(int)EventInfo.Released] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ProcessNormalAttackSequence();
        };
        #endregion

        return eventGroup;
    } //Normal Action 01

    private Action<CustomEventArgs.CommomActionArgs>[] NormalAction02(ActionTrigger pTargetTrigger)
    {

        #region <NormalAction/Methods/Init>
        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];
        #endregion

        #region <NormalAction/Methods/Clicked>
        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
        };
        #endregion

        #region <NormalAction/Methods/Birth>   
        eventGroup[(int)EventInfo.Birth] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(8.5f, 0f, 0f)));
            UnitBoneAnimator.SetCast("Normal-Action", 1, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Attack).SetTrigger();
        };
        #endregion

        #region <NormalAction/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            
            var triggerPosition = lChampion.AttachPoint[(int)AttachPointType.RightHandIndex1].position;
            var direction = other.Candidate == null ?
                    lChampion.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousMagicMissile, lChampion, NormalAction_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * NormalAction_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)NormalAction_Info[(int)Info.max_collider])
                .SetRemoveDelay(0.1f)
                .SetProjectileType((int)Projectile.ProjectileType.Point)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)NormalAction_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetActive(true);

            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <NormalAction/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        #region <NormalAction/Methods/Released>
        eventGroup[(int)EventInfo.Released] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ProcessNormalAttackSequence();
        };
        #endregion

        return eventGroup;
    } //Normal Action 02

    private Action<CustomEventArgs.CommomActionArgs>[] NormalAction03(ActionTrigger pTargetTrigger)
    {
        #region <NormalAction/Methods/Init>
        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];
        #endregion

        #region <NormalAction/Methods/Clicked>
        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(4, resettable: true);
        };
        #endregion

        #region <NormalAction/Methods/Birth>   
        eventGroup[(int)EventInfo.Birth] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(8.5f, 0f, 0f)));
            UnitBoneAnimator.SetCast("Normal-Action", 2, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Attack).SetTrigger();
        };
        #endregion

        #region <NormalAction/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;

            var triggerPosition = lChampion.AttachPoint[(int)AttachPointType.RightHandIndex1].position;
            var direction = other.Candidate == null ?
                    lChampion.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousMagicMissile, lChampion, NormalAction_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * NormalAction_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)NormalAction_Info[(int)Info.max_collider])
                .SetRemoveDelay(0.1f)
                .SetProjectileType((int)Projectile.ProjectileType.Point)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)NormalAction_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetActive(true);

            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <NormalAction/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        #region <NormalAction/Methods/Released>
        eventGroup[(int)EventInfo.Released] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ProcessNormalAttackSequence();
        };
        #endregion

        return eventGroup;
    } //Normal Action 03

    #endregion </NormalAction/Methods>

    #region <Spell/Methods>      

    private Action<CustomEventArgs.CommomActionArgs>[] Spell01(ActionTrigger pTargetTrigger)
    {
        #region <Spell01/Methods/Init>

        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];

        #endregion

        #region <Spell01/Methods/Clicked>

        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(5, resettable: true);
        };

        #endregion

        #region <Spell01/Methods/Birth>

        eventGroup[(int)EventInfo.Birth] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(8.5f, 0f, 0f)));
            lChampion.UnitBoneAnimator.SetCast("Spell", 0, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Serifu).SetTrigger();
        };

        #endregion

        #region <Spell01/Methods/Enter>

        eventGroup[(int)EventInfo.Enter] = other =>
        {
        };

        #endregion

        #region <Spell01/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;

            var triggerPosition = lChampion.AttachPoint[(int)AttachPointType.LeftHandIndex1].position;
            var direction = other.Candidate == null ?
                    lChampion.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            K514VfxManager.GetInstance.CastVfx(K514VfxManager.ParticleType.PFire, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();
            K514VfxManager.GetInstance.CastVfx(K514VfxManager.ParticleType.PEmber, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();

            ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousFireBall, lChampion, FireBall_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * FireBall_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)FireBall_Info[(int)Info.max_collider])
                .SetRemoveDelay(5.0f)
                .SetProjectileType(Projectile.ProjectileType.Box)
                .SetColliderBox(FireBall_Range)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)FireBall_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 10.0f);
                            });
                    }
                })
                .SetActive(true);

            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <Spell01/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        return eventGroup;
    } // Fire_Ball

    private Action<CustomEventArgs.CommomActionArgs>[] Spell02(ActionTrigger pTargetTrigger)
    {
        #region <Spell02/Methods/Init>

        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];

        #endregion

        #region <Spell02/Methods/Clicked>

        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(5, resettable: true);
        };

        #endregion

        #region <Spell02/Methods/Birth>
        eventGroup[(int)EventInfo.Birth] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(7.5f, 0.0f, .0f)));
            lChampion.UnitBoneAnimator.SetCast("Spell", 1, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Serifu).SetTrigger();
        };
        #endregion

        #region <Spell02/Methods/Enter>
        eventGroup[(int)EventInfo.Enter] = other =>
        {
            
        };
        #endregion

        #region <Spell02/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            var triggerPosition = other.Candidate==null?
                lChampion.AttachPoint[(int)AttachPointType.LeftHandIndex1].position
                : other.Candidate.Transform.position;

            K514VfxManager.GetInstance.CastVfx(K514VfxManager.ParticleType.PBlast, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(0.3f)
                    .SetTrigger();

            ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousForceHammer, lChampion, ForceHammer_Info[(int)Info.life_time], triggerPosition)
                .SetMaxColliderNumber((int)ForceHammer_Info[(int)Info.max_collider])
                .SetRemoveDelay(5.0f)
                .SetProjectileType(Projectile.ProjectileType.Sphere)
                .SetColliderBox(ForceHammer_Range)
                .SetCollideUnitAction(args =>
                {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)ForceHammer_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetActive(true);

            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <Spell02/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        return eventGroup;
    } // Force_Hammer

    private Action<CustomEventArgs.CommomActionArgs>[] Spell03(ActionTrigger pTargetTrigger)
    {
        #region <Spell03/Methods/Init>

        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];

        #endregion

        #region <Spell03/Methods/Clicked>

        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(5, resettable: true);
        };

        #endregion

        #region <Spell03/Methods/Birth>

        eventGroup[(int)EventInfo.Birth] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(8.5f, 0f, 0f)));
            lChampion.UnitBoneAnimator.SetCast("Spell", 0, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Serifu).SetTrigger();
        };

        #endregion

        #region <Spell03/Methods/Enter>

        eventGroup[(int)EventInfo.Enter] = other =>
        {
        };

        #endregion

        #region <Spell03/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;

            var triggerPosition = lChampion.AttachPoint[(int)AttachPointType.LeftHandIndex1].position;
            var direction = other.Candidate == null ?
                    lChampion.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;
            var fallDirection = direction + new Vector3(0, -4f, 0);

            K514VfxManager.GetInstance.CastVfx(K514VfxManager.ParticleType.PFire, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();
            K514VfxManager.GetInstance.CastVfx(K514VfxManager.ParticleType.PEmber, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();

            ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousMagmaBall, lChampion, MagmaBall_Info[(int)Info.life_time], triggerPosition- (fallDirection * 5))
                .SetVelocity(fallDirection * 20)
                .SetDirection()
                .SetCollidedObstacleAction(arg =>
                {
                    var proj = (Projectile)arg.MorphObject;
                    ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousMagmaBall, lChampion, MagmaBall_Info[(int)Info.life_time], proj.Transform.position)
                        .SetVelocity(direction * MagmaBall_Info[(int)Info.speed])
                        .SetDirection()
                        .SetMaxColliderNumber((int)MagmaBall_Info[(int)Info.max_collider])
                        .SetProjectileType(Projectile.ProjectileType.Box)
                        .SetColliderBox(MagmaBall_Range)
                        .SetNumberOfHit(15)
                        .SetCollideUnitAction(args =>
                        {
                            var subProj = (Projectile)args.MorphObject;
                            foreach (var collidedUnit in subProj.CollidedUnitGroup)
                            {
                                collidedUnit.Hurt(subProj.Caster, (int)MagmaBall_Info[(int)Info.damage], TextureType.Heavy, subProj.Direction,
                                    (trigger, subject, forceDirection) =>
                                    {
                                        subject.AddForce(forceDirection * 6.0f);
                                    });
                            }
                        })
                        .SetActive(true);
                    proj.Remove();
                })
                .SetActive(true);

                
            

            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <Spell03/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        return eventGroup;
    } // Magma_Ball

    private Action<CustomEventArgs.CommomActionArgs>[] Spell04(ActionTrigger pTargetTrigger)
    {
        #region <Spell04/Methods/Init>

        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];

        #endregion

        #region <Spell04/Methods/Clicked>

        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(5, resettable: true);
        };

        #endregion

        #region <Spell04/Methods/Birth>
        eventGroup[(int)EventInfo.Birth] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(7.5f, 0.0f, .0f)));
            lChampion.UnitBoneAnimator.SetCast("Spell", 1, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Serifu).SetTrigger();
        };
        #endregion

        #region <Spell04/Methods/Enter>
        eventGroup[(int)EventInfo.Enter] = other =>
        {

        };
        #endregion

        #region <Spell04/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            var triggerPosition = other.Candidate == null ?
                lChampion.AttachPoint[(int)AttachPointType.LeftHandIndex1].position
                : other.Candidate.Transform.position;

            ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousMagmaBall, lChampion, FireRain_Info[(int)Info.life_time], triggerPosition)
                .SetMaxColliderNumber((int)FireRain_Info[(int)Info.max_collider])
                .SetRemoveDelay(3.0f)
                .SetProjectileType(Projectile.ProjectileType.Sphere)
                .SetColliderBox(FireRain_Range)
                .SetNumberOfHitOnHeartBeat(3)
                .SetOnHeartBeatTension(3)
                .SetOnHeartBeatAction(args => {
                    var proj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in proj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(proj.Caster, (int)FireRain_Info[(int)Info.damage], TextureType.Magic, proj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 2.0f);
                            });
                    }
                    proj.Remove();
                })
                .SetActive(true)
                .SetActiveHeartBeat(true);
            

            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <Spell04/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        return eventGroup;
    } // Fire_Rain
    //TODO vfx
    private Action<CustomEventArgs.CommomActionArgs>[] Spell05(ActionTrigger pTargetTrigger)
    {
        #region <Spell05/Methods/Init>

        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];

        #endregion

        #region <Spell05/Methods/Clicked>

        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(1, resettable: true);
        };

        #endregion

        #region <Spell05/Methods/Birth>
        eventGroup[(int)EventInfo.Birth] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            lChampion.UnitBoneAnimator.SetCast("Spell", 1, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Serifu).SetTrigger();
        };
        #endregion

        #region <Spell05/Methods/Enter>
        eventGroup[(int)EventInfo.Enter] = other =>
        {

        };
        #endregion

        #region <Spell05/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            var triggerPosition = lChampion.Transform.position + (lChampion.Transform.forward * Blink_Range);

            RaycastHit hit;
            Physics.Raycast(triggerPosition + new Vector3(0, 5f ,0), -Transform.up,out hit);
            Transform.position = hit.point;
            
            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <Spell05/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        return eventGroup;
    } // Blink
    //TODO vfx
    private Action<CustomEventArgs.CommomActionArgs>[] Spell06(ActionTrigger pTargetTrigger)
    {
        #region <Spell06/Methods/Init>

        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];

        #endregion

        #region <Spell06/Methods/Clicked>

        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(1, resettable: true);
        };

        #endregion

        #region <Spell06/Methods/Birth>
        eventGroup[(int)EventInfo.Birth] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            lChampion.UnitBoneAnimator.SetCast("Spell", 0, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Serifu).SetTrigger();
        };
        #endregion

        #region <Spell06/Methods/Enter>
        eventGroup[(int)EventInfo.Enter] = other =>
        {

        };
        #endregion

        #region <Spell06/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            var triggerPosition = lChampion.Transform.position;

            var enemyIterater = Filter.GetTagGroupInRadiusCompareToTag("Enemy", Binding_Range, FilterCheckedObjectArray);

            while (enemyIterater > 0)
            {
                Enemy candidate = (Enemy)FilterCheckedObjectArray[--enemyIterater];

                candidate.Hurt(lChampion, 0, TextureType.Magic, Vector3.zero);

                candidate.AddCrowdControl(new CrowdControl(candidate, CrowdControl.CrowdControlType.Stun, "Stun", 5, true)
                    .SetAction(CrowdControl.EventType.OnBirth,
                               new CustomEventArgs.CrowdControlArgs()
                                .SetCandidate(candidate)
                                .SetCaster(lChampion),
                               (args) =>
                               {
                                   args.Candidate.UnitBoneAnimator.UnityAnimator.speed = .0f;
                                   args.Candidate.ResetSpellColliderActive();
                               }
                    )
                    .SetAction(CrowdControl.EventType.OnHeartBeat,
                                new CustomEventArgs.CrowdControlArgs()
                                    .SetCaster(lChampion)
                                    .SetCandidate(candidate),
                                (args) =>
                                {
                                    args.Candidate.Hurt(lChampion, 1, TextureType.Magic, Vector3.zero);
                                }
                    )
                    .SetAction(CrowdControl.EventType.OnTerminate,
                                new CustomEventArgs.CrowdControlArgs()
                                    .SetCaster(lChampion)
                                    .SetCandidate(candidate),
                                (args) =>
                                {
                                    args.Candidate.UnitBoneAnimator.UnityAnimator.speed = 1.0f;
                                }
                    )
                    .SetOption(CrowdControl.Option.OverrideAbsolute, true));
            }

            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <Spell06/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        return eventGroup;
    } // Binding
    //TODO vfx
    private Action<CustomEventArgs.CommomActionArgs>[] Spell07(ActionTrigger pTargetTrigger)
    {
        #region <Spell07/Methods/Init>

        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];

        #endregion

        #region <Spell07/Methods/Clicked>

        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(1, resettable: true);
        };

        #endregion

        #region <Spell07/Methods/Birth>
        eventGroup[(int)EventInfo.Birth] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            lChampion.SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(7.5f, 0.0f, .0f)));
            if (other.Candidate != null)
            {
                lChampion.UnitBoneAnimator.SetCast("Spell", 0, lChampion.CastSpeed);
                SoundManager.GetInstance
                    .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Serifu).SetTrigger();
            }
                
        };
        #endregion

        #region <Spell07/Methods/Enter>
        eventGroup[(int)EventInfo.Enter] = other =>
        {

        };
        #endregion

        #region <Spell07/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            var target = (Enemy)other.Candidate;
            if (target == null) return;
            if (!isMagicHandGrab)
            {
                var triggerPosition = target.Transform.position;
                var grabPosition = lChampion.Transform.position - lChampion.Transform.forward + lChampion.Transform.right + lChampion.Transform.up;

                var grabVector = grabPosition - triggerPosition;

                target.Hurt(lChampion, 0, TextureType.Magic, Vector3.zero);
                target.AddCrowdControl(new CrowdControl(target, CrowdControl.CrowdControlType.Stun, "Stun", 8, true)
                    .SetAction(CrowdControl.EventType.OnBirth,
                                new CustomEventArgs.CrowdControlArgs()
                                .SetCandidate(target)
                                .SetCaster(lChampion),
                                (args) =>
                                {
                                    args.Candidate.UnitBoneAnimator.UnityAnimator.speed = .0f;
                                    args.Candidate.ResetSpellColliderActive();
                                }
                    )
                    .SetAction(CrowdControl.EventType.OnTerminate,
                                new CustomEventArgs.CrowdControlArgs()
                                    .SetCaster(lChampion)
                                    .SetCandidate(target),
                                (args) =>
                                {
                                    args.Candidate.UnitBoneAnimator.UnityAnimator.speed = 1.0f;
                                }
                ));
                ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.HuntressArrowBlack, lChampion, 0.2f, triggerPosition)
                    .SetVelocity(grabVector * 5)
                    .SetProjectileType(Projectile.ProjectileType.Point)
                    .SetDirection()
                    .SetIgnoreUnit(true)
                    .SetIgnoreObstacle(true)
                    .SetActive(true)
                    .SetExpiredAction(args =>
                    {
                        target.gameObject.tag = "Projectile";
                        target.gameObject.layer = 12;
                        grabedUnit = ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.HuntressArrowBlack, lChampion, (int)MagicHand_Info[(int)Info.life_time], grabPosition)
                            .SetProjectileType(Projectile.ProjectileType.Box)
                            .SetMaxColliderNumber(10)
                            .SetColliderBox(MagicHand_Range)
                            .SetCollideUnitAction(collided => {
                                var proj = (Projectile)collided.MorphObject;
                                foreach (var collidedUnit in proj.CollidedUnitGroup)
                                {
                                    collidedUnit.Hurt(lChampion, (int)MagicHand_Info[(int)Info.damage], TextureType.Heavy, proj.Velocity.normalized,
                                    (caster, candidate, forceVecter) =>
                                    {
                                        candidate.AddForce(forceVecter * 2.0f);
                                    });
                                    target.Hurt(lChampion, (int)MagicHand_Info[(int)Info.damage], TextureType.Heavy, Vector3.zero);
                                    if(target.Hp <= 0)
                                    {
                                        target.gameObject.tag = "Enemy";
                                        target.gameObject.layer = 11;
                                        proj.Remove();
                                        target.CrowdControlGroup.Clear();
                                        isMagicHandGrab = false;
                                    }
                                }
                            })
                            .SetIgnoreObstacle(true)
                            .SetExpiredAction(expired => {
                                target.gameObject.tag = "Enemy";
                                target.gameObject.layer = 11;
                                target.CrowdControlGroup.Clear();
                                isMagicHandGrab = false;
                            })
                            .SetActive(true);
                        
                    });

                //merge후 fixedupdate 기반으로 따라다니게만듬
                target.Transform.position = grabPosition;
                isMagicHandGrab = true;
            }
            else
            {
                var triggerPosition = grabedUnit.Transform.position;
                var targetTransform = target.Transform;
                
                grabedUnit
                    .SetEnqueuePoint(triggerPosition)
                    .SetEnqueuePoint(triggerPosition + targetTransform.right * 5)
                    .SetEnqueuePoint(targetTransform.position + targetTransform.right * 5)
                    .SetEnqueuePoint(targetTransform.position)
                    .SetEnqueuePoint(targetTransform.position)
                    .SetEnqueuePoint(targetTransform.position - targetTransform.right * 5)
                    .SetEnqueuePoint(triggerPosition - targetTransform.right * 5)
                    .SetEnqueuePoint(triggerPosition)
                    .SetBezier(4, 0.3f)
                    .SetBezier(4, 0.3f)
                    .SetActive(true);
                
                grabedUnit.ExCollidedUnitGroup.Clear();
            }
            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <Spell07/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        return eventGroup;
    } // Magic Hand
    //TODO vfx
    private Action<CustomEventArgs.CommomActionArgs>[] Spell08(ActionTrigger pTargetTrigger)
    {
        #region <Spell08/Methods/Init>

        var eventGroup = new Action<CustomEventArgs.CommomActionArgs>[(int)EventInfo.Count];

        #endregion

        #region <Spell08/Methods/Clicked>

        eventGroup[(int)EventInfo.Clicked] = other =>
        {
            pTargetTrigger.Processtype = ActionTrigger.ProcessType.Simple;
            pTargetTrigger.Countertype = ActionTrigger.CounterType.TimeLerp;
            pTargetTrigger.UpdateCooldown(5, resettable: true);
        };

        #endregion

        #region <Spell08/Methods/Birth>

        eventGroup[(int)EventInfo.Birth] = other =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.UpdateTension();
            SetCurrentEventState(other.SetCandidate(lChampion.DetectAndChaseEnemyInRange(8.5f, 0f, 0f)));
            lChampion.UnitBoneAnimator.SetCast("Spell", 0, lChampion.CastSpeed);
            SoundManager.GetInstance
                .CastSfx(SoundManager.AudioMixerType.VOICE, lChampion.ChampionType, K514SfxStorage.ActivityType.Serifu).SetTrigger();
        };

        #endregion

        #region <Spell08/Methods/Enter>

        eventGroup[(int)EventInfo.Enter] = other =>
        {
        };

        #endregion

        #region <Spell08/Methods/Exit>
        eventGroup[(int)EventInfo.Exit] = (other) =>
        {
            var lChampion = (Champion)other.Caster;

            var triggerPosition = lChampion.AttachPoint[(int)AttachPointType.LeftHandIndex1].position;
            var direction = other.Candidate == null ?
                    lChampion.Transform.TransformDirection(Vector3.forward).normalized
                    : (new Vector3(other.Candidate.Transform.position.x, triggerPosition.y, other.Candidate.Transform.position.z) - triggerPosition).normalized;

            K514VfxManager.GetInstance.CastVfx(K514VfxManager.ParticleType.PFire, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();
            K514VfxManager.GetInstance.CastVfx(K514VfxManager.ParticleType.PEmber, triggerPosition)
                    .SetLifeSpan(.3f)
                    .SetForward(triggerPosition)
                    .SetScale(1f)
                    .SetTrigger();


            ProjectileManager.GetInstance.CreateProjectile(ProjectileManager.Type.PsyousMagmaBall, lChampion, BurningSheild_Info[(int)Info.life_time], triggerPosition)
                .SetVelocity(direction * BurningSheild_Info[(int)Info.speed])
                .SetDirection()
                .SetMaxColliderNumber((int)BurningSheild_Info[(int)Info.max_collider])
                .SetProjectileType(Projectile.ProjectileType.Box)
                .SetColliderBox(BurningSheild_Range)
                .SetNumberOfHitOnHeartBeat(4)
                .SetOnHeartBeatTension(4)
                .SetOnHeartBeatAction(args =>
                {
                    var subProj = (Projectile)args.MorphObject;
                    foreach (var collidedUnit in subProj.CollidedUnitGroup)
                    {
                        collidedUnit.Hurt(subProj.Caster, (int)BurningSheild_Info[(int)Info.damage], TextureType.Heavy, subProj.Direction,
                            (trigger, subject, forceDirection) =>
                            {
                                subject.AddForce(forceDirection * 6.0f);
                            });
                    }
                })
                .SetActiveHeartBeat(true)
                .SetActive(true);

            
            lChampion.ResetSpellColliderActive();
        };
        #endregion

        #region <Spell08/Methods/Terminate>
        eventGroup[(int)EventInfo.Terminate] = (other) =>
        {
            var lChampion = (Champion)other.Caster;
            lChampion.ResetFromCast();
        };
        #endregion

        return eventGroup;
    } // BurningSheild

    #endregion </Spell/Methods>
    */
}
