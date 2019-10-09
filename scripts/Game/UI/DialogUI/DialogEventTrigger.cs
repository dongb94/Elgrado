
public class DialogEventTrigger : CustomUIEventListener
{
    public override bool OnClickBeginEvent()
    {
        if (!IsActive) return false;

        CustomUIEventCaster.GetInstance.LastUIEventInfo.IsActive = false;
        
        DialogManager.GetInstance.NextEvent();

        SoundManager.GetInstance.Play_UI_Sfx(K514SfxStorage.BeepType.Touch);
        
        return true;
    }

    public override bool OnClickEndEvent()
    {
        return true;
    }
}