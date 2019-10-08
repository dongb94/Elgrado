using UnityEngine.Serialization;

public class UIResultScene : CustomUIRoot
{
    [FormerlySerializedAs("GameResult")] public UISprite GameResultSprite;
    [FormerlySerializedAs("PlaytimeMinute")] public UILabel PlaytimeMinuteLabel;
    [FormerlySerializedAs("PlaytimeSecond")] public UILabel PlaytimeSecondLabel;
    [FormerlySerializedAs("EarnedGold")] public UILabel EarnedGoldLabel;
    [FormerlySerializedAs("ExpBefore")] public UISprite ExpBeforeSprite;
    [FormerlySerializedAs("ExpAfter")] public UISprite ExpAfterSprite;
    public UISprite[] LootingItemFrameGroup;
    public UISprite[] LootingItemGroup;

    public void SetGameResult(GameResultStruct gameResult)
    {
        switch (gameResult.Result)
        {
                case GameResultStruct.GameResult.Win :
                    GameResultSprite.spriteName = "Result-Win";
                    break;
                case GameResultStruct.GameResult.Lose :
                    GameResultSprite.spriteName = "Result-Lose";
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
        ExpBeforeSprite.fillAmount = 0;
        ExpAfterSprite.fillAmount = ExpBeforeSprite.fillAmount + gameResult.EarnedExp / 10000;

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
        }
    }
}