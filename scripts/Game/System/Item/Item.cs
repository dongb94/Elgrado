using System;

public class Item {

    #region <Field>
    //public int Cost;
    public int Price;
    public int ConsolidationRate;
    public int Frequency;
    public ItemManager.ItemRank Rank;
    public ItemManager.ItemList Name;
    
    public float EquipmentDamage;
    public int EquipmentDefense;
    public int EquipmentHealth;

    public Action<ItemEventArgs> MountEvent;
    public Action<ItemEventArgs> UnMountEvent;

    public bool Release;
    public int ReleaseCost;

    public float ActiveEffectCoolTime;

    [NonSerialized]public readonly int ItemHashNum;
    
    #endregion </Field>

    public Item()
    {
        //Cost = 0;
        Price = 0;
        ConsolidationRate = 0;

        ItemHashNum = GetHashCode();
    }
    
    public Item(ItemManager.ItemList item, ItemManager.ItemRank itemRank)
    {
        Name = item;
        Rank = itemRank;
        //Cost = 0;
        Price = 0;
        ConsolidationRate = 0;

        Release = false;

        ActiveEffectCoolTime = 0;
        
        ItemHashNum = GetHashCode();
    }

    public Item SetCost(int cost)
    {
        //Cost = cost;
        return this;
    }
    
    public Item SetPrice(int price)
    {
        Price = price;
        return this;
    }

    public Item SetItem(ItemManager.ItemList item)
    {
        Name = item;
        return this;
    }

    public Item SetRank(ItemManager.ItemRank rank)
    {
        Rank = rank;
        return this;
    }

    public Item SetDamage(float damage)
    {
        EquipmentDamage = damage;
        return this;
    }
    
    public Item SetDefense(int defense)
    {
        EquipmentDefense = defense;
        return this;
    }
    
    public Item SetHealth(int health)
    {
        EquipmentHealth = health;
        return this;
    }
    
    public Item SetRelease(bool release)
    {
        Release = release;
        return this;
    }

    public Item SetReleaseCost(int cost)
    {
        ReleaseCost = cost;
        return this;
    }

    public void ReinforceRelative(int variation)
    {
        ConsolidationRate = Math.Max(0, ConsolidationRate + variation);
        ConsolidationRate = Math.Min(3, ConsolidationRate);
    }
}