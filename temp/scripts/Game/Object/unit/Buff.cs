using System;
using System.Collections.Generic;

public class Buff
{
    #region <Consts>

    /// <summary>
    /// LifeSpan을 Infinity로 설정하면 해당 버프 효과는 특정 상황이 되지 않으면 무제한 지속된다.
    /// </summary>
    public const int Infinity = -1;
    
    #endregion </Consts>
    
    #region <Fields>

    private static readonly List<Buff> BuffStorage = new List<Buff>();
    
    public bool IsPositive { get; private set; }
    public int Lifespan { get; private set; }

    private Unit _owner;
    private _ParentType _parentType;
    private _Type _type;
    private readonly Action<BuffArgs>[] _buffActionGroup;

    #endregion </Fields>

    #region <Enums>
    
    /// <summary>
    /// [None] 기본적으로 유효성을 가지지 못한 Buff와 같다.
    /// </summary>
    public enum _ParentType
    {
        None,        
        Stun,
        Bleeding,
        CastSpeed,
        MovementSpeed,  
        Invincible,
        
        Count
    }

    public enum _Type
    {
        None,
        Stun,
        Bleeding,
        MoveSlow,
        CastSlow,
        BoneWall,
        
        Count
    }

    /// <summary>
    /// [Recent] 가장 마지막으로 생성된 Buff 타입이 덮어쓰는 방식
    /// [Race] LifeTime을 기준으로 더 오랜 LifeTime을 가지고 있는 얘로 덮어쓰는 방식
    /// [Merge] 가장 먼저 생성된 남아 있는 Buff 타입이 덮어쓰며, 생성하려는 Buff의 LifeTime을 유산으로 남긴다.
    /// </summary>
    public enum OverrideType
    {
        Recent,
        Race,
        Merge,
        
        Count        
    }

    public enum EventType
    {
        OnBirth,
        OnHeartBeat,
        OnTerminate,

        OnFixedUpdate,
        
        Count        
    }    
    
    #endregion </Enums>

    #region <Constructor>

    public Buff()
    {
        _buffActionGroup = new Action<BuffArgs>[(int) EventType.Count];
        
        Initialize();
    }
    
    private void Initialize()
    {        
        Owner = null;
        Lifespan = 0;
        ParentType = _ParentType.None;
        Type = _Type.None;
        BuffArgs = new BuffArgs();        
    }

    #endregion </Constructor>        
    
    #region <Callbacks>

    public void OnBirth()
    {
        _buffActionGroup[(int) EventType.OnBirth]?.Invoke(BuffArgs);        
    }

    /// <summary>
    /// Called by <see cref="Unit.FixedUpdate"/>.
    /// </summary>
    public void OnFixedUpdate()
    {
        _buffActionGroup[(int) EventType.OnFixedUpdate]?.Invoke(BuffArgs);
    }

    public void OnHeartBeat()
    {        
        if (Lifespan == 0)
        {
            OnTerminate();
            return;
        }

        if (Lifespan != Infinity)
            --Lifespan;
        
        _buffActionGroup[(int) EventType.OnHeartBeat]?.Invoke(BuffArgs);
    }

    public void OnTerminate()
    {               
        Owner = null;
        
        _buffActionGroup[(int) EventType.OnTerminate]?.Invoke(BuffArgs);
    }        
    
    #endregion </Callbacks>
    
    #region <Properties>    
    
    public Unit Owner {
        get
        {
            return _owner;            
        }
        private set
        {            
            // 만약 지정 대상이 죽은 상태라면 소유주 권한을 빼앗고 Initialize.
            if (value != null && UnitFilter.Check(value, UnitFilter.Condition.IsDead))
            {
                _owner = null;
                Initialize();
                return;
            }

            // 이전 소유주가 남아있는 경우, 해당 소유주로부터 탈환.
            if (_owner != null)
            {
                if (_owner.CrowdControlGroup.Contains(this))
                    _owner.CrowdControlGroup.Remove(this);
            }

            var isValid = true;
            // 만약 소유주가 등록되는 경우에는 ParentType을 통한 버프 Valid 체크를 해야함.            
            if (value != null)
            {
                // 각 ParentType 별 Default 옵션을 지정한다.
                switch (_parentType)
                {
                    case _ParentType.None:
                        // None으로 초기화되면 _buffAction을 다 비운다.
                        for (var eventIndex = 0; eventIndex < _buffActionGroup.Length; ++eventIndex)
                        {
                            _buffActionGroup[eventIndex] = null;
                        }

                        break;
                    case _ParentType.Stun:
                        // 오버라이드를 통해 가치 잔류 여부를 판단한다.
                        isValid = !OverrideValidCheck(value, OverrideType.Recent);
                        if (isValid)
                        {
                            SetAction(EventType.OnBirth, buffArgs =>
                            {
                                // TODO<Carey>: 스턴에 대한 표현 방법이 바뀌어야 함.
                                // 스턴 상태일 때 어떠한 애니메이션을 취할 지 명시하게 템플릿 형태로 구성해야함.
                                // 현재 애니메이션들이 idle, spell... 등을 상속 받아 명시하고 있는 것과 같이.
                                buffArgs.Owner.UnitBoneAnimator.UnityAnimator.speed = .0f;
                            }, isOverride: true);
                            SetAction(EventType.OnTerminate,
                                buffArgs => { buffArgs.Owner.UnitBoneAnimator.UnityAnimator.speed = 1.0f; },
                                isOverride: true);
                        }

                        break;
                    case _ParentType.Bleeding:
                        SetAction(EventType.OnHeartBeat, buffArgs =>
                        {
                            buffArgs.Owner.Hurt(buffArgs.Caster, damage: buffArgs.Integer,
                                type: Unit.TextureType.Universal);
                        }, isOverride: true);
                        break;
                    case _ParentType.CastSpeed:
                        SetAction(EventType.OnBirth,
                            buffArgs => { buffArgs.Owner.CastSpeedMultiplier += buffArgs.Float; },
                            isOverride: true);
                        SetAction(EventType.OnTerminate,
                            buffArgs => { buffArgs.Owner.CastSpeedMultiplier -= buffArgs.Float; },
                            isOverride: true);
                        break;
                    case _ParentType.MovementSpeed:
                        SetAction(EventType.OnBirth,
                            buffArgs => { buffArgs.Owner.MovementSpeedMultiplier += buffArgs.Float; },
                            isOverride: true);
                        SetAction(EventType.OnTerminate,
                            buffArgs => { buffArgs.Owner.MovementSpeedMultiplier -= buffArgs.Float; },
                            isOverride: true);
                        break;
                    case _ParentType.Invincible:
                        isValid = !OverrideValidCheck(value, OverrideType.Recent);
                        if (isValid)
                        {
                            SetAction(EventType.OnBirth, buffArgs => { buffArgs.Owner.SetInvincible(true); },
                                isOverride: true);
                            SetAction(EventType.OnTerminate, buffArgs => { buffArgs.Owner.SetInvincible(false); },
                                isOverride: true);
                        }
                        break;
                    case _ParentType.Count:
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }

            if (isValid) _owner = value;
            
            // null reference 체크 후 CrowdControl에 등록.
            if (_owner != null)
                _owner.CrowdControlGroup.Add(this);
        } 
    }

    public _ParentType ParentType
    {
        get { return _parentType; }
        private set { _parentType = value; }
    }

    public _Type Type
    {
        get { return _type; }
        private set { _type = value; } 
    }
    
    public BuffArgs BuffArgs { get; private set; }

    #endregion </Properties>
    
    #region <Setters/Methods>

    /// <summary>
    /// 해당 버프는 이벤트마다 어떠한 액션을 할 지에 대해서 명시를 한다.
    /// </summary>
    /// <param name="eventType">어떠한 이벤트인지?</param>
    /// <param name="action">어떠한 액션을 할 건지?</param>
    /// <param name="isOverride">기존 액션을 덮어쓸 건지?</param>
    /// <returns></returns>
    public Buff SetAction(EventType eventType, Action<BuffArgs> action, bool isOverride = false)
    {
//        // 여기여기!!
//        if (_parentType == _ParentType.None) return this;

        if (!isOverride)
            _buffActionGroup[(int) eventType] += action;
        else
            _buffActionGroup[(int) eventType] = action;
        
        return this;
    }
    
    #endregion </Setters/Methods>
    
    #region <Methods>

    /// <summary>
    /// 해당 type Buff를 owner가 가지고 있는 지 알려준다.
    /// </summary>
    /// <param name="owner">어떠한 유닛이</param>
    /// <param name="type">무슨 버프를 가지고 있는 지</param>
    /// <returns>해당 owner가 type 버프를 가지고 있으면 true를 리턴.</returns>
    public static bool HasBuff(Unit owner, _Type type)
    {
        foreach (var crowdControl in owner.CrowdControlGroup)
        {
            if (crowdControl.Type == type)
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// 버프 생성 시 버프가 같은 종류일 때 상존할 수 없는 경우 덮어써야하는 데,
    /// 이 경우 어떤 식으로 덮어 쓸 지에 대해서 이야기하고 있다.
    /// </summary>
    /// <param name="owner">버프 소유주</param>
    /// <param name="overrideType">어떤 식으로 버프를 덮어쓸 지 그 타입에 대해서 서술하고 있다.</param>
    /// <returns>이 객체가 이 매서드에 의해 유효성을 잃었는 지의 여부를 돌려준다.</returns>
    /// <exception cref="ArgumentOutOfRangeException">구현되지 않은 부분에 대한 익셉션</exception>
    public bool OverrideValidCheck(Unit owner, OverrideType overrideType)
    {
        var duplicated = (Buff) null;

        // 같은 ParentType을 두고 있는 Buff를 가지고 있는 지 찾는다.
        foreach (var crowdControlCandidate in owner.CrowdControlGroup)
        {
            if (crowdControlCandidate == this)
                duplicated = crowdControlCandidate;
        }

        if (duplicated == null) return false;
        
        // OverrideType 참조
        switch (overrideType)
        {
            case OverrideType.Recent:
                duplicated.OnTerminate();                
                break;
            case OverrideType.Race:                
                if (duplicated.Lifespan <= Lifespan)
                    Owner.CrowdControlGroup.Remove(duplicated);
                else
                    return true;
                break;
            case OverrideType.Merge:
                duplicated.Lifespan += Lifespan;
                return true;
            // 카운트는 실제로 사용될 수 있는 부분이 아니니 exception 처리
            case OverrideType.Count:    
            default:
                throw new ArgumentOutOfRangeException(nameof(overrideType), overrideType, null);
        }

        return false;
    }

    /// <summary>
    /// 스턴 shorthand
    /// </summary>
    /// <param name="target">스턴 입힐 대상</param>
    /// <param name="lifeSpan">하트 비트에 기반한 기간</param>
    /// <returns>만들어진 버프를 돌려줌</returns>
    public static Buff Stun(Unit target, int lifeSpan)
    {
        var buff = CreateBuff(target, _ParentType.Stun, _Type.Stun, lifeSpan, false);       
        
        return buff;
    }
    
    /// <summary>
    /// 출혈 shorthand
    /// </summary>
    /// <param name="caster">데미지 줄 시전자</param>
    /// <param name="target">데미지 받을 대상</param>
    /// <param name="lifeSpan">하트 비트에 기반한 기간</param>
    /// <param name="damage">하트 비트마다 줄 데미지</param>
    /// <returns>만들어진 버프를 돌려줌</returns>
    public static Buff Bleeding(Unit caster, Unit target, int lifeSpan, int damage)
    {
        var buff = CreateBuff(target, _ParentType.Bleeding, _Type.Bleeding, lifeSpan, false);
        buff.BuffArgs
            .SetCaster(caster)
            .SetFactor(damage);
        
        return buff;
    }

    /// <summary>
    /// 이동속도 slow shorthand
    /// </summary>
    /// <param name="target"></param>
    /// <param name="lifeSpan"></param>
    /// <param name="degree"></param>
    /// <returns></returns>
    public static Buff SlowMove(Unit target, int lifeSpan, float degree)
    {
        var buff = CreateBuff(target, _ParentType.MovementSpeed, _Type.MoveSlow, lifeSpan, false);

        buff.BuffArgs.SetFactor(-degree);

        return buff;
    }

    /// <summary>
    /// 캐스트속도 slow shorthand
    /// </summary>
    /// <param name="target"></param>
    /// <param name="lifeSpan"></param>
    /// <param name="degree"></param>
    /// <returns></returns>
    public static Buff SlowCast(Unit target, int lifeSpan, float degree)
    {
        var buff = CreateBuff(target, _ParentType.CastSpeed, _Type.CastSlow, lifeSpan, false);

        buff.BuffArgs.SetFactor(-degree);

        return buff;
    }

    public static Buff CreateBuff(Unit owner, _ParentType parentType, _Type type, int lifespan, bool isPositive)
    {
        var buff = Instantiate();
        
        buff.Initialize();
        buff.ParentType = parentType;            
        buff.Type = type;
        buff.Lifespan = lifespan;
        buff.IsPositive = isPositive;
        buff.Owner = owner;

        if (buff.Owner == null) return null;
        
        buff.BuffArgs = new BuffArgs().SetOwner(buff.Owner);
        
        buff.OnBirth();

        return buff;
    }
    
    private static Buff Instantiate()
    {
        foreach (var buffCandidate in BuffStorage)
        {
            if (buffCandidate.Owner != null) continue;                                                
            
            return buffCandidate;
        }

        var newBuff = new Buff();
        BuffStorage.Add(newBuff);

        return newBuff;
    }


    #endregion </Methods>

}