
using UnityEngine;

public class AddItemTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerItemManager.GetInstance.Mount(
            ItemManager.GetInstance.CreateItem(ItemManager.ItemList.Goggles, ItemManager.ItemRank.magic)
        );
        PlayerItemManager.GetInstance.Mount(
            ItemManager.GetInstance.CreateItem(ItemManager.ItemList.ShortBow, ItemManager.ItemRank.magic)
        );
    }
}