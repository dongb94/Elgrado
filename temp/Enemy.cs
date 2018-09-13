using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Unit
{

    #region <Consts>

    private const float IgnoreFocusUpdateTime = 1.5f;
    private const float CampReachConsiderateFactor = 1.5f;
    private const float SqrCampReachConsiderateFactor = CampReachConsiderateFactor * CampReachConsiderateFactor;    
    
    #endregion </Consts>   
    
    #region <Fields>

    public bool IsCampUnit;
    public bool IsHadInitialAttackCooldown;
    public bool IsCanceledAttackWhenItHurt;
    public int AttackCooldown;
    public float FocusRadius;
    public float FocusReleaseRadius;
    
    public K514SfxStorage.EnemyType EnemyType;
    public float DistanceTowardPlayer { get; private set; }
    public float DistanceTowardBase { get; private set; }

    [NonSerialized] public Activity UnitActivity;
    [NonSerialized] public Vector3 CampingPosition;
    [NonSerialized] public int AttackCooldownLeft;    
        
    private GameObject _healthBar;
    private Quaternion _rotation;
    private UIEnemyStateView _uiEnemyStateView;
    private NavMeshAgent _navMeshAgent;

    private float _ignoreFocusTime;
//    private Spawner _spawner;
    private bool _isFocused;
    
    private static Champion Focus
    {
        get { return PlayerManager.GetInstance.PlayerChampion; }
    }
    
    #endregion </Fields>

    #region <Enums>
   
    public enum Activity
    {
        Hunt,
        Rest,
        Returntocamp
    }
    
    #endregion </Enums>

    #region <Unity/Callbacks>

    /// <inheritdoc />
    /// <summary>
    /// Caching the components.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
//        _uiEnemyStateView = GetComponent<UIEnemyStateView>();
//        _spawner = GetComponentInParent<Spawner>();
        _ignoreFocusTime = .0f;
        FocusRadius *= FocusRadius;
        FocusReleaseRadius *= FocusReleaseRadius;
        AttackRange *= AttackRange;
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Work on activity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not expected parameter.</exception>
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        if (Focus == null
            || State == UnitState.Dead 
            || UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Hit
            || UnitBoneAnimator.CurrentState == BoneAnimator.AnimationState.Cast) return;
        
        UpdateFocus();
        
        /*TickAction();*/
        switch (UnitActivity)
        {
            case Activity.Hunt:
                Hunt();
                break;
            case Activity.Rest:
                Rest();
                break;
            case Activity.Returntocamp:
                ReturnToCamp();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Move) return;
        
        switch (UnitActivity)
        {
            case Activity.Hunt:
                if(!IsInAttackRange)
                    Move(_navMeshAgent.destination = Focus.transform.position);
                break;
            case Activity.Returntocamp:
                Move(_navMeshAgent.destination = CampingPosition);
                break;
            case Activity.Rest:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    #endregion </Unity/Callbacks>    
    
    #region <Callbacks>
    
    protected override void OnDeath()
    {
        Destroy(_navMeshAgent);
        SoundManager.GetInstance.CastSfx(SoundManager.AudioMixerType.VOICE,EnemyType,K514SfxStorage.ActivityType.Dead).SetTrigger();
        base.OnDeath();
    }  
    
    public override void OnHeartBeat()
    {
        base.OnHeartBeat();                
        
        if (State == UnitState.Dead)
        {
            if (DecayTimeLeft <= 0)
            {
                MaterialApplier.RevertTrigger();
                ObjectManager.RemoveObject(gameObject);
            }

            return;
        }
        
        if (UnitActivity == Activity.Hunt && IsInAttackRange)
        {
            TryAttack();
        }
    }

    public override void OnCreated()
    {
        base.OnCreated();
        
        _isFocused = false;
        _healthBar = null;
        AttackCooldownLeft = AttackCooldown;
        UnitActivity = Activity.Rest;
        UnitBoneAnimator.Initialize();
    }

//    public override void OnRemoved()
//    {
//    }

    #endregion

    #region <Properties>

    protected virtual bool IsReadyToAttack
    {
        get { return AttackCooldownLeft <= 0 
                     && UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Cast; }
    }
    
    private bool IsInAttackRange
    {
        get { return DistanceTowardPlayer < AttackRange; }
    }
    
    #endregion </Properties>
    
    #region <UpdateBehaviours/Methods>
    
    protected void Hunt()
    {
        // When it's not in release condition,
        if (_isFocused)
        {
            // Is satisfied with the attack range for trying this attack?
            if (IsInAttackRange)
            {
                SetAngleToDestination(GetNormDirectionToMove(Focus));
                _Transform.eulerAngles = Vector3.up * AngleToDestination;
            }
            // If not satisfied the above condition,
            else
            {                
                SwitchActivity(Activity.Hunt);

                if(!IsInAttackRange)
                    Move(_navMeshAgent.destination = Focus.transform.position);

                /*
                 * before code
                 * 
                Move(GetNormDirectionToMove(Focus));
                */
            }
        }
        else
        {
            SwitchActivity(!IsCampUnit ? Activity.Rest : Activity.Returntocamp);
        }
    }
    
    protected void Rest()
    {
        if (Tension > 0)
            --Tension;
    }

    protected void ReturnToCamp()
    {                      
        // Sync the height coordinate.                
        if (DistanceTowardBase <= SqrCampReachConsiderateFactor)
        {
            SwitchActivity(Activity.Rest);
        }        
    }        
    
    #endregion </UpdateBehaviours/Methods>
    
    #region <Methods>
    
    public override void Move(Vector3 forceVector)
    {
        base.Move(GetNormDirectionToMove(forceVector));
        if (UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Move)
            UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Move);
    }
    

    public override void Hurt(Unit caster, int damage, TextureType type, Vector3 forceDirection, 
        Action<Unit, Unit, Vector3> action = null, bool isCancelCast = true)
    {                
        base.Hurt(caster, damage, type, forceDirection, action, isCancelCast);                

        if (State == UnitState.Lives)
        {
            SoundManager.GetInstance.CastSfx(SoundManager.AudioMixerType.VOICE,EnemyType,K514SfxStorage.ActivityType.Dead).SetTrigger();
            UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Hit);
        }

        // @Temp: when it attacked in out of range, it would be tracking on you until you're in dead.
        IsCampUnit = false;
        _isFocused = true;
        SwitchActivity(Activity.Hunt);

        if (State == UnitState.Dead)
        {
            forceDirection *= (Math.Abs(Hp) + 1) * 3;
            AddForce(forceDirection);
        }                
        
        if (_uiEnemyStateView != null)        
            _uiEnemyStateView.UpdateState();
                
        if (IsCanceledAttackWhenItHurt && isCancelCast)
            SetAttackCooldown(AttackCooldown);
    }
    
    protected virtual void TryAttack()
    {
        if (IsReadyToAttack)
        {
            AttackTrigger();
        }
        else
        {
            if (UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Cast 
                && UnitBoneAnimator.CurrentState != BoneAnimator.AnimationState.Hit)
                SetAttackCooldown(AttackCooldownLeft - 1);            
        }
    }
    
    protected virtual void AttackTrigger()
    {
        SetAttackCooldown(AttackCooldown);
    }

    /// <summary>
    /// Switch the enemy state named "activity".
    /// </summary>
    /// <param name="activity">Used to change this activity.</param>
    /// <exception cref="ArgumentOutOfRangeException">Not expected parameter.</exception>
    private void SwitchActivity(Activity activity)
    {
        
        if (UnitActivity == activity && State == UnitState.Lives) return;
                
        switch (activity)
        {
            case Activity.Hunt:
                // TODO: Move this management statement to hud manager or ui root.            
                if (_healthBar != null) return;
                
                // Distance, this enemy with the focusing target
                var distance = Vector3.Distance(GetUnitOrthographicPosition, Focus.GetUnitOrthographicPosition);
                // @TODO<Carey>: Fix the margin vector3.up to be a detail factor.
                // Direction, catch the player's location
                var direction = Focus.GetUnitPosition - GetUnitPosition + Vector3.up;

                var isInBetweenWall =
                    Physics.Raycast(_Transform.position + Vector3.up, direction, distance, (1 << 1));
                
                if (isInBetweenWall) return;               
            
                _healthBar = ObjectManager.GetInstance.GetObject(ObjectManager.PoolTag.General, 
                    HUDManager.GetInstance.EnemyHealthBarPrefab, null, GameManager.GetInstance.UIRootTransform);
                _uiEnemyStateView = _healthBar.GetComponent<UIEnemyStateView>();
                _uiEnemyStateView.SetTrigger(this);
                
                // @Temp<Carey>: Replace this with what based on a serialized parameter.
                UpdateTension();

                break;
            case Activity.Rest:
                if (IsHadInitialAttackCooldown)
                    SetAttackCooldown(AttackCooldown);

                if (_healthBar != null)
                {
                    ObjectManager.RemoveObject(_healthBar);
                    _healthBar = null;
                }

                UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Idle);

                break;
                
            case Activity.Returntocamp:
                _ignoreFocusTime = IgnoreFocusUpdateTime;
                
                if (IsHadInitialAttackCooldown)
                    SetAttackCooldown(AttackCooldown);

                if (_healthBar != null)
                {
                    ObjectManager.RemoveObject(_healthBar);
                    _healthBar = null;
                }

                break;

            default:
                throw new ArgumentOutOfRangeException("activity", activity, null);
        }       
        
        UnitActivity = activity;
    }
    
    private void UpdateFocus()
    {
        DistanceTowardPlayer = MathVector.SqrDistance(Focus.GetUnitOrthographicPosition, GetUnitOrthographicPosition);
        DistanceTowardBase = MathVector.SqrDistance(CampingPosition, GetUnitPosition);
        
        if (!_isFocused)
        {       
            if (DistanceTowardPlayer > FocusRadius) return;
            if (_ignoreFocusTime > Mathf.Epsilon)
            {
                _ignoreFocusTime -= Time.fixedDeltaTime;
                return;
            }

            _isFocused = true;
            SwitchActivity(Activity.Hunt);
        }
        else
        {
            if (IsCampUnit && DistanceTowardBase > FocusReleaseRadius)
            {
                _isFocused = false;
                SwitchActivity(Activity.Returntocamp);
            }
            if(IsInAttackRange)
            {
                _navMeshAgent.enabled = false;
                if(UnitBoneAnimator.CurrentState!= BoneAnimator.AnimationState.Idle)
                    UnitBoneAnimator.SetTrigger(BoneAnimator.AnimationState.Idle);
            }
            else if(!_navMeshAgent.enabled)
            {
                _navMeshAgent.enabled = true;
            }
        }
    }

    private void SetAttackCooldown(int value)
    {
        AttackCooldownLeft = Math.Max(0, value);
        if (_uiEnemyStateView != null) _uiEnemyStateView.UpdateState();
    }
    
    #endregion </Methods>
    
}