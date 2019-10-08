
using System.Collections.Generic;

public struct GameResultStruct
{
    public GameResult Result;
    public int EarnedGold;
    public float PlayTime;
    public int EarnedExp;
    public Queue<Item> LootingItemGroup;
    
    public enum GameResult 
    {
        Win,
        Lose,
        Count
    }
}