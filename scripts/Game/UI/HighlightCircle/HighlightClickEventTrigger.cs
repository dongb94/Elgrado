using UnityEngine;

public class HighlightClickEventTrigger : CustomUIEventListener
{
    public override bool OnClickBeginEvent()
    {
        if(!IsActive || !CustomUIEventCaster.GetInstance.LastUIEventInfo.IsActive) return false;
        HUDManager.GetInstance.Deactivate = HUDManager.HUDState.Highlight;
        
        return true;
    }

    public override bool OnClickEndEvent()
    {
        return true;
    }
}