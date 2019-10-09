
public class DialogSkipTrigger : CustomUIEventListener
{
    public override bool OnClickBeginEvent()
    {
        if (!IsActive) return false;
        
        CustomUIEventCaster.GetInstance.LastUIEventInfo.IsActive = false;
        SoundManager.GetInstance.Play_UI_Sfx(K514SfxStorage.BeepType.Skip);

        DialogManager.GetInstance.ExitScript();
        
        return true;
    }

    public override bool OnClickEndEvent()
    {
        return true;
    }
}