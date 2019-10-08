
using System.Collections.Generic;
using UnityEngine;

public class GameInfomation : Singleton<GameInfomation>
{
    private float _startTime;
    private Queue<Item> _lootingItemGroup;
    private int _earnedGold;
    private int _earnedExp;

    protected override void Initialize()
    {
        _startTime = Time.realtimeSinceStartup;
        _lootingItemGroup = new Queue<Item>();
        _earnedGold = 0;
        _earnedExp = 0;
    }

    public void AddGold(int gold)
    {
        _earnedGold += gold;
        if (_earnedGold < 0) _earnedGold = 0;
    }
    
    public void AddExp(int exp)
    {
        _earnedExp += exp;
        if (_earnedExp < 0) _earnedExp = 0;
    }

    public void AddItem(Item item)
    {
        _lootingItemGroup.Enqueue(item);
    }

    public void SetResultUI(GameResultStruct.GameResult result)
    {
        HUDManager.GetInstance.State = HUDManager.HUDState.GameResult;
        HUDManager.GetInstance.ResultScene.SetGameResult(
            new GameResultStruct()
            {
                Result = result,
                PlayTime = PlayTime,
                EarnedGold = EarnedGold,
                EarnedExp = EarnedExp,
                LootingItemGroup = LootingItemGroup
            });
    }
    
    #region <Properties>
    
    public float PlayTime => Time.realtimeSinceStartup - _startTime;

    public Champion Player => PlayerChampionHandler.GetInstance.Handle;

    public Queue<Item> LootingItemGroup => _lootingItemGroup;

    public int EarnedGold => _earnedGold;
    
    public int EarnedExp => _earnedExp;

    #endregion
}