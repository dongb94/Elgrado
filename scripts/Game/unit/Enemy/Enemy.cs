using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Computer
{
    #region <Fields>

    public K514SfxStorage.EnemyType EnemyType;
    
    #endregion
    
    #region <Callbacks>
    
    #region <InstantiateEvent>

    public override void OnCreated()
    {
        base.OnCreated();
        inner_CampingPosition = Transform.position;
    }

    public override void OnDeath()
    {
        base.OnDeath();
        SoundManager.GetInstance.CastSfx(SoundManager.AudioMixerType.VOICE,EnemyType,K514SfxStorage.ActivityType.Dead).SetTrigger();
    }  
    
    #endregion    

    #endregion
    
    #region <Properties>
   
    public override Vector3 CampingPosition
    {
        get
        {
            return inner_CampingPosition;
        }
    }
    
    #endregion
    
    #region <Methods>

    public override void Hurt(Unit caster, int damage, TextureType type, Vector3 forceDirection, 
        Action<Unit, Unit, Vector3> action = null)
    {
        base.Hurt(caster, damage, type, forceDirection, action);
        
        if (State == UnitState.Lives)
        {
            SoundManager.GetInstance.CastSfx(SoundManager.AudioMixerType.VOICE,EnemyType,K514SfxStorage.ActivityType.Dead).SetTrigger();
        }
    }

    #endregion </Methods>
    
}