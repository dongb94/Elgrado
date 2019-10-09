
 
using System;
using System.Collections;
using System.Collections.Generic;
/// TODO 기존 스택과 상관 없이 발현하는 UI 스택 정의
///
/// 
public class CustomHUDStack : Stack<HUDManager.HUDState>
{
    private HUDManager _hudManager;
    
    public CustomHUDStack(HUDManager.HUDState BaseUIState)
    {
        base.Push(BaseUIState);
        _hudManager = HUDManager.GetInstance;

        ActivationUIState(BaseUIState, CustomUIRoot.ActiveType.Enable);
    }
    
    public void Push(HUDManager.HUDState item)
    {
        switch (item)
        {
            case HUDManager.HUDState.None:
                HUDManager.GetInstance.SetActive(false);
                return;
            case HUDManager.HUDState.Playing:
                while (Count!=0) ActivationUIState(base.Pop(), CustomUIRoot.ActiveType.Removed);
                goto default;
            case HUDManager.HUDState.Highlight:
                ActivationUIState(Peek(), CustomUIRoot.ActiveType.Disable);                
                break;
            default :
                if(Peek() == HUDManager.HUDState.Playing) 
                    ActivationUIState(Peek(), CustomUIRoot.ActiveType.Disable);
                else
                    ActivationUIState(Peek(), CustomUIRoot.ActiveType.Removed);
                break;
        }
        
        base.Push(item);
        
        ActivationUIState(item, CustomUIRoot.ActiveType.Enable);
    }

    private void Pop()
    {
        // can't unsafe pop.
    }

    public bool Pop(HUDManager.HUDState item)
    {
        if (Peek() != item) return false; 
        
        ActivationUIState(base.Pop(), CustomUIRoot.ActiveType.Removed);
        ActivationUIState(Peek(), CustomUIRoot.ActiveType.Enable);

        return true;
    }

    private HUDManager.HUDState ActivationUIState(HUDManager.HUDState UIState, CustomUIRoot.ActiveType active)
    {
        switch (UIState)
        {
            case HUDManager.HUDState.Playing :
                _hudManager.ChampionBuffUI.SetActive(active);
                _hudManager.JoystickController.SetActive(active);
                _hudManager.SetActionTriggerActive(active);
                _hudManager.SetChampionStateViewActive(active);
                _hudManager.ItemSlot.SetActive(active);
                _hudManager.MoneyBar.SetActive(active);
                _hudManager.OptionButton.SetActive(active);
                break;
            case HUDManager.HUDState.TargettingArea:
                _hudManager.ProjectorController.SetActive(active);
                break;
            case HUDManager.HUDState.OpenItemShop:
                _hudManager.ItemShop.SetActive(active);
                break;
            case HUDManager.HUDState.Script:
                _hudManager.DialogUI.SetActive(active);
                break;
            case HUDManager.HUDState.Highlight :
                _hudManager.Highlight.SetActive(active);
                break;
            case HUDManager.HUDState.GameResult :
                _hudManager.ResultScene.SetActive(active);
                break;
            default:
                throw new ArgumentOutOfRangeException("HUDState", UIState, null);
        }

        return UIState;
    }
}