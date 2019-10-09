
using UnityEngine;

public class UIResultSceneAnimation : MonoBehaviour
{
    public Animator Frame;
    public Animator[] GearGroup;
    public Animator GameResult;
    public Animator GameInfo;

    public AnimationClip FrameAnimation;
    public AnimationClip GearAnimation;
    public AnimationClip ResultAnimation;
    public AnimationClip InfoAnimation;

    private void Awake()
    {
        Initialize();
    }

    public void PlayResultAnimations(int exp)
    {
        Frame.speed = 1;
        foreach (var gear in GearGroup)
        {
            gear.speed = 1;
        }

        var resultAnimation = K514PooledCoroutine.GetCoroutine().GetDeferredAction(2f,
            refs =>
            {
                var result = refs.MorphObject as Animator;
                var info = refs.MorphObject2 as Animator;
                result.speed = 1;
                info.speed = 1;
                
                HUDManager.GetInstance.ResultScene.UpdateExpBar(refs.I_factor);
            });

        resultAnimation._mParams.SetMorphable(GameResult);
        resultAnimation._mParams.SetMorphable2(GameInfo);
        resultAnimation._mParams.SetFactor(exp);
        resultAnimation.SetTrigger();
    }

    public void Initialize()
    {
        Frame.Rebind();
        Frame.speed = 0;
        foreach (var gear in GearGroup)
        {
            gear.Rebind();
            gear.speed = 0;
        }
        GameResult.Rebind();
        GameResult.speed = 0;
        GameInfo.Rebind();
        GameInfo.speed = 0;
    }
}