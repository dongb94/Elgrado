using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using FlyingWormConsole3.LiteNetLib;
using UnityEngine;

/// <summary>
/// 플레이어의 챔피언에 대한 액션 실행을 위한 버튼 클릭과 관련된 이벤트의 시작과 끝을 다룬다.
/// </summary>
public class ActionButtonTrigger : CustomUIEventListener
{
    
    #region <Consts>
        
    public static readonly Action<UnitEventArgs>[] CastTypeSetter =
        Enumerable.Range(0, (int) CastType.Count)
            .Select<int, Action<UnitEventArgs>>(
                (index) => 
                    (args) => args.ActionButtonTrigger.CastTriggerType = (CastType) index       
            ).ToArray();        
    
    #endregion </Consts>
    
    #region <Fields>        
    
    [SerializeField] private Type _type;
    [SerializeField] private UISprite _onActiveUiSprite;
    [SerializeField] private UISprite _onInactiveUiSprite;
    [SerializeField] private UISprite _onActiveOuterUiSprite;
    [SerializeField] private UISprite _onInactiveOuterUiSprite;
    private CastType _castType;

    #endregion </Fields>
    
    #region <Enums>
    
    /// <summary>
    /// 어떠한 액션을 실행시켰는 지 구분하기 위한 인자
    /// </summary>
    public enum Type
    {
        Normal,
        Primary /*Left*/,
        Secondary /*Right*/,
        
        Count
    }
    
    public enum CastType
    {        
        [Description("No additional action for begin")]
        Auto,
        [Description("Additional action until clicking this action button")]
        Suspend,
        [Description("Additional action for begin which selecting the cast area")]        
        TargetToLocation,
        [Description("Additional action for begin which selecting the cast target")]
        TargetToUnit,
        
        Count
    }
    
    #endregion </Enums>

    #region <Unity/Callbacks>

    protected override void Awake()
    {
        base.Awake();
        
        SetActive(true);        
    }

    private void LateUpdate()
    {
        if (_castType != CastType.TargetToLocation && _castType != CastType.TargetToUnit) return;       
                
        var raycast = CameraManager.GetInstance.UICamera.ScreenPointToRay(GetPointPosition());                
                
        if (Physics2D.RaycastNonAlloc(raycast.origin, raycast.direction, RaycastHit2DGroup) > 0)
        {            
            if (RaycastHit2DGroup[0].collider.gameObject == gameObject)
            {                
                return;
            }
        }

        EventInfo.Collider = null;
        CustomUIEventCaster.GetInstance.SetLastUIEventInfo(EventInfo);        
        HUDManager.GetInstance.ProjectorController.OnClickBegin();
        OnClickEnd();
    }

    #endregion </Unity/Callbacks>
    
    #region <Callbacks>
            
    public override void OnClickBegin()
    {                      
        var eventInfo = CustomUIEventCaster.GetInstance.GetLastUIEventInfo;
        EventInfo = eventInfo;

        if (HUDManager.GetInstance.State != HUDManager.HUDState.Playing)
        {
            HUDManager.GetInstance.State = HUDManager.HUDState.Playing;
            
            if (HUDManager.GetInstance.ProjectorController.Listener != this)
                PlayerChampionHandler.GetInstance.OnActionTrigger(this, _type);        
        }        
        else
        {
            // 1. 플레이어 매니저에게 본인 타입에 대한 이벤트 전달
            PlayerChampionHandler.GetInstance.OnActionTrigger(this, _type);
        }

    }

    public override void OnClickEnd()
    {
        var eventInfo = CustomUIEventCaster.GetInstance.GetLastUIEventInfo;
        
        switch (_castType)
        {
            case CastType.Auto:
                break;
            case CastType.Suspend:
                var actionCaster = PlayerChampionHandler.GetInstance.Handle;
                actionCaster.OnActionTriggerEnd(ActionButtonType);
                break;
            case CastType.TargetToLocation:
                break;
            case CastType.TargetToUnit:
                break;            
            case CastType.Count:
            default:
                throw new ArgumentOutOfRangeException();
        }

        _castType = CastType.Auto;
        EventInfo.IsActive = false;
    }

    public static void OnReceiveCastRequest(Vector3 castLocation, bool isCanceled = false)
    {
        HUDManager.GetInstance.State = HUDManager.HUDState.Playing;

        if (isCanceled) return;
        
        var actionCaster = PlayerChampionHandler.GetInstance.Handle;
        actionCaster.CurrentActionArgs.SetCastPosition(castLocation);
        actionCaster.ActionTrigger(Champion.UnitEventType.Begin);
    }
    
    #endregion </Callbacks>
       
    #region <Properties>

    public Type ActionButtonType
    {
        get { return _type; }
    }

    public CastType CastTriggerType
    {
        get { return _castType;}
        set
        {
            var actionCaster = PlayerChampionHandler.GetInstance.Handle;
            
            _castType = value;

            switch (_castType)
            {
                case CastType.Auto:
                case CastType.Suspend:
                    actionCaster.ActionTrigger(Champion.UnitEventType.Begin);
                    break;
                case CastType.TargetToLocation:
                    var projectorScale = actionCaster.CurrentActionArgs.FloatFactor;                    
                    HUDManager.GetInstance.ProjectorController.SetListener(this);
                    HUDManager.GetInstance.ProjectorController.SetScale(projectorScale);
                    HUDManager.GetInstance.State = HUDManager.HUDState.TargettingArea;
                    break;
                case CastType.TargetToUnit:
                    break;                
                case CastType.Count:
                default:
                    throw new ArgumentOutOfRangeException("CastTrigger", value, null);
            }
        }
    }   
    
    #endregion </Properties>
    
    #region <Methods>   

    public void Sync()
    {
        var action = PlayerChampionHandler.GetInstance.Handle.ActionGroupRoot[(int) _type];
        var actionStatus = PlayerChampionHandler.GetInstance.Handle.ActionStatusGroup[action];

        //cooltime status
        if (actionStatus.CurrentCooldown > 0)
        {
            if (_onActiveUiSprite.gameObject.activeSelf.Equals(true))
            {
                _onActiveUiSprite.gameObject.SetActive(false);
                _onInactiveUiSprite.gameObject.SetActive(true);
            }
            
            _onInactiveUiSprite.fillAmount = 1.0f - (float) actionStatus.CurrentCooldown / actionStatus.MaximumCooldown;
        }
        else
        {
            if (_onActiveUiSprite.gameObject.activeSelf.Equals(false))
            {
                _onActiveUiSprite.gameObject.SetActive(true);
                _onInactiveUiSprite.gameObject.SetActive(false);
            }
        }

        //stack cooltime status
        if (actionStatus.MaximumStack > 0)
        {
            if(actionStatus.CurrentStack < actionStatus.MaximumStack)
            {
                if (_onActiveOuterUiSprite.gameObject.activeSelf.Equals(true))
                {
                    _onActiveOuterUiSprite.gameObject.SetActive(false);
                    _onInactiveOuterUiSprite.gameObject.SetActive(true);
                }

                _onInactiveOuterUiSprite.fillAmount = 1.0f - (float) actionStatus.CurrentStackCooldown / actionStatus.MaximumStackCooldown;
            }
            else
            {
                if (_onActiveOuterUiSprite.gameObject.activeSelf.Equals(false))
                {
                    _onActiveOuterUiSprite.gameObject.SetActive(true);
                    _onInactiveOuterUiSprite.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (_onActiveOuterUiSprite.gameObject.activeSelf.Equals(true) || _onInactiveOuterUiSprite.gameObject.activeSelf.Equals(true))
            {
                _onActiveOuterUiSprite.gameObject.SetActive(false);
                _onInactiveOuterUiSprite.gameObject.SetActive(false);
            }
        }
    }        

    #endregion </Methods>
}
