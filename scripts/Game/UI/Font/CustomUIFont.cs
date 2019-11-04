
using System;
using UnityEngine;

public class CustomUIFont : CustomUIRoot
{
    [SerializeField] private UISprite _sprite;
    private float _size;
    private int _characterWideValue; // Difference in width depending on the type of characters (Pixel)

    public void OnPooling()
    {
        _size = 1;
        _characterWideValue = 1;
        _sprite.depth = -1;
        _sprite.color = Color.white;
        SetActive(ActiveType.Removed);
    }

    public CustomUIFont SetFont(string character, int wide)
    {
        _sprite.spriteName = character;
        _characterWideValue = wide;
        _sprite.width = wide;

        return this;
    }

    public CustomUIFont SetSize(float size)
    {
        _size = size;
        transform.localScale = Vector3.one * size;

        return this;
    }

    public CustomUIFont SetPosition(Vector3 position)
    {
        transform.localPosition = position;

        return this;
    }

    public CustomUIFont SetDepth(int depth)
    {
        _sprite.depth = depth;
        
        return this;
    }

    public UISprite Sprite => _sprite;
    public float Size => _size;
    public float Wide => _size * _characterWideValue;
}

