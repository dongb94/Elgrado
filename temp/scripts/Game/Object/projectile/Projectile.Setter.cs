using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public partial class Projectile{
    
    public Projectile SetType(ProjectileFactory.Type type)
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
        Transform.forward = direction.normalized;

        return this;
    }
    
    public Projectile SetDirection()
    {
        if(Velocity != Vector3.zero) Transform.forward = Velocity.normalized;

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

    public Projectile SetFixedUpdateAction(Action<CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _fixedUpdateAction = null;
        _fixedUpdateAction += action;

        return this;
    }

    public Projectile SetCollideUnitAction(Action<CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _collideUnitAction = null;
        _collideUnitAction += action;
        
        return this;
    }

    public Projectile SetCollidedObstacleAction(Action<CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _collideObstacleAction = null;
        _collideObstacleAction += action;

        return this;
    }
    
    public Projectile SetExpiredAction(Action<CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _expiredLifeAction = null;
        _expiredLifeAction += action;

        return this;
    }
    
    public Projectile SetUpdateAction(Func<CommomActionArgs,ProjectileAnimationEventArgs,Func<float,float,float,float,float>,Vector3> action, 
        float p_Duration, int p_NumToReadQueueData = 1, bool p_VariableFlag = false,
        Action<CommomActionArgs, ProjectileAnimationEventArgs> p_InitAction = null, 
        Action<CommomActionArgs, ProjectileAnimationEventArgs> p_ExpiredAction = null
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
        
    public Projectile SetRemoveStartAction(Action<CommomActionArgs> action, bool isStackAction = true)
    {
        if (!isStackAction) _onRemoveStart = null;
        _onRemoveStart += action;

        return this;
    }
    
    public Projectile SetWhileRemovingAction(Action<CommomActionRefs> action, bool isStackAction = true)
    {
        if (!isStackAction) _onWhileRemovingAction = null;
        _onWhileRemovingAction += action;
        
        return this;
    }
       
    public Projectile SetRemoveEndAction(Action<CommomActionArgs> action, bool isStackAction = true)
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
    
    public Projectile SetOnHeartBeatAction(Action<CommomActionArgs> action)
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

    public Projectile SetJoin(Projectile p_Join)
    {
        _Join = p_Join;

        return this;
    }

    public Projectile SetBezier(int pPointNumber, float p_Duration, Action<CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction = null, 
        Action<CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction = null, Func<float,float,float,float,float> p_LerpFunction = null)
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
    
    public Projectile SetBezierChaseEnemy(int pPointNumber,Transform p_Target, float p_Duration, Action<CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction = null, 
        Action<CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction = null, Func<float,float,float,float,float> p_LerpFunction = null)
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
        
        SetChaseTarget(p_Target).
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
                    for (int j = stepSize; j > 0; j--)
                    {
                        for (int i = 0; i < j; i++)
                        {
                            if (i != j - 1)
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
                            else
                            {
                                proj.CopiedBezierPointSet[i].x = K514MathManager.ReverseLinearLerp(
                                    proj.CopiedBezierPointSet[i].x,
                                    proj._chasingTarget.position.x - proj.CopiedBezierPointSet[i].x, elapsed,
                                    reversedDuration);
                                proj.CopiedBezierPointSet[i].y = K514MathManager.ReverseLinearLerp(
                                    proj.CopiedBezierPointSet[i].y,
                                    proj._chasingTarget.position.y - proj.CopiedBezierPointSet[i].y, elapsed,
                                    reversedDuration);
                                proj.CopiedBezierPointSet[i].z = K514MathManager.ReverseLinearLerp(
                                    proj.CopiedBezierPointSet[i].z,
                                    proj._chasingTarget.position.z - proj.CopiedBezierPointSet[i].z, elapsed,
                                    reversedDuration);
                            }
                        }
                    }
                }
                else
                {
                    for (int j = stepSize; j > 0; j--)
                    {
                        for (int i = 0; i < j; i++)
                        {
                             if (i != j - 1)
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
                            else
                            {
                                proj.CopiedBezierPointSet[i].x = lerpFunction(
                                    proj.CopiedBezierPointSet[i].x,
                                    proj._chasingTarget.position.x - proj.CopiedBezierPointSet[i].x, elapsed,
                                    reversedDuration);
                                proj.CopiedBezierPointSet[i].y = lerpFunction(
                                    proj.CopiedBezierPointSet[i].y,
                                    proj._chasingTarget.position.y - proj.CopiedBezierPointSet[i].y, elapsed,
                                    reversedDuration);
                                proj.CopiedBezierPointSet[i].z = lerpFunction(
                                    proj.CopiedBezierPointSet[i].z,
                                    proj._chasingTarget.position.z - proj.CopiedBezierPointSet[i].z, elapsed,
                                    reversedDuration);
                            }
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
        Action<CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction = null, Action<CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction = null
        )
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
                var lastPosition = proj.Transform.position;
                var tuple3 = proj._moveActionSequenceValueSet[1];
                proj.Transform.RotateAround(proj._moveActionSequenceValueSet[0],rotationPivot,tuple3.x*Time.fixedDeltaTime);
                var result = proj.Transform.position;
                proj.Transform.position = lastPosition;
                result += Vector3.up * tuple3.y * Time.fixedDeltaTime;
                var directionToCaster = (proj._moveActionSequenceValueSet[0] - proj.Transform.position).normalized;
                return result + directionToCaster * tuple3.z * Time.fixedDeltaTime;
            }
            ,p_Duration
            ,2
            ,p_IsClockWise
            ,p_InitAction
            ,p_ExpiredAction);
        
        return this;
    }
    
    public Projectile SetSpiral(bool p_IsClockWise, float p_Duration, float p_AngularVelocity, float p_Radius = 0f, float p_ThetaOffset = 0f, float p_InwardVelocity = 0f, 
        Action<CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction = null, Action<CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction = null
    )
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
        
        p_InitAction += (customArgs,projectileAnimationEventArgs) =>
        {
            var proj = (Projectile) customArgs.MorphObject;
            proj._moveActionSequenceValueSet[0] = proj.Transform.position;
            var perpVec = Vector3.Cross(proj.Direction, Vector3.up).normalized;
            proj.Transform.position += perpVec * proj._moveActionSequenceValueSet[2].x;
            proj.Transform.RotateAround(proj._moveActionSequenceValueSet[0],proj.Direction,proj._moveActionSequenceValueSet[1].y);
        };
        
        SetEnqueuePoint(Vector3.zero)
            .SetEnqueuePoint(new Vector3(p_AngularVelocity, p_ThetaOffset, p_InwardVelocity))
            .SetEnqueuePoint(Vector3.one * p_Radius)
            .SetUpdateAction( (customArgs,projectileAnimationEventArgs,lerpFunction) =>
                {
                    var proj = (Projectile) customArgs.MorphObject;
                    var rotationPivot = projectileAnimationEventArgs.VariableUseFlag ? proj.Direction : proj.Direction * -1f;
                    var lastPosition = proj.Transform.position;
                    var tuple3 = proj._moveActionSequenceValueSet[1];
                    proj._moveActionSequenceValueSet[0] = proj.SimulateDefaultLinearProgressKinematic(proj._moveActionSequenceValueSet[0]);
                    proj.Transform.RotateAround(proj._moveActionSequenceValueSet[0],rotationPivot,tuple3.x*Time.fixedDeltaTime);
                    proj.Transform.position = proj.DefaultLinearProgressKinematic();
                    var result = proj.Transform.position;
                    proj.Transform.position = lastPosition;
                    var directionToCaster = (proj._moveActionSequenceValueSet[0] - proj.Transform.position).normalized;
                    return result + directionToCaster * tuple3.z * Time.fixedDeltaTime;
                }
                ,p_Duration
                ,3
                ,p_IsClockWise
                ,p_InitAction
                ,p_ExpiredAction);
        
        return this;
    }
    
    public Projectile SetChaser(float p_Duration, Transform p_ChaseTarget,
        Action<CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction = null, Action<CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction = null
        , Func<float,float,float,float,float> p_LerpFunction = null)
    {
        
        SetChaseTarget(p_ChaseTarget).
        SetUpdateAction( (customArgs,projectileAnimationEventArgs,lerpFunction) =>
                {
                    var proj = (Projectile) customArgs.MorphObject;
                    var elapsed = proj._currentMoveActionCumulativeTime - ElapsedTime;
                    var reversedDuration = 1f / proj._currentMoveAction.AnimationDuration;

                    if (p_LerpFunction == null)
                    {
                        return K514MathManager.LinearLerp(proj.Transform.position, proj._chasingTarget.position,
                            elapsed, reversedDuration);
                    }
                    else
                    {
                        var p_From = proj.Transform.position;
                        var p_To = proj._chasingTarget.position;
                        var distance = p_To - p_From;
                        return new Vector3(
                            p_LerpFunction(p_From.x, distance.x,  elapsed,  reversedDuration),
                            p_LerpFunction(p_From.y, distance.y,  elapsed,  reversedDuration),
                            p_LerpFunction(p_From.z, distance.z,  elapsed,  reversedDuration)
                        );
                    }
                }
                ,p_Duration
                ,0
                ,false
                ,p_InitAction
                ,p_ExpiredAction
                ,p_LerpFunction);
        
        return this;
    }

    public Projectile SetChaser(float p_Duration, Vector3 p_ChaseTarget,
        Action<CommomActionArgs,ProjectileAnimationEventArgs> p_InitAction = null, Action<CommomActionArgs,ProjectileAnimationEventArgs> p_ExpiredAction = null
        , Func<float,float,float,float,float> p_LerpFunction = null)
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

        SetEnqueuePoint(p_ChaseTarget)
            .SetUpdateAction( (customArgs,projectileAnimationEventArgs,lerpFunction) =>
                {
                    var proj = (Projectile) customArgs.MorphObject;
                    var elapsed = proj._currentMoveActionCumulativeTime - ElapsedTime;
                    var reversedDuration = 1f / proj._currentMoveAction.AnimationDuration;

                    if (p_LerpFunction == null)
                    {
                        return K514MathManager.LinearLerp(proj.Transform.position, proj._moveActionSequenceValueSet[0],
                            elapsed, reversedDuration);
                    }
                    else
                    {
                        var p_From = proj.Transform.position;
                        var p_To = proj._moveActionSequenceValueSet[0];
                        var distance = p_To - p_From;
                        return new Vector3(
                            p_LerpFunction(p_From.x, distance.x,  elapsed,  reversedDuration),
                            p_LerpFunction(p_From.y, distance.y,  elapsed,  reversedDuration),
                            p_LerpFunction(p_From.z, distance.z,  elapsed,  reversedDuration)
                        );
                    }
                }
                ,p_Duration
                ,1
                ,false
                ,p_InitAction
                ,p_ExpiredAction
                ,p_LerpFunction);
        
        return this;
    }
    
    public Projectile SetChaseTarget(Transform p_Target)
    {
        _chasingTarget = p_Target;

        return this;
    }
    
    public Projectile SetInvincibleCheck(bool p_Flag)
    {
        _IsCheckInvincible = p_Flag;
        
        return this;
    }

    public void SetParitcleComponentActive(bool pFlag)
    {
        for (var i = 0 ; i < _particleList.Length ; i++)
        {
            _particleList[i].gameObject.SetActive(pFlag);
        }
    }
    
    public Projectile SetTrigger(bool activate, float p_DefferOffest = 0f)
    {
        if (p_DefferOffest < Mathf.Epsilon)
        {
            _isActive = activate;
        }
        else
        {
            #region <PooledCoroutineForAsyncProjectilActive>
            _DefferedActivateTime = p_DefferOffest;
            var lContent = ObjectManager.GetInstance.GetObject<K514PooledCoroutine>(ObjectManager.PoolTag.Coroutine,
                K514PrefabStorage.GetInstance.GetPrefab(K514PrefabStorage.PrefabType.PooledCoroutine));
            lContent._mParams.SetMorphable(this);
            lContent
                .SetAction(K514PooledCoroutine.ActionType.Init,ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        proj.SetParitcleComponentActive(false);
                    }
                )
                .SetAction(K514PooledCoroutine.ActionType.EndTrigger,ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        return proj._DefferedActivateTime < ano.F_factor;
                    }
                )
                .SetInterval(Time.fixedDeltaTime)
                .SetAction(K514PooledCoroutine.ActionType.Activity, ano => { ano.SetFactor(ano.F_factor + Time.fixedDeltaTime); })
                .SetAction(K514PooledCoroutine.ActionType.Finalize, ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        // <K514> : there is no reason to reserving SetTrigger(false) deferred
                        proj.SetParitcleComponentActive(true);
                        proj.SetTrigger(true);
                    }
                )
                .SetTrigger();
            #endregion
        }
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