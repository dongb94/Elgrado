
public class DialogEventTrigger : CustomUIEventListener
{
    public override bool OnClickBeginEvent()
    {
        if (!IsActive) return false;

        CustomUIEventCaster.GetInstance.LastUIEventInfo.IsActive = false;
        
        DialogManager.GetInstance.NextEvent();
        
        return true;
    }

    public override bool OnClickEndEvent()
    {
        return true;
    }
}