using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GraphicRaycasterEx : MonoBehaviour
{
    private GraphicRaycaster gr;

    private void Awake()
    {
        gr = GetComponent<GraphicRaycaster>();
    }

    private void Update()
    {
        var ped = new PointerEventData(null);
        ped.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        gr.Raycast(ped, results);

        if (results.Count <= 0) return;
        results[0].gameObject.transform.position = ped.position;
    }
}