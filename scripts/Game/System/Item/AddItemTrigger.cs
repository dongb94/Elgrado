
using UnityEngine;

public class AddItemTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerItemManager.GetInstance.Mount(
            ItemManager.GetInstance.GetRandomItem()
        );
    }
}