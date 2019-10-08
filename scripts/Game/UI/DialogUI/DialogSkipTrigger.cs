
public class DialogSkipTrigger : CustomUIEventListener
{
    public override bool OnClickBeginEvent()
    {
        if (!IsActive) return false;
        
        CustomUIEventCaster.GetInstance.LastUIEventInfo.IsActive = false;
        
        DialogManager.GetInstance.ExitScript();
        
        return true;
    }

    public override bool OnClickEndEvent()
    {
        return true;
    }
}