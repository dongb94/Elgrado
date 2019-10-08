using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogStartTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        DialogManager.GetInstance.PlayScript("SampleScript.json");        
    }
}
