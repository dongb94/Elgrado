using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Inventory
public class PlayerItemManager : Singleton<PlayerItemManager>
{
    public List<Item> EquipmentItem { get; private set; }
    public List<Item> Inventory { get; private set; }
    //public int MaxCost;
    //private int CurrentCost;
    
    private float _equipmentDamage;
    private int _equipmentDefense;
    private int _equipmentHealth;
    private int _WaveStone;

    protected override void Initialize()
    {
        base.Initialize();
        EquipmentItem = new List<Item>();
        Inventory = new List<Item>();
        WaveStone = 0;
        //CurrentCost = 0;

        _equipmentDamage = 0;
        _equipmentDefense = 0;
        _equipmentHealth = 0;
    }
//
    #region <Properties>

    public float EquipmentDamage
    {
        get => _equipmentDamage;
        set
        {
            _equipmentDamage = value;
            PlayerChampionHandler.GetInstance.Handle?.SetAutoFireClusterPreset();
        }
    }

    public int EquipmentDefense
    {
        get => _equipmentDefense;
        set
        {
            _equipmentDefense = value;
            PlayerChampionHandler.GetInstance.Handle?.UpdateDefenceRate();
        }
    }

    public int EquipmentHealth
    {
        get => _equipmentHealth;
        set
        {
            _equipmentHealth = value;
            if (PlayerChampionHandler.GetInstance.Handle == null) return;
            PlayerChampionHandler.GetInstance.Handle.CurrentHealthPoint += 
                _equipmentHealth * PlayerChampionHandler.GetInstance.Handle.HealthPointRate;
            PlayerChampionHandler.GetInstance.Handle.UpdateReversedHealthPoint();
        }
    }

    public int WaveStone
    {
        get => _WaveStone;
        set
        {
            _WaveStone = Math.Max(0, value);
            HUDManager.GetInstance.MoneyBar.Sync();
            HUDManager.GetInstance.ItemSlot.Sync();
        }
    }

    #endregion

    #region <Methods>

    public void AddWaveStone(int p_Count)
    {
        WaveStone += p_Count;
    }

    #endregion
    
    #region <Function/AdministrateInvetory>

    public bool AddItem(Item item)
    {
        if (Inventory.Contains(item)) return false; //already have item
        Inventory.Add(item);
        return true;
    }

    public bool DeleteItem(Item item)
    {
        var findItem = FindItemInInventoryWithHashNumber(item.ItemHashNum);
        if (findItem==null) return false;//have not item
        Inventory.Remove(findItem);
        return true;
    }
    
    public bool DeleteItem(ItemManager.ItemList name, ItemManager.ItemRank rank)
    {
        var itemGroup = FindItemInInventoryWithItemNameAndRank(name, rank);
        if (itemGroup.Count == 0) return false;//have not item
        Inventory.Remove(itemGroup.Dequeue());
        return true;
    }
    
    public bool DeleteItem(int hashNum)
    {
        var item = FindItemInInventoryWithHashNumber(hashNum);
        if (item == null) return false;//have not item
        Inventory.Remove(item);
        return true;
    }

    #endregion
//
    #region <Function/MountAndUnmount>

    public bool Mount(Item item)
    {
        if (EquipmentItem.Count >= 3) return false;
        //if (CurrentCost + item.Cost > MaxCost) return false;
        //CurrentCost += item.Cost;
        
        EquipmentDamage += item.EquipmentDamage;
        EquipmentDefense += item.EquipmentDefense;
        EquipmentHealth += item.EquipmentHealth;
        
        HUDManager.GetInstance.ItemSlot.SetItem(item);
        
        EquipmentItem.Add(item);
        item.MountEvent(new ItemEventArgs().SetItem(item));
        
        return true;
    }
    
    public bool UnMount(Item item)
    {
        if (!EquipmentItem.Contains(item)) return false;//not equip item
        //CurrentCost -= item.Cost;

        var slotNum = EquipmentItem.IndexOf(item);
        
        EquipmentDamage -= item.EquipmentDamage;
        EquipmentDefense -= item.EquipmentDefense;
        EquipmentHealth -= item.EquipmentHealth;
        
        HUDManager.GetInstance.ItemSlot.DeleteItem(slotNum);
        
        EquipmentItem.Remove(item);
        item.UnMountEvent(new ItemEventArgs().SetItem(item));
        
        return true;
    }

    #endregion
//
    #region <Function/SearchItem> 

    public Item FindItemInItemSlotWithHashNumber(int hashNum)
    {
        foreach (var item in EquipmentItem)
        {
            if (item.ItemHashNum == hashNum) return item;
        }

        return null;
    }

    public Queue<Item> FindItemInItemSlotWithItemName(ItemManager.ItemList name)
    {
        var itemGroup = new Queue<Item>();
        foreach (var item in EquipmentItem)
        {
            if(item.Name==name) itemGroup.Enqueue(item);
        }

        return itemGroup;
    }
    
    public Queue<Item> FindItemInItemSlotWithItemRank(ItemManager.ItemRank rank)
    {
        var itemGroup = new Queue<Item>();
        foreach (var item in EquipmentItem)
        {
            if(item.Rank==rank) itemGroup.Enqueue(item);
        }

        return itemGroup;
    }
    
    public Queue<Item> FindItemInItemSlotWithItemNameAndRank(ItemManager.ItemList name, ItemManager.ItemRank rank)
    {
        var itemGroup = new Queue<Item>();
        foreach (var item in EquipmentItem)
        {
            if(item.Name==name && item.Rank==rank) itemGroup.Enqueue(item);
        }

        return itemGroup;
    }
    
    public Item FindItemInInventoryWithHashNumber(int hashNum)
    {
        foreach (var item in Inventory)
        {
            if (item.ItemHashNum == hashNum) return item;
        }

        return null;
    }

    public Queue<Item> FindItemInInventoryWithItemName(ItemManager.ItemList name)
    {
        var itemGroup = new Queue<Item>();
        foreach (var item in Inventory)
        {
            if(item.Name==name) itemGroup.Enqueue(item);
        }

        return itemGroup;
    }
    
    public Queue<Item> FindItemInInventoryWithItemRank(ItemManager.ItemRank rank)
    {
        var itemGroup = new Queue<Item>();
        foreach (var item in Inventory)
        {
            if(item.Rank==rank) itemGroup.Enqueue(item);
        }

        return itemGroup;
    }
    
    public Queue<Item> FindItemInInventoryWithItemNameAndRank(ItemManager.ItemList name, ItemManager.ItemRank rank)
    {
        var itemGroup = new Queue<Item>();
        foreach (var item in Inventory)
        {
            if(item.Name==name && item.Rank==rank) itemGroup.Enqueue(item);
        }

        return itemGroup;
    }

    #endregion
    
}