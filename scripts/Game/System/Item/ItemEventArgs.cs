using System;

public class ItemEventArgs : EventArgs
{
    public Item Item;

    public ItemEventArgs SetItem(Item item)
    {
        Item = item;
        return this;
    }
}