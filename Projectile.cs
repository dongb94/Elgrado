using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Projectile : MonoBehaviour, IInstantiated
{    
    
    #region <Fields>
     
    public bool IsActive { get { return _isActive; } }
    public ProjectileManager.Type Type { get; private set; }
    
    public Vector3 Velocity { get; private set; }
    public Vector3 Direction { get { return Velocity.normalized; }}
    public Vector3 Acceleration { get; private set; }
    
    public float LifeTime { get; private set; }      
    public Collider[] ColliderGroup { get; private set; }    
    public List<Collider> ExColliderGroup { get; private set; }
    public List<GameObject> CollidedObstacleGroup { get; private set; }
    public List<Unit> CollidedUnitGroup { get; private set; }
    public int CollidedUnitNumber { get; private set; }
    
    [NonSerialized] public Renderer _mRenderer;
    [SerializeField] private Vector3 _halfExtents;

    private Action<CustomEventArgs.CommomActionArgs> _collideUnitAction;
    private Action<CustomEventArgs.CommomActionArgs> _collideObstacleAction;
    private Action<CustomEventArgs.CommomActionArgs> _updateAction;
    private Action<CustomEventArgs.CommomActionArgs> _expiredLifeAction;
    private Action _removeAction;

    private K514VfxParticle _mParticle;
    private TrailRenderer _mTrail;
    private Unit _caster;
    private int _projectileType;
    private int _maxColliderNumber;
    private int _maxNumberOfHitTime;
    private bool _isActive;
	    
    public int UnitIdetifyKey { get; set; }
    public Transform _Transform { get; set; }

    /*extends*/
    [NonSerialized] public float RemoveDelay;
    [NonSerialized] public int RemoveDelayCount;
    [NonSerialized] public int IsPierce = 0;
    [NonSerialized] public bool IsArrowLeave;
    [NonSerialized] public bool RemoveDeadlock;


    #endregion </Fields>

    #region <Enum>
    public enum ProjectileType{
        Point,
        Box,
        Sphere,
    }

    #endregion

    #region <Unity/Callbacks>

    private void Awake()
    {
        Velocity = Vector3.zero;
        Acceleration = Vector3.zero;
        _Transform = GetComponent<Transform>();
        CollidedUnitGroup = new List<Unit>();
        ExColliderGroup = new List<Collider>();
        _maxColliderNumber = 0;
        _mTrail = GetComponentInChildren<TrailRenderer>();
        _mParticle = GetComponentInChildren<K514VfxParticle>();
        _mRenderer = GetComponentInChildren<Renderer>();
        ColliderGroup = new Collider[0];
    }    

    private void OnEnable()
    {
        _isActive = false;
        _collideUnitAction = null;
        _collideObstacleAction = null;
        _updateAction = null;
        _expiredLifeAction = null;
        _removeAction = null;
        
        Type = ProjectileManager.Type.None;
        CollidedUnitNumber = _maxColliderNumber = 0;
        CollidedUnitGroup.Clear();
        ExColliderGroup.Clear();

        _mRenderer.enabled = true;
        Velocity  = Acceleration = Vector3.zero;
        SetDirection(Vector3.forward);
        LifeTime = 0f;
        IsPierce = RemoveDelayCount = 0;
        RemoveDelay = 0f;
        _projectileType = 0;
        _maxNumberOfHitTime = 1;
    }

    private void FixedUpdate()
    {
        if (!_isActive) return;
        
        var center = _Transform.position;

        var hit = new RaycastHit();
        var isCollided = false;

        int raycast_mask = 1 << 1 | 1 << 13;
        //int raycast_mask = 1 << 1;  // for test

        switch (_projectileType)
        {
            case 0: //case point
                isCollided = Physics.Raycast(center - Direction, Direction, out hit, Velocity.magnitude * Time.fixedDeltaTime, raycast_mask);
                break;
            case 1: //case box
                isCollided = Physics.Raycast(center - Direction, Direction, out hit, Velocity.magnitude * Time.fixedDeltaTime + _halfExtents.z, raycast_mask);
                break;
            case 2: //case sphere
                isCollided = Physics.Raycast(center - Direction, Direction, out hit, Velocity.magnitude * Time.fixedDeltaTime + _halfExtents.magnitude, raycast_mask);
                break;
            default: //imposible to reached
                break;
        }
        // CASE#1: has been collided on any obstacle.
        if (isCollided)
        {
            // Defined Action: Collided Obstacle.
            if (_collideObstacleAction != null)
            {
                _collideObstacleAction(new CustomEventArgs.CommomActionArgs()
                    .SetMorphable(this)
                );
            }
            // Default Action: Collided Obstacle.
            else
            {
                Remove();
            }
            return;
        }
        switch (_projectileType)
        {
            case 0: //case point
                CollidedUnitNumber = Physics.OverlapCapsuleNonAlloc(center, center + Velocity * Time.fixedDeltaTime, 0.05f, ColliderGroup,  1 << 11);
                break;
            case 1: //case box
                CollidedUnitNumber = Physics.OverlapBoxNonAlloc(center, _halfExtents + new Vector3(0,0,Velocity.magnitude * Time.fixedDeltaTime), ColliderGroup, _Transform.rotation, 1 << 11);
                break;
            case 2: //case sphere
                CollidedUnitNumber = Physics.OverlapCapsuleNonAlloc(center, center + Velocity * Time.fixedDeltaTime, _halfExtents.magnitude, ColliderGroup,  1 << 11);
                break;
            default: //imposible to reached
                break;
        }
        CollidedUnitGroup.Clear();

        // Filtering the reentering and negative.
        for (var collidedUnitIndex = 0;
            collidedUnitIndex < ColliderGroup.Length && collidedUnitIndex < CollidedUnitNumber;
            ++collidedUnitIndex)
        {
            if (ExColliderGroup.Exists(exCollided => exCollided == ColliderGroup[collidedUnitIndex])) continue;
            
            var filterCollided = ColliderGroup[collidedUnitIndex];                
            ExColliderGroup.Add(filterCollided);
            
            var filteredCollidedUnit = filterCollided.GetComponent<Unit>();
            
            if (Filter.IsNegative(_caster, filteredCollidedUnit))
            {
                CollidedUnitGroup.Add(filteredCollidedUnit);
            }

            if (CollidedUnitGroup.Count >= _maxColliderNumber) break;
        }
        
        // CASE#2: has been collided on any negative unit.
        if (CollidedUnitGroup.Count > 0)
        {
            // Defined Action: Collided Unit.
            if (_collideUnitAction != null)
            {
                _collideUnitAction(new CustomEventArgs.CommomActionArgs()
                    .SetMorphable(this)
                );
            }            
            // Default Action: Collided Unit.
            else
            {
                Remove();
            }

            if(_maxNumberOfHitTime > 1)
            {
                _maxNumberOfHitTime--;
                ExColliderGroup.Clear();
            }
            
            return;
        }                
        
        // CASE#3: life time has been expired.
        if (LifeTime < .0f && _isActive)
        {
            // Defined Action: Expired.
            if (_expiredLifeAction != null)
            {
                _expiredLifeAction(new CustomEventArgs.CommomActionArgs()
                    .SetMorphable(this)
                );
            }
            // Default Action: Expired.
            else
            {
                Remove();
            }                                       

            return;
        }                

        
        if (_updateAction != null)
        {
            _updateAction(new CustomEventArgs.CommomActionArgs()
                .SetMorphable(this)
            );
        }
        else
        {
            _Transform.position += Velocity * Time.fixedDeltaTime;
            Velocity += Acceleration * Time.fixedDeltaTime;
            LifeTime -= Time.fixedDeltaTime;
        }
    }
    
    #endregion </Unity/Callbacks>
    
    #region <Callbacks>
    
    public void OnCreated() {}

    public void OnRemoved()
    {
        if (_mParticle != null)
        {
            _mParticle.SetRemove();
            _mParticle = null;
        }

        if (_mTrail != null)
        {
            _mTrail.Clear();
        }
    }
    
    #endregion </Callbacks>

    #region <Methods>

    public void Remove()
    {
        if (RemoveDeadlock) return;
        if (RemoveDelay == 0f)
        {
            ObjectManager.RemoveObject(gameObject);
        }
        else
        {
            RemoveDeadlock = true;
            _isActive = false;
            if (!IsArrowLeave) _mRenderer.enabled = false; 
            var lContent = ObjectManager.GetInstance.GetObject<K514PooledCoroutine>(ObjectManager.PoolTag.Coroutine,
                    K514PrefabStorage.GetInstance.GetPrefab(K514PrefabStorage.PrefabType.PooledCoroutine));
            lContent._mParams.SetMorphable(this);

            lContent.SetAction(ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        var result = 0.1f * proj.RemoveDelayCount > proj.RemoveDelay;
                        if (!result) proj.RemoveDelayCount++;
                        return result;
                    }
                )
                .SetInterval(0.1f, K514PooledCoroutine.DelayType.Interval)
                .SetAction(K514PooledCoroutine.ActionType.Finalize, ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        ObjectManager.RemoveObject(proj.gameObject);
                        proj.RemoveDeadlock = false;
                        if (!proj.IsArrowLeave) proj._mRenderer.enabled = false;
                    }
                );
        
            if (_removeAction != null)
            {
                lContent.SetAction(K514PooledCoroutine.ActionType.Activity, ano =>
                    {
                        var proj = (Projectile) ano.MorphObject;
                        proj._removeAction();
                    }
                );
            }

            lContent.SetTrigger();
        }
    }

    #endregion </Methods>

}