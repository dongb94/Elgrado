using System;
using UnityEngine;

public class CustomUIItemSlot : CustomUIRoot
{
    public ItemSlotButtonTrigger[] ItemSlot;
    
    protected override void Awake()
    {
        base.Awake();
    }

    public override void SetActive(ActiveType active)
    {
        base.SetActive(active);
        foreach (var itemSlot in ItemSlot)
        {
            itemSlot.SetActive(active);
        }
    }

    public void SetItem(Item item)
    {
        var index = 0;
        while (!ItemSlot[index++].SetItem(item));
    }

    public void DeleteItem(int slotNum)
    {
        ItemSlot[slotNum].DeleteItem();
    }

    public void Sync()
    {
        foreach (var itemSlot in ItemSlot)
        {
            itemSlot.Sync();
        }
    }
}