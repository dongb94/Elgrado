using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIFontManager : Singleton<UIFontManager>
{
    #region <Fields>
    
    [SerializeField] private CustomUIFont _baseFont;
    [SerializeField] private Transform _parentTransform;
    
    private Stack<CustomUIFont> _waitingFontStack;

    private SizeOfCharacter[] _sizeOfCharacter;

    #endregion
    
    private enum TypeOfCharacter
    {
        n0 = '0', n1, n2, n3, n4, n5, n6, n7, n8, n9,
        A = 'A', B, C, D, E, F, G, H, I, J, K, L, M, N ,O, P, Q, R, S, T, U, V, W, X, Y, Z,
        a = 'a', b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z,
        Count
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        _waitingFontStack = new Stack<CustomUIFont>();
        
        _sizeOfCharacter = new SizeOfCharacter[(int)TypeOfCharacter.Count];

        #region <SetCharacterSize>

        #region <Numbers>

        _sizeOfCharacter['0'] = new SizeOfCharacter()
        {
            size = 15
        };
        _sizeOfCharacter['1'] = new SizeOfCharacter()
        {
            size = 13
        };
        _sizeOfCharacter['2'] = new SizeOfCharacter()
        {
            size = 15
        };
        _sizeOfCharacter['3'] = new SizeOfCharacter()
        {
            size = 16
        };
        _sizeOfCharacter['4'] = new SizeOfCharacter()
        {
            size = 18
        };
        _sizeOfCharacter['5'] = new SizeOfCharacter()
        {
            size = 16
        };
        _sizeOfCharacter['6'] = new SizeOfCharacter()
        {
            size = 16
        };
        _sizeOfCharacter['7'] = new SizeOfCharacter()
        {
            size = 16
        };
        _sizeOfCharacter['8'] = new SizeOfCharacter()
        {
            size = 17
        };
        _sizeOfCharacter['9'] = new SizeOfCharacter()
        {
            size = 16
        };

        #endregion
        
        #endregion

        // pooling 20 font when lodding
        for (var i = 0; i < 20; i++)
        {
            var initializedPooledFont = Instantiate();
            Pooling(initializedPooledFont);
        }
    }

    public CustomUIFont[] GetFont(string str, Vector3 position, float size = 1f)
    {
        var charArr = str.ToCharArray();
        var fontGroup = new CustomUIFont[charArr.Length];
        for(var i = 0; i < charArr.Length; i++)
        {
            var font = _waitingFontStack.Count != 0 ? _waitingFontStack.Pop() : Instantiate();
            font.SetActive(CustomUIRoot.ActiveType.Enable);
            font.SetFont(charArr[i].ToString(), _sizeOfCharacter[charArr[i]].size)
                .SetPosition(position)
                .SetSize(size);
            
            position += Vector3.right * font.Wide;
            fontGroup[i] = font;
        }
        return fontGroup;
    }

    public void SetFontOverUI(CustomUIFont[] fontGroup, UISprite sprite, Vector3 localPosition)
    {
        foreach (var font in fontGroup)
        {
            font.SetDepth(sprite.depth + 1);
            font.transform.parent = sprite.transform;
            font.SetPosition(localPosition);
            localPosition += Vector3.right * font.Wide;
        }
    }
    
    public void SetFontOverUI(CustomUIFont font, UISprite sprite, Vector3 localPosition)
    {
        font.SetDepth(sprite.depth + 1);
        font.transform.parent = sprite.transform;
        font.SetPosition(localPosition);
    }
    
    public void Pooling(CustomUIFont[] pooledFontGroup)
    {
        foreach (var pooledFont in pooledFontGroup)
        {
            pooledFont.OnPooling();
            pooledFont.transform.parent = _parentTransform;
            _waitingFontStack.Push(pooledFont);
        }
    }

    public void Pooling(CustomUIFont pooledFont)
    {
        pooledFont.OnPooling();
        pooledFont.transform.parent = _parentTransform;
        _waitingFontStack.Push(pooledFont);
    }

    private CustomUIFont Instantiate()
    {
        var font = Instantiate(_baseFont);
        font.transform.parent = _parentTransform;
        font.SetSize(1f);

        return font;
    }
       
    private struct SizeOfCharacter
    {
        public int size;
    }
}
