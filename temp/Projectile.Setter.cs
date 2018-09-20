using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public partial class Projectile{
    
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

    public Projectile SetRemoveDelay(float pDelay, bool p_Flag = false)
    {
        if (pDelay < 0f) return this;
        _particleStopLoopWhenRemoveStartFlag = p_Flag;
        RemoveDelay = pDelay;
        
        return this;
    }
    
    public Projectile SetArrowFadeWholeTime(bool pLeave)
    {
        _arrowFadeWhenRemovingWholeFlag = pLeave;
        
        return this;
    }
    
    public Projectile SetArrowFadeWhenRemove(bool pLeave)
    {
        _arrowFadeWhenRemovingStartFlag = pLeave;
        
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

    public Projectile SetAcceleration(float pFactor)
    {
        AccelerationFactor = pFactor;

        return this;
    }

    public Projectile SetDirection(Vector3 direction)
    {
        _Transform.forward = direction.normalized;

        return this;
    }
    
    public Projectile SetDirection()
    {
        if(Velocity != Vector3.zero) _Transform.forward = Velocity.normalized;

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
        _maxColliderNumber = maxColliderNumber;

        return this;
    }

    public Projectile SetCollideUnitAction(Action<CustomEventArgs.CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _collideUnitAction = null;
        _collideUnitAction += action;
        
        return this;
    }

    public Projectile SetCollidedObstacleAction(Action<CustomEventArgs.CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _collideObstacleAction = null;
        _collideObstacleAction += action;

        return this;
    }
    
    public Projectile SetExpiredAction(Action<CustomEventArgs.CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _expiredLifeAction = null;
        _expiredLifeAction += action;

        return this;
    }
    
    public Projectile SetUpdateAction(Func<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs,Func<float,float,float,float,float>,Vector3> action, 
        float p_Duration, int p_NumToReadQueueData = 1, bool p_VariableFlag = false,
        Action<CustomEventArgs.CommomActionArgs, ProjectileAnimationEventArgs> p_InitAction = null, 
        Action<CustomEventArgs.CommomActionArgs, ProjectileAnimationEventArgs> p_ExpiredAction = null
        , Func<float,float,float,float,float> p_LerpFunction = null)
    {
        _projectileMoveActionSequenceList.Enqueue(new ProjectileAnimationEventArgs()
            .SetProjectile(this)
			.SetInitAction(p_InitAction)
            .SetMoveAction(action)
            .SetLerpFunction(p_LerpFunction)
            .SetExpiredAction(p_ExpiredAction)
            .SetDuration(p_Duration)
            .SetNumberToReadQueue(p_NumToReadQueueData)
            .SetVariableFlag(p_VariableFlag)
            .SetActive()
        );
        return this;
    }
        
    public Projectile SetRemoveStartAction(Action<CustomEventArgs.CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _onRemoveStart = null;
        _onRemoveStart += action;

        return this;
    }
    
    public Projectile SetWhileRemovingAction(Action<CustomEventArgs.CommomActionRefs> action, bool isStackAction = true)
    {
        if (!isStackAction) _onWhileRemovingAction = null;
        _onWhileRemovingAction += action;
        
        return this;
    }
       
    public Projectile SetRemoveEndAction(Action<CustomEventArgs.CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _onRemoveEnd = null;
        _onRemoveEnd += action;
        
        return this;
    }
    
    public Projectile SetRecursive(int pRecursiveKey)
    {
        RecursiveKey = pRecursiveKey;

        return this;
    }
    
    public Projectile SetOnHeartBeatAction(Action<CustomEventArgs.CommomActionArgs> action)
    {
        _onHeartBeatAction += action;        

        return this;
    }

        
    public Projectile SetIgnoreObstacle(bool pFlag)
    {
        _isIgnoreObstacle = pFlag;

        return this;
    }
    
    public Projectile SetIgnoreUnit(bool pFlag)
    {
        _isIgnoreUnit = pFlag;

        return this;
    }

    public Projectile SetGravity(float pGravity)
    {
        Gravity = pGravity;

        return this;
    }

    public Projectile SetEnqueuePoint(Vector3 p_Point)
    {
        _moveActionSequenceValueSet.Add(p_Point);
        return this;
    }

    public Projectile SetBezier(int pPointNumber, float p_Duration, Action<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction = null, 
        Action<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction = null, Func<float,float,float,float,float> p_LerpFunction = null)
    {
        p_ExpiredAction += (customArgs,projectileAnimationEventArgs) =>
        {
            var proj = (Projectile) customArgs.MorphObject;
            var cnt = projectileAnimationEventArgs.NumberToReadQueue;
            while (cnt > 0)
            {
                proj._moveActionSequenceValueSet.RemoveAt(0);
                cnt--;
            }
        };
        
        SetUpdateAction( (customArgs,projectileAnimationEventArgs,lerpFunction) =>
            {
                var proj = (Projectile) customArgs.MorphObject;
                var elapsed = proj._currentMoveActionCumulativeTime - ElapsedTime;
                var reversedDuration = 1f / proj._currentMoveAction.AnimationDuration;
                var stepSize = projectileAnimationEventArgs.NumberToReadQueue;
                for (var i = 0; i < stepSize; i++)
                {
                    proj.CopiedBezierPointSet[i] = proj._moveActionSequenceValueSet[i];
                }

                if (lerpFunction == null)
                {
                    for (int j = stepSize - 1; j > 0; j--)
                    {
                        for (int i = 0; i < j; i++)
                        {
                            proj.CopiedBezierPointSet[i].x = K514MathManager.ReverseLinearLerp(
                                proj.CopiedBezierPointSet[i].x,
                                proj.CopiedBezierPointSet[i + 1].x - proj.CopiedBezierPointSet[i].x, elapsed,
                                reversedDuration);
                            proj.CopiedBezierPointSet[i].y = K514MathManager.ReverseLinearLerp(
                                proj.CopiedBezierPointSet[i].y,
                                proj.CopiedBezierPointSet[i + 1].y - proj.CopiedBezierPointSet[i].y, elapsed,
                                reversedDuration);
                            proj.CopiedBezierPointSet[i].z = K514MathManager.ReverseLinearLerp(
                                proj.CopiedBezierPointSet[i].z,
                                proj.CopiedBezierPointSet[i + 1].z - proj.CopiedBezierPointSet[i].z, elapsed,
                                reversedDuration);
                        }
                    }
                }
                else
                {
                    for (int j = stepSize - 1; j > 0; j--)
                    {
                        for (int i = 0; i < j; i++)
                        {
                            proj.CopiedBezierPointSet[i].x = lerpFunction(
                                proj.CopiedBezierPointSet[i].x,
                                proj.CopiedBezierPointSet[i + 1].x - proj.CopiedBezierPointSet[i].x, elapsed,
                                reversedDuration);
                            proj.CopiedBezierPointSet[i].y = lerpFunction(
                                proj.CopiedBezierPointSet[i].y,
                                proj.CopiedBezierPointSet[i + 1].y - proj.CopiedBezierPointSet[i].y, elapsed,
                                reversedDuration);
                            proj.CopiedBezierPointSet[i].z = lerpFunction(
                                proj.CopiedBezierPointSet[i].z,
                                proj.CopiedBezierPointSet[i + 1].z - proj.CopiedBezierPointSet[i].z, elapsed,
                                reversedDuration);
                        }
                    }  
                }

                return proj.CopiedBezierPointSet[0];
            }
            ,p_Duration
            ,pPointNumber
            ,false
            ,p_InitAction
            ,p_ExpiredAction
            ,p_LerpFunction
        );
        
        return this;
    }

    public Projectile SetHelix(bool p_IsClockWise, float p_Duration, Vector3 p_TargetPosition, float p_AngularVelocity, float p_HorizontalVelocity, float p_InwardVelocity = 0f, 
        Action<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction = null, Action<CustomEventArgs.CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction = null)
    {
        p_ExpiredAction += (customArgs,projectileAnimationEventArgs) =>
        {
            var proj = (Projectile) customArgs.MorphObject;
            var cnt = projectileAnimationEventArgs.NumberToReadQueue;
            while (cnt > 0)
            {
                proj._moveActionSequenceValueSet.RemoveAt(0);
                cnt--;
            }
        };
        
        SetEnqueuePoint(p_TargetPosition)
        .SetEnqueuePoint(new Vector3(p_AngularVelocity, p_HorizontalVelocity, p_InwardVelocity))
        .SetUpdateAction( (customArgs,projectileAnimationEventArgs,lerpFunction) =>
            {
                var proj = (Projectile) customArgs.MorphObject;
                var rotationPivot = projectileAnimationEventArgs.VariableUseFlag ? Vector3.up : Vector3.down;
                var lastPosition = proj._Transform.position;
                var tuple3 = proj._moveActionSequenceValueSet[1];
                proj._Transform.RotateAround(proj._moveActionSequenceValueSet[0],rotationPivot,tuple3.x*Time.fixedDeltaTime);
                var result = proj._Transform.position;
                proj._Transform.position = lastPosition;
                result += Vector3.up * tuple3.y * Time.fixedDeltaTime;
                var directionToCaster = (proj._moveActionSequenceValueSet[0] - proj._Transform.position).normalized;
                return result + directionToCaster * tuple3.z * Time.fixedDeltaTime;
            }
            ,p_Duration
            ,2
            ,p_IsClockWise
            ,p_InitAction
            ,p_ExpiredAction);
        
        return this;
    }

    public Projectile SetActive(bool activate)
    {
        _isActive = activate;
        // Assert.IsFalse(Type == ProjectileManager.Type.None 
        //                || LifeTime <= Mathf.Epsilon
        //                || _maxColliderNumber <= 0);

        return this;
    }

    public Projectile SetActiveHeartBeat(bool activate)
    {
        _isOnHeartBeat = activate;
        return this;
    }


    public Projectile SetProjectileType(ProjectileType type)
    {
        _projectileType = type;

        return this;
    }

    public Projectile SetNumberOfHit(int numberOfHit)
    {
        _maxNumberOfHitTime = numberOfHit;

        return this;
    }

    public Projectile SetOnHeartBeatTension(int tension, int term = 0)
    {
        _onHeartBeatEventTension = tension;
        _onHeartBeatEventTerm = _onHeartBeatEventCounter = term;

        return this;
    }

    public Projectile SetCaster(Unit pCaster)
    {
        Caster = pCaster;
        return this;
    }
}