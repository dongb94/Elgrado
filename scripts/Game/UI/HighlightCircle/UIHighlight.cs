using System;
using UnityEngine;

public class UIHighlight : CustomUIRoot
{
    public UISprite HighlightCircle;

    [SerializeField] private Collider2D _highlightCircleCollider; 

    [SerializeField] private HighlightClickEventTrigger _clickEventTrigger;
    
    private float _scale;
    private float _scaleMultiplier;
    
    //
    #region <Unity/CallBack>

    private void Awake()
    {
        base.Awake();

        Scale = 1.0f;
        ScaleMultiplier = 1.0f;
    }

    private void FixedUpdate()
    {
        if(!_clickEventTrigger.IsActive) return;
        ScaleMultiplier = Math.Max(0.6f, (ScaleMultiplier + 0.5f * Time.fixedDeltaTime) % 1.5f);
    }

    #endregion
    //
    #region <CallBack>

    public override void SetActive(ActiveType active)
    {
        base.SetActive(active);
        _clickEventTrigger.SetActive(active);
    }

    #endregion
    //
    #region <Function>

    public void SetHighlightCircle(Vector3 position, float scale, CustomUIEventListener parent = null)
    {
        HUDManager.GetInstance.State = HUDManager.HUDState.Highlight;
        
        CirclePosition = position;
        Scale = scale;

        _highlightCircleCollider.enabled = parent == null;
        
        parent?.SetActive(ActiveType.Enable);
        
        parent?.SetOnClickBeginAfterAction(args =>
        {
            HUDManager.GetInstance.Highlight._clickEventTrigger.OnClickBeginEvent();
        });
    }

    #endregion
    //
    #region <Properties>

    public Vector3 CirclePosition
    {
        get => HighlightCircle.transform.position;
        set
        {
            HighlightCircle.transform.position = value;
            _highlightCircleCollider.transform.position = value;
        }
    }
    
    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            HighlightCircle.transform.localScale = Vector3.one * _scale * _scaleMultiplier;
            _highlightCircleCollider.transform.localScale = Vector3.one * _scale;
        }
    }

    private float ScaleMultiplier
    {
        get => _scaleMultiplier;
        set
        {
            _scaleMultiplier = value;
            HighlightCircle.transform.localScale = Vector3.one * _scale * _scaleMultiplier;

            HighlightCircle.alpha = 1 - (ScaleMultiplier - 0.8f) * 1.3f;
        }
    }
    
    #endregion
}