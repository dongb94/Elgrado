
using System.Linq;
using UnityEngine;

public class DeleteItemTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerItemManager.GetInstance.UnMount(
            PlayerItemManager.GetInstance.EquipmentItem.Last()
        );
    }
}