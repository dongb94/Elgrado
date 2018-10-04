using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SkeletonOneHanded : Enemy
{
    #region <Fields>

    private Trigger _trigger;

    #endregion

    #region <Enums>

    private enum Trigger
    {
        NormalAction01,
        NormalAction02,
        NormalAction03,
        
        Count
    }

    private enum SpellCollider
    {
        WeaponHanded,
        OtherHanded,
        
        Count
    }
    
    #endregion </Enums>
    
    #region <Callbacks>
    
//    public override void OnObjectTriggerEnterUnit(Unit collidedUnit)
//    {
//        if (UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Cast) return;
////            || CollidedUnitGroup.Exists(collidedUnitCandidate => collidedUnitCandidate == collidedUnit)) return;
//
//        switch (_trigger)
//        {
//            case Trigger.NormalAction01:
//            case Trigger.NormalAction02:
//            case Trigger.NormalAction03:
//                collidedUnit.Hurt(this, Power, TextureType.Medium, GetNormDirectionToMove(collidedUnit),
//                    (trigger, subject, forceVector) =>
//                    {
//                        subject.AddForce(forceVector);
//                    });
//                break;
//            case Trigger.Count:
//            default:
//                throw new ArgumentOutOfRangeException();
//        }
//    }
//
//    public override void OnObjectTriggerExitUnit(Unit collidedUnit)
//    {
//        if (UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Cast) return;
//    }

    public override void OnCastAnimationStandby()
    {
        throw new NotImplementedException();
    }

    public override void OnCastAnimationCue()
    {
        throw new NotImplementedException();
    }
    
    public override void OnCastAnimationCue2()
    {
        throw new NotImplementedException();
    }
    
    public override void OnCastAnimationCue3()
    {
        throw new NotImplementedException();
    }
    
    public override void OnCastAnimationCue4()
    {
        throw new NotImplementedException();
    }

    public override void OnCastAnimationExit()
    {
        switch (_trigger)
        {
            case Trigger.NormalAction01:
            case Trigger.NormalAction02:
            case Trigger.NormalAction03:
                UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Idle);
                break;
            default:
                break;
        }
    }

    public override void OnCastAnimationEnd()
    {
    }

    public override void OnIdleRelax() {}
    
    protected override void OnDeath()
    {
        SoundManager.GetInstance.CastSfx(SoundManager.AudioMixerType.VOICE,K514SfxStorage.EnemyType.Skeleton,K514SfxStorage.ActivityType.Dead).SetTrigger();
        base.OnDeath();
    }

    #endregion

    #region <Methods>

    protected override void AttackTrigger()
    {
        SoundManager.GetInstance.CastSfx(SoundManager.AudioMixerType.VOICE,K514SfxStorage.EnemyType.Skeleton,K514SfxStorage.ActivityType.Attack).SetTrigger();
        base.AttackTrigger();

        var normalAttackIndex = Random.Range(0, (int) Trigger.Count);
        UnitBoneAnimator.SetCast("Normal-Action", normalAttackIndex);
    }

    #endregion </Methods>
    
}