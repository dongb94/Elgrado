using UnityEngine;

/// <summary>
/// Define customizing NGUI
/// UI top level class 
/// </summary>
public class CustomUIRoot : MonoBehaviour
{
    public UISprite[] SpriteGroup;
    public UILabel[] LabelGroup;

    protected bool UIEnable;
    
    #region <Enum>

    public enum ActiveType
    {
        Removed,    // Remove from screen (object not removed)
        Disable,    // Darkening, Disable state
        Enable      // Active state
    }

    #endregion

    protected virtual void Awake()
    {
        UIEnable = true;
    }

    public virtual void SetActive(ActiveType active)
    {
        switch (active)
        {
            case ActiveType.Removed :
                gameObject.SetActive(false);
                break;
            case ActiveType.Disable :
                
                gameObject.SetActive(true);
                if (!UIEnable) break;
                
                UIEnable = false;
                foreach (var sprite in SpriteGroup)
                {
                    sprite.color = new Color(sprite.color.r/2, sprite.color.g/2, sprite.color.b/2, sprite.color.a);
                }
                foreach (var sprite in LabelGroup)
                {
                    sprite.color = new Color(sprite.color.r/2, sprite.color.g/2, sprite.color.b/2, sprite.color.a);
                }
                break;
            case ActiveType.Enable :
                
                gameObject.SetActive(true);
                if (UIEnable) break;

                UIEnable = true;
                foreach (var sprite in SpriteGroup)
                {
                    sprite.color = new Color(sprite.color.r*2, sprite.color.g*2, sprite.color.b*2, sprite.color.a);
                }
                foreach (var sprite in LabelGroup)
                {
                    sprite.color = new Color(sprite.color.r*2, sprite.color.g*2, sprite.color.b*2, sprite.color.a);
                }
                break;
            default:
                break;
        }
    }
}