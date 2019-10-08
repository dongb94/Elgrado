
using System;
using UnityEngine;

public class TutorialManager : Singleton<TutorialManager>
{
    private const string TutorialScriptPath = "Tutorial/";

    public enum TutorialName
    {
        Move,
        Attack,
        ChargeAttack,
        ActiveSkill,
        Dash,
        Count        
    }

    public void PlayTutorial(TutorialName name)
    {
        GameManager.GetInstance.GameStopFlag = true;

        DialogManager.GetInstance.PlayScript(TutorialScriptPath + name.ToString() + ".json", TutorialEvent(name));
    }

    private Action<EventArgs> TutorialEvent(TutorialName name)
    {
        Action<EventArgs> action;
        switch (name)
        {
            case TutorialName.Move :
                action = (args) =>
                {
                    GameManager.GetInstance.GameStopFlag = true;
                    HUDManager.GetInstance.Highlight.SetHighlightCircle(
                        HUDManager.GetInstance.JoystickController.transform.position, 2.0f,
                        HUDManager.GetInstance.JoystickController);
                    HUDManager.GetInstance.JoystickController.SetOnClickBeginAfterAction((onClick) =>
                    {
                        GameManager.GetInstance.GameStopFlag = false;
                        HUDManager.GetInstance.Background.LeftScreenHighlight(false);
                    });
                    HUDManager.GetInstance.Background.LeftScreenHighlight(true);
                    MessageBoxUI.GetInstance.SetLabel("조이스틱을 움직여 이동하세요");
                    MessageBoxUI.GetInstance.SetTrigger(SceneEnvironmentContoller.GetInstance.LocalSystem,
                        MessageBoxUI.MessageActionType.Showing, MessageBoxUI.MessageEventType.None);
                };
                break;
            case TutorialName.Attack :
                action = (args) =>
                {
                    GameManager.GetInstance.GameStopFlag = true;
                    HUDManager.GetInstance.Highlight.SetHighlightCircle(
                        HUDManager.GetInstance.ActionButtonTriggerGroup[0].transform.position, 1.5f,
                        HUDManager.GetInstance.ActionButtonTriggerGroup[0]);
                    HUDManager.GetInstance.ActionButtonTriggerGroup[0].SetOnClickBeginBeforeAction((onClick) =>
                    {
                        GameManager.GetInstance.GameStopFlag = false;
                        HUDManager.GetInstance.Background.UIHighlight(false);
                    });
                    HUDManager.GetInstance.Background.UIHighlight(true);
                    MessageBoxUI.GetInstance.SetLabel("공격버튼을 눌러 공격하세요");
                    MessageBoxUI.GetInstance.SetTrigger(SceneEnvironmentContoller.GetInstance.LocalSystem,
                        MessageBoxUI.MessageActionType.Showing, MessageBoxUI.MessageEventType.None);
                };
                break;
            case TutorialName.Dash :
                action = (args) =>
                {
                    GameManager.GetInstance.GameStopFlag = true;
                    HUDManager.GetInstance.Highlight.SetHighlightCircle(
                        HUDManager.GetInstance.ActionButtonTriggerGroup[2].transform.position, 1.5f,
                        HUDManager.GetInstance.ActionButtonTriggerGroup[2]);
                    HUDManager.GetInstance.ActionButtonTriggerGroup[2].SetOnClickBeginBeforeAction((onClick) =>
                    {
                        GameManager.GetInstance.GameStopFlag = false;
                        HUDManager.GetInstance.Background.UIHighlight(false);
                    });
                    HUDManager.GetInstance.Background.UIHighlight(true);
                    MessageBoxUI.GetInstance.SetLabel("대쉬버튼을 눌러 회피하세요");
                    MessageBoxUI.GetInstance.SetTrigger(SceneEnvironmentContoller.GetInstance.LocalSystem,
                        MessageBoxUI.MessageActionType.Showing, MessageBoxUI.MessageEventType.None);
                };

                break;
            case TutorialName.ChargeAttack :
                action = (args) =>
                {
                    GameManager.GetInstance.GameStopFlag = true;
                    HUDManager.GetInstance.Highlight.SetHighlightCircle(
                        HUDManager.GetInstance.ActionButtonTriggerGroup[0].transform.position, 1.5f,
                        HUDManager.GetInstance.ActionButtonTriggerGroup[0]);
                    HUDManager.GetInstance.ActionButtonTriggerGroup[0].SetOnClickBeginBeforeAction((onClick) =>
                    {
                        GameManager.GetInstance.GameStopFlag = false;
                        HUDManager.GetInstance.Background.UIHighlight(false);
                    });
                    HUDManager.GetInstance.Background.UIHighlight(true);
                    MessageBoxUI.GetInstance.SetLabel("공격버튼을 누르고 있으면 차징 할 수 있습니다.");
                    MessageBoxUI.GetInstance.SetTrigger(SceneEnvironmentContoller.GetInstance.LocalSystem,
                        MessageBoxUI.MessageActionType.Showing, MessageBoxUI.MessageEventType.None);
                };

                break;
            case TutorialName.ActiveSkill :
                action = (args) =>
                {
                    GameManager.GetInstance.GameStopFlag = true;
                    HUDManager.GetInstance.Highlight.SetHighlightCircle(
                        HUDManager.GetInstance.ActionButtonTriggerGroup[1].transform.position, 1.5f,
                        HUDManager.GetInstance.ActionButtonTriggerGroup[1]);
                    HUDManager.GetInstance.ActionButtonTriggerGroup[1].SetOnClickBeginBeforeAction((onClick) =>
                    {
                        GameManager.GetInstance.GameStopFlag = false;
                        HUDManager.GetInstance.Background.UIHighlight(false);
                    });
                    HUDManager.GetInstance.Background.UIHighlight(true);
                    MessageBoxUI.GetInstance.SetLabel("스킬버튼을 눌러 스킬을 사용하세요");
                    MessageBoxUI.GetInstance.SetTrigger(SceneEnvironmentContoller.GetInstance.LocalSystem,
                        MessageBoxUI.MessageActionType.Showing, MessageBoxUI.MessageEventType.None);
                };

                break;
            default :
                action = null;
                break;
        }

        return action;
    }
}