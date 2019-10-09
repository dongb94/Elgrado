using UnityEngine;

namespace Game.UI.LobbyUI
{
    public class StageButton : MonoBehaviour
    {
        public LoadingSceneManager.TargetMainScene Stage;

        private void Awake()
        {
            if(PlayerPrefs.GetInt("ClearedStageNumber") < (int)Stage) gameObject.SetActive(false);
        }
    }
}