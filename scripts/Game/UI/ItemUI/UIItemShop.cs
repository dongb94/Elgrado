using UnityEngine;

public class UIItemShop : CustomUIRoot{
    //Shuffle item what displaying on shop
    public ItemButtonTrigger[] SaleItemButtonTriggers;
    public UILabel PlayerMoney;

    public void Initialize()
    {
        SetItemGroup();
    }

    public override void SetActive(ActiveType active)
    {
        base.SetActive(active);
        
        foreach (var button in SaleItemButtonTriggers)
        {
            button.SetActive(active);
        }
    }

    public void SetItemGroup()
    {
        foreach (var item in SaleItemButtonTriggers)
        {
            item.SetItem();
        }
    }

}