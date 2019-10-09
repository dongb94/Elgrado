using UnityEngine;

/// <summary>
/// Define customizing NGUI
/// UI top level class 
/// </summary>
public class CustomUIRoot : MonoBehaviour
{
    public UISprite[] SpriteGroup;
    public UILabel[] LabelGroup;

    protected bool IsActive;
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
        IsActive = true;
        UIEnable = true;
        SetActive(ActiveType.Removed);
    }

    public virtual void SetActive(ActiveType active)
    {
        switch (active)
        {
            case ActiveType.Removed :
                if (!IsActive) break;
                IsActive = false;
                transform.localPosition -= Vector3.up * 2048;
                break;
            case ActiveType.Disable :
                if (!IsActive)
                {
                    IsActive = true;
                    transform.localPosition += Vector3.up * 2048;
                }
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
                if(!IsActive)
                {
                    IsActive = true;
                    transform.localPosition += Vector3.up * 2048;          
                }
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