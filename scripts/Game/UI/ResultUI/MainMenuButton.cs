
public class MainMenuButton : CustomUIEventListener
{
    protected override void Awake()
    {
        base.Awake();
        
        SetActive(ActiveType.Disable);
    }
    
    public override void SetActive(ActiveType active)
    {
        base.SetActive(active);
        if(active == ActiveType.Enable) SpriteGroup[0].spriteName = "ButtonActive";
        else SpriteGroup[0].spriteName = "ButtonUnactive";
    }

    public override bool OnClickBeginEvent()
    {
        throw new System.NotImplementedException();
    }

    public override bool OnClickEndEvent()
    {
        throw new System.NotImplementedException();
    }
}