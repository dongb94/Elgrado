
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public TutorialManager.TutorialName TutorialName;

    private bool _isEnable;

    private void Awake()
    {
        _isEnable = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Player") || !_isEnable) return;
        TutorialManager.GetInstance.PlayTutorial(TutorialName);
        _isEnable = false;
    }
}