using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// TODO Byeon : JSON 파일 형식 확정
/// </summary>
public class DialogManager : Singleton<DialogManager>
{
	private string basePath = "Assets/Resources/Script/";
	private Language currentLanguage;

	private Queue<DialogData> currentDialog;
	private DialogData currentScript;

	private Action<EventArgs> _onExitAction;
	
	#region <enum>
	public enum Character
	{
		NULL,
		Empty,
		Default,
		Katarina,
		Psyous,
		Hanagi,
		count,
	}

	public enum Language
	{
		Korean,
		count
	}
	#endregion

	#region <Unity/callback>
	#endregion

	#region <Callback>

	protected override void Initialize()
	{
		base.Initialize();
		currentLanguage = Language.Korean;
	}

	#endregion

	#region <Function>

	public void FRead(string path)
	{
	#if !UNITY_EDITOR
		if (Application.platform == RuntimePlatform.Android)
		{
			basePath = Application.persistentDataPath;
			basePath = basePath.Substring(0, basePath.LastIndexOf('/') + 1);
		}
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			basePath = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
			basePath = basePath.Substring(0, basePath.LastIndexOf('/') + 1);
			basePath = Path.Combine(basePath, "Documents/");
		}
	#endif
		
		var finalPath = basePath + currentLanguage.ToString() + "/" + path;
		
		using (var r = new StreamReader(finalPath))
		{
			var json = r.ReadToEnd();
			currentDialog = DialogParser.Parse(json);
		}
	}

	public void SetLanguage(Language language)
	{
		currentLanguage = language;
		
		/// TODO : sync()호출하여 인터페이스 언어 변경
	}

	public void PlayScript(string path, Action<EventArgs> exitEvent = null)
	{
		_onExitAction = exitEvent;
		
		FRead(path);
		
		if (currentDialog.Count == 0)
		{
			ExitScript();
			return;
		}

		currentScript = currentDialog.Dequeue();

		StartScript();
		DisplayScene();
		
	}

	private void DisplayScene(DialogData data = null)
	{
		if (data == null) data = currentScript;
		else currentScript = data;
		
		if (data.LeftCharacter != Character.NULL)
			HUDManager.GetInstance.DialogUI.SetLeftImage(data.LeftCharacter, data.LeftActive);

		if (data.RightCharacter != Character.NULL)
			HUDManager.GetInstance.DialogUI.SetRightImage(data.RightCharacter, data.RightActive);
		
		if (data.Name != null)
			HUDManager.GetInstance.DialogUI.SetName(data.Name);
		
		if (data.Text.Count != 0)
			HUDManager.GetInstance.DialogUI.SetDialog(data.Text.Dequeue());
	}

	private void DisplayNextText()
	{
		HUDManager.GetInstance.DialogUI.SetDialog(currentScript.Text.Dequeue());
	}

	public void NextEvent()
	{
		if (currentScript.Text.Count == 0)
		{
			if (currentDialog.Count == 0)
			{
				ExitScript();
				return;
			}

			currentScript = currentDialog.Dequeue();
			DisplayScene();
			return;
		}
		
		DisplayNextText();
	}

	public void StartScript()
	{
		HUDManager.GetInstance.State = HUDManager.HUDState.Script;
	}

	public void ExitScript()
	{
		HUDManager.GetInstance.Deactivate = HUDManager.HUDState.Script;
		_onExitAction?.Invoke(new EventArgs());
	}
	
	#endregion
}