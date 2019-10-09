
using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.SceneManagement;

public class GameReadyPanel : MonoBehaviour
{
    #region <Consts>

    public const string SceneDirectoryHead = "Scenes/MainActivity/";
    public const string OnStar = "Star_On";
    public const string OffStar = "Star_Off";

    #endregion

    #region <Field>

    public LoadingSceneManager.TargetMainScene TargetScene;
    
    public UILabel[] MissionLabel;
    public UISprite[] MissionSprite;

    public RankingPanel RankingPanel;

    #endregion

    #region <Function>

    public void MissionSet()
    {
        for (var i = 0; i < 3; i++)
        {
            MissionSet(i);
        }
    }
    
    public void MissionSet(int num)
    {
        if (num >= MissionLabel.Length || num < 0) return;
        
        var data = MissionData.GetData((int) TargetScene);
        if (data == null)
        {
            MissionLabel[num].text = "목표가 지정되지 않음";
            return;
        }

        int missionCode, value;
        switch (num)
        {
            case 0:
                missionCode = data.Mission1;
                value = data.Var1;
                break;
            case 1:
                missionCode = data.Mission2;
                value = data.Var2;
                break;
            case 2:
                missionCode = data.Mission3;
                value = data.Var3;
                break; 
            default:
                missionCode = 0;
                value = 0;
                break;
        }

        switch (missionCode)
        {
            case 1 :
                MissionLabel[num].text = "점수 "+value+"점 이상 획득";
                break;
            case 2 :
                MissionLabel[num].text = "시간 "+value+"초 안에 클리어";
                break;
            case 3 :
                MissionLabel[num].text = "체력 "+value+"% 이상으로 클리어";
                break;
            case 4 :
                MissionLabel[num].text = value+"회 이하 피격";
                break;
            case 5 :
                MissionLabel[num].text = "스킬을 사용하지 않고 클리어";
                break;
            case 6 :
                MissionLabel[num].text = "차지샷 사용하지 않고 클리어";
                break;
            case 7 :
                MissionLabel[num].text = "스킬 "+value+"회 이하로 사용하고 클리어";
                break;
            case 8 :
                MissionLabel[num].text = "차지샷 "+value+"회 이사로 사용하고 클리어";
                break;
            case 9 :
                MissionLabel[num].text = "회복 오브젝트 "+value+"개 이상 획득";
                break;
            case 10 :
                MissionLabel[num].text = "점수 오브젝트 "+value+"개 이상 획득";
                break;
            default:
                MissionLabel[num].text = "목표가 지정되지 않음";
                break;
        }
        
        var clear = PlayerPrefs.GetInt(TargetScene.ToString() + "Mission" + num);
        MissionSprite[num].spriteName = clear == 1 ? OnStar : OffStar;
    }

    #endregion

    #region <ButtonEvent>
    
    public void StageStart()
    {
        LobbyUIController.GetInstance.system.gameObject.SetActive(true);
        SceneManager.LoadScene(SceneDirectoryHead + TargetScene.ToString());
//        LoadingSceneManager.LoadScene(SceneDirectoryHead + TargetScene.ToString(),
//        new PlayerChampionHandler.ChampionBuffer(PlayerChampionHandler.ChampionType.Sietra));
    }

    public void ChangeRankSort()
    {
        // TODO switching ranking sorting between number one and player
    }

    #endregion
}