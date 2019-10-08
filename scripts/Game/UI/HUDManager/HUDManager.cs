using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class HUDManager : Singleton<HUDManager> {
    
    #region <Fields>

    private CustomHUDStack HUDStack;
    
    public PreProcessedMonoBehaviour EnemyHealthBarPrefab;
    //Listener Group
    public ProjectorController ProjectorController;
    public ProjectorController RangeCircleController;
    public UIChampionTagController UIChampionTagController;
    public ModernJoystickController JoystickController;
    public ActionButtonTrigger[] ActionButtonTriggerGroup;
    public UIOptionButton OptionButton;

    public UIBackground Background;
    [SerializeField] private UIChampionStateView[] _championStateViewGroup;
    //public CompassUIController CompassUiController; // Deleted
    public ChampionBuffPanelUI ChampionBuffUI;
    public UIMoneyBar MoneyBar;
    public CustomUIItemSlot ItemSlot;
    public BossHealthBarUI BossHealthBar;
    public UIHighlight Highlight;
    public UIDialog DialogUI;
    public UIItemShop ItemShop;
    public UIResultScene ResultScene;
    public UICover Cover;
    
#if UNITY_EDITOR
    [SerializeField] private K514KeyBoardAttacher _keyboardController;
#endif
    
    #endregion <Fields>

    #region <Enums>
    
    public enum ActionTriggerType
    {
        NormalAction,
        PrimaryAction,
        SecondaryAction,
        
        Count
    }

    public enum HUDState
    {
        [Description("UI가 꺼진 상태")]
        None,
        [Description("현재 정상적으로 게임이 플레이 되고 있는 경우의 HUD UI 상태")]
        Playing,        
        [Description("특정한 장소를 타겟팅하는 경우의 HUD UI 상태")]
        TargettingArea,
        [Description("아이템 샵이 오픈된 경우의 HUD UI 상태")]
        OpenItemShop,
        [Description("대화 중의 HUD UI 상태")]
        Script,
        [Description("하이라이트 중의 HUD UI 상태")]
        Highlight,
        [Description("게임 결과 HUD UI 상태")]
        GameResult
    }
      
    #endregion </Enums>

    #region <Properties>
    
    public HUDState State
    {
        get => HUDStack.Peek();
        set => HUDStack.Push(value);
    }

    public HUDState Deactivate
    {
        set => HUDStack.Pop(value);
    }
    
    #endregion </Properties>
    
    public void Trigger()
    {
        HUDStack = new CustomHUDStack(HUDState.Playing);
        
        SetChampionStateViewActive(CustomUIRoot.ActiveType.Enable);
    }

    public void UpdateChampionInfo(Champion playerChampion)
    {        
        UpdateChampionStateView();
        UpdateChampionIconView();
        
#if UNITY_EDITOR
        _keyboardController.SetFocus(playerChampion);        
#endif        
    }

    public void UpdateChampionStateView()
    {
        foreach (var championStateView in _championStateViewGroup)
        {
            championStateView.Sync();            
        }
    }
    
    public void UpdateChampionIconView()
    {
        foreach (var championStateView in _championStateViewGroup)
        {
            championStateView.IconSync();            
        }
    }

    public void UpdateChampionTagCoolTimeView()
    {
        foreach (var championTagCoolTimeView in _championStateViewGroup)
        {
            championTagCoolTimeView.TagCoolSync();            
        }
    }
    
    public void SetChampionStateViewActive(CustomUIRoot.ActiveType active)
    {
        foreach (var championStateView in _championStateViewGroup)
        {
            championStateView.SetActive(active);
        }
    }
    
    public void CleanUp()
    {
        JoystickController.EventInfo.IsActive = false;
        UIChampionTagController.EventInfo.IsActive = false;
        ProjectorController.EventInfo.IsActive = false;
        RangeCircleController.EventInfo.IsActive = false;
    }
    
    public void SetActionTriggerActive(CustomUIRoot.ActiveType active)
    {
        for(var i = 0 ; i < ActionButtonTriggerGroup.Length ; i++)
            ActionButtonTriggerGroup[i].SetActive(active);
    }

    public void ActionTriggerUpdate(ActionTriggerType p_Type)
    {
        ActionButtonTriggerGroup[(int)p_Type].Sync();
    }

    public void ActionTriggerUpdate()
    {
        for(var i = 0 ; i < ActionButtonTriggerGroup.Length ; i++)
        ActionButtonTriggerGroup[i].Sync();
    }

    public void StackCounterUpdate()
    {
        for (var i = 0; i < ActionButtonTriggerGroup.Length; i++)
        {
            var baringActionTrigger = ActionButtonTriggerGroup[i] as ActionTriggerWithBaring;
            if (baringActionTrigger == null) continue;
            if(baringActionTrigger.StackType != ActionTriggerWithBaring.StackProcessType.NoneStack) baringActionTrigger.UpdateStackCounter();
        }
    }

    public void RotateAntionTriggerByQuaterBeat(int p_Index)
    {
        var baringActionTrigger = ActionButtonTriggerGroup[p_Index] as ActionTriggerWithBaring;
        baringActionTrigger?.UpdateActionTriggerCogWheel(0.25f);
    }
/*
    public void SetCompass(Transform Boss, Transform Exit) => CompassUiController.SetCompassTarget(Boss, Exit);
    
    public void UpdateCompass()
    {
        CompassUiController.UpdateCompassUI();
    }

    public void UpdateBgmTimer(int p_Number)
    {
        CompassUiController.UpdateTimerLabel(p_Number);
    }
*/
    public void UpdateBuffUI()
    {
        ChampionBuffUI.UpdateBuffGroup();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}