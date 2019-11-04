
using UnityEngine;
// If font location is strange, check font's parent object position
public class UIDamageFont : MonoBehaviour
{
    private Camera _mainCamera;

    public void SetMainCamera()
    {
        _mainCamera = CameraManager.GetInstance.MainCamera;
    }

    public void PrintDamageFont(int damage, Vector3 position)
    {
        var damageFont = UIFontManager.GetInstance.GetFont(damage.ToString(),
            _mainCamera.WorldToScreenPoint(position));

        var AsyncFadeAction = K514PooledCoroutine.GetCoroutine().GetDeferredAction(1.0f, refs =>
        {
            var _damageFont = refs.MorphObject as CustomUIFont[];
            foreach (var font in _damageFont)
            {
                font.Sprite.alpha = 0f;
                UIFontManager.GetInstance.Pooling(font);
            }
        }).SetAction(K514PooledCoroutine.ActionType.Activity, refs =>
        {
            var _damageFont = refs.MorphObject as CustomUIFont[];
            var fixedPosition = CameraManager.GetInstance.MainCamera.WorldToScreenPoint(refs.CastPosition);
            foreach (var font in _damageFont)
            {
                font.Sprite.alpha = 1 - refs.F_stack_factor * refs.F_Time_ReversedFactor;
                font.SetPosition(fixedPosition);
                fixedPosition += Vector3.right * font.Wide;
            }
        });

        AsyncFadeAction._mParams.SetCastPosition(position);
        AsyncFadeAction._mParams.SetMorphable(damageFont);

        AsyncFadeAction.SetTrigger();

    }
}