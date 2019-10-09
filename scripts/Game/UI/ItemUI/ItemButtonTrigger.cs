using UnityEngine;

//item button on item shop
public class ItemButtonTrigger : CustomUIEventListener
{

    public Item Item { private set; get; }

    [SerializeField] private Type _type;
    [SerializeField] private UISprite _rankFrame;
    [SerializeField] private UISprite _icon;
    [SerializeField] private UILabel _name;
    [SerializeField] private UILabel _price;
    [SerializeField] private Collider2D _buttonCollider;
	
    public enum Type
    {
        FixedItem /*selected item before in of game*/,
        RandomItem /*random item*/,
        
        Count
    }

    protected override void Awake()
    {
        base.Awake();
        _buttonCollider.enabled = false;
    }

    public override void SetActive(ActiveType active)
    {
        base.SetActive(active);
        _buttonCollider.enabled = active==ActiveType.Enable;
    }

    public override bool OnClickBeginEvent()
    {
        if (!IsActive) return false;

        SetItem();
        
        return true;
    }
    public override bool OnClickEndEvent()
    {
        if (!IsActive) return false;

        SaleItem();
        
        return true;
    }

    // call by UIItemShop
    public void SetItem()
    {
        Item = _type == Type.FixedItem ? ItemManager.GetInstance.GetRandomItem(true) : ItemManager.GetInstance.GetRandomItem();
        Sync();
    }

    public bool SaleItem()
    {
		
        if (Item == ItemManager.GetInstance.DefaultItem) return false; // sold out
		
        var player = PlayerItemManager.GetInstance;
        //비용 확인
        if (Item.Price > player.WaveStone) return false;
        player.WaveStone -= Item.Price;
        //add to inventory
        player.AddItem(Item);
		
        //change state
        Item = ItemManager.GetInstance.DefaultItem;
        Sync();
        _name.text = "Sold Out";
        _price.text = "---";

        return true;
    }

    public void Sync()
    {
        _rankFrame.spriteName = Item.Rank.ToString();
        _icon.spriteName = Item.Name.ToString();
        _name.text = Item.Name.ToString();
        _price.text = Item.Price.ToString();
        HUDManager.GetInstance.ItemShop.PlayerMoney.text = PlayerItemManager.GetInstance.WaveStone.ToString();
    }
}