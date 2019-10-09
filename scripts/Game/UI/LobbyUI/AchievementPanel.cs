
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class AchievementPanel : Singleton<AchievementPanel>
{
    public void AchievementPanelCall()
    {
        if(!Social.localUser.authenticated) Login.AutoLogin();
#if PLATFORM_ANDROID
        PlayGamesPlatform.Instance.ShowAchievementsUI();
#elif UNITY_IOS
        // show game center achievementsUI
#endif
    }
}
