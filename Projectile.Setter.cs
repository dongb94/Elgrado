using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Projectile{
    
    public Projectile SetParticle(K514VfxParticle pVfx)
    {
        _mParticle = pVfx;
        
        return this;
    }

    public Projectile SetType(ProjectileManager.Type type)
    {
        Type = type;

        return this;
    }

    public Projectile SetLifeTime(float lifeTime)
    {
        if (lifeTime < 0f) return this;
        LifeTime = lifeTime;
        
        return this;
    }

    public Projectile SetRemoveDelay(float pDelay)
    {
        if (pDelay < 0f) return this;
        RemoveDelay = pDelay;
        
        return this;
    }
    
    public Projectile SetPierce(int pPierceCount)
    {
        if (pPierceCount < 0f) return this;
        IsPierce = pPierceCount;
        
        return this;
    }

    public Projectile SetArrowLeave(bool pLeave)
    {
        IsArrowLeave = pLeave;
        
        return this;
    }

    public Projectile SetVelocity(Vector3 velocity)
    {
        Velocity = velocity;

        return this;
    }

    public Projectile SetAcceleration(Vector3 acceleration)
    {
        Acceleration = acceleration;

        return this;
    }

    public Projectile SetDirection(Vector3 direction)
    {
        _Transform.forward = direction.normalized;

        return this;
    }
    
    public Projectile SetDirection()
    {
        _Transform.forward = Velocity.normalized;

        return this;
    }

    public Projectile SetColliderBox(Vector3 halfExtents)
    {
        _halfExtents = halfExtents;                       

        return this;
    }

    public Projectile SetMaxColliderNumber(int maxColliderNumber)
    {
        if (_maxColliderNumber >= maxColliderNumber) return this;
        
        ColliderGroup = new Collider[maxColliderNumber];
        _maxColliderNumber = maxColliderNumber;

        return this;
    }

    public Projectile SetCollideUnitAction(Action<CustomEventArgs.CommomActionArgs> action)
    {
        _collideUnitAction += action;
        
        return this;
    }

    public Projectile SetCollidedObstacleAction(Action<CustomEventArgs.CommomActionArgs> action)
    {
        _collideObstacleAction += action;

        return this;
    }
    
    public Projectile SetExpiredAction(Action<CustomEventArgs.CommomActionArgs> action)
    {
        _expiredLifeAction += action;

        return this;
    }
    
    public Projectile SetUpdateAction(Action<CustomEventArgs.CommomActionArgs> action)
    {
        _updateAction += action;

        return this;
    }
        
    public Projectile SetRemoveAction(Action action)
    {
        _removeAction += action;

        return this;
    }

    public Projectile SetActive(bool activate)
    {
        _isActive = activate;
        if (_isActive && _mParticle!=null) _mParticle.SetLifeSpan(1000f).SetParent(_Transform).SetTrigger(); 
        // Assert.IsFalse(Type == ProjectileManager.Type.None 
        //                || LifeTime <= Mathf.Epsilon
        //                || _maxColliderNumber <= 0);

        return this;
    }

    public Projectile SetProjectileType(int type)
    {
        _projectileType = type;

        return this;
    }

    public Projectile SetNumberOfHit(int numberOfHit)
    {
        _maxNumberOfHitTime = numberOfHit;

        return this;
    }

    public Projectile SetCaster(Unit pCaster)
    {
        _caster = pCaster;
        return this;
    }
    
    public Unit GetCaster(){
        return _caster;
    }

}