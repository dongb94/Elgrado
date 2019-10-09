
using System;

public class UIMoneyBar:CustomUIRoot
{
    public UILabel MoneyBar;

    public void Sync()
    {
        var moneyChange = K514PooledCoroutine.GetCoroutine().GetDeferredAction(0.3f,
            refs =>
            {
                var moneyBar = refs.MorphObject as UILabel;
                moneyBar.text = PlayerItemManager.GetInstance.WaveStone.ToString();
            }
        ).SetAction(K514PooledCoroutine.ActionType.Activity,
            refs =>
            {
                var moneyBar = refs.MorphObject as UILabel;
                moneyBar.text = ((int)(refs.I_factor +
                                (PlayerItemManager.GetInstance.WaveStone - refs.I_factor) *
                                refs.F_stack_factor * refs.F_Time_ReversedFactor)).ToString();
            });

        moneyChange._mParams.SetMorphable(MoneyBar);
        moneyChange._mParams.SetFactor(int.Parse(MoneyBar.text));
        moneyChange.SetTrigger();
    }
}