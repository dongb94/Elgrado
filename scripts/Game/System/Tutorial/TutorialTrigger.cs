
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public TutorialManager.TutorialName TutorialName;

    private bool _isEnable;

    public void CustomAwake()
    {
        _isEnable = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Player") || !_isEnable) return;
        var async = K514PooledCoroutine.GetCoroutine().GetDeferredAction(3f,
            CAR => { TutorialManager.GetInstance.PlayTutorial((TutorialManager.TutorialName) CAR.I_factor); });
        async._mParams.SetFactor((int) TutorialName);
        async.SetTrigger();
        _isEnable = false;
    }
}