
public class RetryButton : CustomUIEventListener
{
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    public override void SetActive(ActiveType active)
    {
        base.SetActive(active);
        if(active == ActiveType.Enable) SpriteGroup[0].spriteName = "ButtonActive";
        else SpriteGroup[0].spriteName = "ButtonUnactive";
    }
    
    public override bool OnClickBeginEvent()
    {
        HUDManager.GetInstance.Deactivate = HUDManager.HUDState.GameResult;
        LoadingSceneManager.LoadCurrentScene();
        
        return true;
    }

    public override bool OnClickEndEvent()
    {
        return true;
    }
}