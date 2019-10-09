using System;
using UnityEngine;

public class ItemSlotButtonTrigger : CustomUIEventListener
{
    [SerializeField]private UISprite _icon;
    [SerializeField]private UISprite _rankFrame;

    [SerializeField] private ParticleSystem _itemHighlight;

    [NonSerialized] public Item EquipItem;

    private CustomUIFont[] _releaseCost;
    private bool _isReadyRelease;

    public override bool OnClickBeginEvent()
    {
        return EquipItem != null && ReleaseItem();
    }

    public override bool OnClickEndEvent()
    {
        return true;
    }

    public override void SetActive(ActiveType active)
    {
        base.SetActive(active);
        
        if(active == ActiveType.Enable) Sync();
    }

    public bool SetItem(Item item)
    {
        if (EquipItem != null) return false;
        
        _icon.spriteName = item.Name.ToString();
        _rankFrame.spriteName = item.Rank.ToString();
        
        _icon.color = item.Release?Color.white:Color.gray;

        EquipItem = item;

        if (!EquipItem.Release)
        {
            _releaseCost = UIFontManager.GetInstance.GetFont(EquipItem.ReleaseCost.ToString(),Vector3.zero);
            UIFontManager.GetInstance.SetFontOverUI(_releaseCost, _icon, new Vector3(-13,-16,0));
            Sync();
        }
        
        return true;
    }

    public void DeleteItem()
    {
        _icon.spriteName = ItemManager.ItemList.EmptyEquipment.ToString();
        _rankFrame.spriteName = ItemManager.ItemRank.none.ToString();

        EquipItem = null;
    }

    public bool ReleaseItem()
    {
        if (PlayerItemManager.GetInstance.WaveStone < EquipItem.ReleaseCost || EquipItem.Release) return false;
        
        PlayerItemManager.GetInstance.WaveStone -= EquipItem.ReleaseCost;
        EquipItem.SetRelease(true);
        _icon.color = Color.white;
        
        _itemHighlight.Stop();
        UIFontManager.GetInstance.Pooling(_releaseCost);
        
        #region <FX>
        SoundManager.GetInstance.PlaySfx(K514SfxStorage.CommonType.Release);
        VfxManager.GetInstance.CreateVfx(VfxManager.Type.ItemRelease, PlayerChampionHandler.GetInstance.Handle.GetPosition, true);

        UnitFilter.GetUnitAtPlaneCircle(PlayerChampionHandler.GetInstance.Handle.GetPosition, 8.0f, 
            PlayerChampionHandler.GetInstance.Handle, 
            UnitFilter.Condition.IsAliveNegative, 
            PlayerChampionHandler.GetInstance.Handle.InteractUnitGroup);
        foreach (var FilteredUnit in PlayerChampionHandler.GetInstance.Handle.InteractUnitGroup)
        {
            if (FilteredUnit == null) break;
            var unit = (Unit) FilteredUnit;
            var direction = unit.GetPosition - PlayerChampionHandler.GetInstance.Handle.GetPosition;
            unit.AddForce(direction.normalized * Mathf.Min(40/direction.magnitude, 8.0f), true, true);
            var damageStruct = new DamagePreset();
            damageStruct.SetCaster(PlayerChampionHandler.GetInstance.Handle);
            damageStruct.SetDamage(1);
            damageStruct.SetFlags(p_IsCastHitMotion: true);
            damageStruct.SetKnock(4);
            unit.Hurt(damageStruct);
        }
        #endregion
        
        return true;
    }

    // Called When Player in-game Money is changed
    public void Sync ()
    {
        if (EquipItem==null || EquipItem.Release) return;

        _icon.color = Color.gray;
        
        if (PlayerItemManager.GetInstance.WaveStone >= EquipItem.ReleaseCost)
        {
            _itemHighlight.Play();
            _isReadyRelease = true;
        }
        else
        {
            _itemHighlight.Stop();
            _isReadyRelease = false;
        }
    }
}