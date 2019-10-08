using System;
using UnityEngine;

public class CustomUIItemSlot : CustomUIRoot
{
    [SerializeField]private UISprite[] _slot;
    [SerializeField]private UISprite[] _rankFrame;

    [NonSerialized]public Item[] ItemSlot;

    protected override void Awake()
    {
        base.Awake();
        
        ItemSlot = new Item[3];
    }

    public void SetItem(Item item, int slotNum)
    {
        _slot[slotNum].spriteName = item.Name.ToString();
        _rankFrame[slotNum].spriteName = item.Rank.ToString();

        ItemSlot[slotNum] = item;
    }

    public void DeleteItem(int slotNum)
    {
        _slot[slotNum].spriteName = ItemManager.ItemList.EmptyEquipment.ToString();
        _rankFrame[slotNum].spriteName = ItemManager.ItemRank.none.ToString();

        ItemSlot[slotNum] = null;
    }
}