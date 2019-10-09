
using UnityEngine;
using UnityEngine.Serialization;

public class UIResultScene : CustomUIRoot
{
    public UIResultSceneAnimation ResultAnimation;
    
    [FormerlySerializedAs("GameResult")] public UISprite GameResultSprite;
    [FormerlySerializedAs("PlaytimeMinute")] public UILabel PlaytimeMinuteLabel;
    [FormerlySerializedAs("PlaytimeSecond")] public UILabel PlaytimeSecondLabel;
    [FormerlySerializedAs("EarnedGold")] public UILabel EarnedGoldLabel;
    [FormerlySerializedAs("PlayerLevel")] public UILabel PlayerLevel;
    [FormerlySerializedAs("ExpBefore")] public UISprite ExpBeforeSprite;
    [FormerlySerializedAs("ExpAfter")] public UISprite ExpAfterSprite;
    public UISprite[] LootingItemFrameGroup;
    public UISprite[] LootingItemGroup;

    public CustomUIEventListener MainMenuButton;
    public CustomUIEventListener RetryButton;
    public CustomUIEventListener NextLevelButton;

    public override void SetActive(ActiveType active)
    {
        base.SetActive(active);
        NextLevelButton.SetActive(active);
        RetryButton.SetActive(active);
        
        if(active==ActiveType.Enable) ResultAnimation.Initialize();
    }

    public void SetGameResult(GameResultStruct gameResult)
    {
        var totalExp = PlayerInformation.ExperiencePoint + gameResult.EarnedExp;

        ResultAnimation.PlayResultAnimations(totalExp);
        
        switch (gameResult.Result)
        {
                case GameResultStruct.GameResult.Win :
                    GameResultSprite.spriteName = "Result-Win";
                    SoundManager.GetInstance.SetBGMtoRhythmManagerAndPlay(LoadManager.BGM_Index.Victory, false, false);
                    break;
                case GameResultStruct.GameResult.Lose :
                    NextLevelButton.SetActive(ActiveType.Disable);
                    GameResultSprite.spriteName = "Result-Lose";
                    SoundManager.GetInstance.SetBGMtoRhythmManagerAndPlay(LoadManager.BGM_Index.Lose, false, false);
                    break;
                default:
                    break;
        }

        var minute = (int)gameResult.PlayTime / 60;
        var second = (int)gameResult.PlayTime % 60;

        if (minute < 10) PlaytimeMinuteLabel.text = "0" + minute;
        else PlaytimeMinuteLabel.text = minute.ToString();

        if (second < 10) PlaytimeSecondLabel.text = "0" + second;
        else PlaytimeSecondLabel.text = second.ToString();

        EarnedGoldLabel.text = gameResult.EarnedGold.ToString();
        
        ///TODO 기존 경험치, 다음레벨 까지 필요 경험치 필요
        ExpBeforeSprite.fillAmount = PlayerInformation.ExperiencePoint * PlayerInformation.InverseExpRequirement;

        for (var i = 0; i < 8; i++)
        {
            if (gameResult.LootingItemGroup.Count == 0)
            {
                LootingItemFrameGroup[i].spriteName = ItemManager.ItemRank.none.ToString();
                LootingItemGroup[i].spriteName = ItemManager.ItemList.none.ToString();
                break;
            }
            
            var item = gameResult.LootingItemGroup.Dequeue();
            LootingItemFrameGroup[i].spriteName = item.Rank.ToString();
            LootingItemGroup[i].spriteName = item.Name.ToString();
            
            
            // G2 Fasta Temp Code, should be delete
            PlayerItemManager.GetInstance.Mount(item);
        }
    }

    public void UpdateExpBar(int exp)
    {
        if (exp >= PlayerInformation.DummyExp[PlayerInformation.PlayerLevel - 1])
        {
            var expBarLevelUpChange = K514PooledCoroutine.GetCoroutine()
                .GetDeferredAction(1f,
                    refs =>
                    {
                        var expAfter = refs.MorphObject as UISprite;
                        var levelLabel = refs.MorphObject2 as UILabel;
                        expAfter.fillAmount = 0f;
                        PlayerInformation.PlayerLevel += 1;
                        PlayerInformation.ExperiencePoint = 0;
                        levelLabel.text = "Lv " + PlayerInformation.PlayerLevel;
                        HUDManager.GetInstance.ResultScene.ExpBeforeSprite.fillAmount = 0;
                        HUDManager.GetInstance.ResultScene.UpdateExpBar(refs.I_factor);
                    }).SetAction(K514PooledCoroutine.ActionType.Activity,
                    refs =>
                    {
                        var expAfter = refs.MorphObject as UISprite;
                        expAfter.fillAmount = refs.F_factor2 +
                                              (1-refs.F_factor2) * refs.F_stack_factor * refs.F_Time_ReversedFactor;
                    });

            expBarLevelUpChange._mParams.SetMorphable(ExpAfterSprite);
            expBarLevelUpChange._mParams.SetMorphable2(PlayerLevel);
            expBarLevelUpChange._mParams.SetFactor(exp - PlayerInformation.DummyExp[PlayerInformation.PlayerLevel - 1]);
            expBarLevelUpChange._mParams.SetFactor2(PlayerInformation.ExperiencePoint *
                                                    PlayerInformation.InverseExpRequirement);
            expBarLevelUpChange.SetTrigger();
        }
        else
        {
            var expBarChange = K514PooledCoroutine.GetCoroutine()
                .GetDeferredAction(2f,
                    refs =>
                    {
                        var expBefore = refs.MorphObject2 as UISprite;
                        expBefore.fillAmount =  (refs.I_factor) * PlayerInformation.InverseExpRequirement;
                    }).SetAction(K514PooledCoroutine.ActionType.Activity,
                    refs =>
                    {
                        var expAfter = refs.MorphObject as UISprite;
                        expAfter.fillAmount = refs.F_factor2 +
                                              (refs.I_factor) * PlayerInformation.InverseExpRequirement * 
                                              refs.F_stack_factor * refs.F_Time_ReversedFactor;
                    });

            expBarChange._mParams.SetMorphable(ExpAfterSprite);
            expBarChange._mParams.SetMorphable2(ExpBeforeSprite);
            expBarChange._mParams.SetFactor(exp - PlayerInformation.ExperiencePoint);
            expBarChange._mParams.SetFactor2(PlayerInformation.ExperiencePoint *
                                             PlayerInformation.InverseExpRequirement);
            expBarChange.SetTrigger();

            PlayerInformation.ExperiencePoint += exp;
        }
        
    }
}