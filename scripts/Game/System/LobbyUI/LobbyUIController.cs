
using System;
using Game.UI.LobbyUI;
using GooglePlayGames;
using UnityEngine;

public class LobbyUIController : Singleton<LobbyUIController>
{
    public GameReadyPanel GameReadyPanel;
    public SkillSelectPanel SkillSelectPanel;
    public UpgradePanel UpgradePanel;
    public AchievementPanel AchievementPanel;
    public OptionPanel OptionPanel;

    [NonSerialized]public Strapper system;

    private bool isOtherPanelActive;
    
    private float timer;
    private int flag;

    private readonly float _animationTime = 0.5f;
    private readonly Vector3 _upperPosition = new Vector3(0, 700, 0);

    protected override void Initialize()
    {
        base.Initialize();
        system = FindObjectOfType<Strapper>();
        system.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (flag == 0) return;

        timer += Time.fixedDeltaTime;
        timer = Math.Min(_animationTime, timer);
        
        switch (flag)
        {
            case 1 : // GameReadyPanel Down
                GameReadyPanel.transform.localPosition = _upperPosition * (_animationTime - timer) / _animationTime;        
                break;
            case 2 : // GameReadyPanel Up
                GameReadyPanel.transform.localPosition = _upperPosition * (timer / _animationTime);        
                break;
            case 3 : // SkillSelectPanel Down
                SkillSelectPanel.transform.localPosition = _upperPosition * (_animationTime - timer) / _animationTime;        
                break;
            case 4 : // SkillSelectPanel Up
                SkillSelectPanel.transform.localPosition = _upperPosition * (timer / _animationTime);        
                break;
            case 5 : // UpgradePanel Down
                UpgradePanel.transform.localPosition = _upperPosition * (_animationTime - timer) / _animationTime;        
                break;
            case 6 : // UpgradePanel Up
                UpgradePanel.transform.localPosition = _upperPosition * (timer / _animationTime);        
                break;
            case 7 : // AchievementPanel Down
                AchievementPanel.transform.localPosition = _upperPosition * (_animationTime - timer) / _animationTime;        
                break;
            case 8 : // AchievementPanel Up
                AchievementPanel.transform.localPosition = _upperPosition * (timer / _animationTime);        
                break;
            case 9 : // OptionPanel Down
                OptionPanel.transform.localPosition = _upperPosition * (_animationTime - timer) / _animationTime;        
                break;
            case 10 : // OptionPanel Up
                OptionPanel.transform.localPosition = _upperPosition * (timer / _animationTime);        
                break;
        }

        if (!(timer >= _animationTime)) return;
        timer = 0;
        flag = 0;
    }
    
    public void GameReadyPanelDown(LoadingSceneManager.TargetMainScene stageName)
    {
        if (flag != 0 || isOtherPanelActive) return;
        GameReadyPanel.TargetScene = stageName;
        GameReadyPanel.MissionSet();
        GameReadyPanel.RankingPanel.LeaderBoardSet(RankingPanel.RankSort.TopBased);
        isOtherPanelActive = true;
        flag = 1;
        ButtonEvent();
    }
    
    public void GameReadyPanelUp()
    {
        if (flag != 0) return;
        isOtherPanelActive = false;
        flag = 2;
        ButtonEvent();
    }
    
    public void SkillSelectPanelDown()
    {
        if (flag != 0 || isOtherPanelActive) return;
        isOtherPanelActive = true;
        flag = 3;
        ButtonEvent();
    }
    
    public void SkillSelectPanelUp()
    {
        if (flag != 0) return;
        isOtherPanelActive = false;
        flag = 4;
        ButtonEvent();
    }
    
    public void UpgradePanelDown()
    {
        if (flag != 0 || isOtherPanelActive) return;
        isOtherPanelActive = true;
        flag = 5;
        ButtonEvent();
    }
    
    public void UpgradePanelUp()
    {
        if (flag != 0) return;
        isOtherPanelActive = false;
        flag = 6;
        ButtonEvent();
    }
    
    public void AchievementPanelDown()
    {
        if (flag != 0 || isOtherPanelActive) return;
        isOtherPanelActive = true;
        PlayGamesPlatform.Instance.ShowAchievementsUI();
        flag = 7;
        ButtonEvent();
    }
    
    public void AchievementPanelUp()
    {
        if (flag != 0) return;
        isOtherPanelActive = false;
        flag = 8;
        ButtonEvent();
    }
    
    public void OptionPanelDown()
    {
        if (flag != 0 || isOtherPanelActive) return;
        isOtherPanelActive = true;
        flag = 9;
        ButtonEvent();
    }
    
    public void OptionPanelUp()
    {
        if (flag != 0) return;
        isOtherPanelActive = false;
        flag = 10;
        ButtonEvent();
    }

    public void ButtonEvent()
    {
        LobbySFX.PlaySfx(LobbySFX.ClipName.ButtonClick);
    }
}