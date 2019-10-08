using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class ItemManager : Singleton<ItemManager>
{
    private ItemInfo[] _itemInformationGroup;
    
    public Array AllItemGroup;
    public List<ItemList> ProbabilityRisingItemGroup { get; private set;  } // come out frequently at item shop
    public Array RankGroup;

    public Item DefaultItem { get; private set; }

    public enum ItemList
    {
        LeatherArmor,       Goggles,     IronBoots,     Gloves,        WoodenClub, 
        Dagger,             Axe,         Rapier,        Cloak,         LongBow,
        LongSword,          Maingauche,  Mace,          HalfGloves,    Boots,
        Circlet,            CrossBow,    ShortBow,      ShortSword,    Shoes,
        ThrowingDagger,     Staff,       IronGloves,    ShoulderArmor, Robe,
        Orb,                Wand,        Cloth,         IronArmor,     IronHelmet,
        
        count,
        none,
        EmptyEquipment
    }
    
    public enum ItemRank
    {
        normal,
        magic,
        rare,
        unique,
        mystic,
        
        count,
        none,
    }
    
    private void Awake()
    {
        _itemInformationGroup = new ItemInfo[(int)ItemList.count];

        //TODO 발동조건, 효과 등록
        #region <Item Infomation>
        _itemInformationGroup[(int)ItemList.LeatherArmor] = 
            new ItemInfo()
            {
                Price = 50, IntFactor = 10,
                EquipmentDamage = 10,
                EquipmentHealth = 20
            };
        _itemInformationGroup[(int)ItemList.Goggles] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.IronBoots] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Gloves] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.WoodenClub] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Dagger] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Axe] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Rapier] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Cloak] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.LongBow] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.LongSword] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Maingauche] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Mace] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.HalfGloves] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Boots] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Circlet] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.CrossBow] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.ShortBow] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.ShortSword] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Shoes] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.ThrowingDagger] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Staff] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.IronGloves] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.ShoulderArmor] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Robe] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Orb] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Wand] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.Cloth] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.IronArmor] = 
            new ItemInfo()
            {
                Price = 50
            };
        _itemInformationGroup[(int)ItemList.IronHelmet] = 
            new ItemInfo()
            {
                Price = 50
            };
        #endregion
        
        
        AllItemGroup = Enum.GetValues(typeof(ItemList));
        ProbabilityRisingItemGroup = new List<ItemList>();
        RankGroup= Enum.GetValues(typeof(ItemRank));
        
        DefaultItem = new Item(ItemList.none, ItemRank.none);
    }

    public Item GetRandomItem(bool onReservationSet = false)
    {
        int ran;
        ItemList item;
        if (onReservationSet && ProbabilityRisingItemGroup.Count != 0)
        {
            ran = Random.Range(0, ProbabilityRisingItemGroup.Count);
            item = ProbabilityRisingItemGroup[ran];
        }
        else
        {
            ran = Random.Range(0, (int)ItemList.count);
            item = (ItemList)AllItemGroup.GetValue(ran);
        }

        ran = Random.Range(0, (int)ItemRank.count);
        return SetItemInfo(item, (ItemRank)RankGroup.GetValue(ran));
    }

    public Item GetDefaultItem()
    {
        return DefaultItem;
    }

    public void AddProbabilityRisingItemGroup(ItemList item)
    {
        ProbabilityRisingItemGroup.Add(item);
    }

    private Item SetItemInfo(ItemList kinds, ItemRank rank)
    {
        var info = _itemInformationGroup[(int) kinds];
        
        var item = new Item()
            .SetItem(kinds)
            .SetRank(rank)
            //.SetCost(info.Cost)
            .SetPrice(info.Price)
            .SetDamage(info.EquipmentDamage)
            .SetDefense(info.EquipmentDefense)
            .SetHealth(info.EquipmentHealth);
        
        switch (kinds)
        {
            case ItemList.LeatherArmor :
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Goggles:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.IronBoots:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Gloves:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.WoodenClub:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Dagger:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Axe:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Rapier:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Cloak:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.LongBow:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.LongSword:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Maingauche:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Mace:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.HalfGloves:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Boots:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Circlet:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.CrossBow:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.ShortBow:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.ShortSword:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Shoes:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.ThrowingDagger:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Staff:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.IronGloves:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.ShoulderArmor:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Robe:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Orb:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Wand:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.Cloth:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.IronArmor:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            case ItemList.IronHelmet:
                item.MountEvent = (args) =>
                {
                    
                };
                item.UnMountEvent = (args) =>
                {
                    
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kinds), kinds, null);
        }

        return item;
    }
    
    private struct ItemInfo
    {
        public int Cost;
        public int Price;
        public int IntFactor;
        public float FloatFactor;
        
        public float EquipmentDamage;
        public int EquipmentDefense;
        public int EquipmentHealth;
    }
}