using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 
/// </summary>
/// TODO Byeon :
public class UIDialog : CustomUIRoot
{
	public UISprite RightImage;
	public UISprite LeftImage;
	public UILabel Name;
	public UILabel Script;
	public DialogSkipTrigger Skip;
	public DialogEventTrigger EventListener;
	
	#region <Unity/CallBack>

	private void Awake()
	{
		base.Awake();
	}

	#endregion

	#region <Function>

	public override void SetActive(ActiveType active)
	{
		base.SetActive(active);
		Skip.SetActive(active);
		EventListener.SetActive(active);
	}
	
	public void SetName(string name)
	{
		Name.text = name;
	}

	public void SetDialog(string dialog)
	{
		Script.text = dialog;
	}

	#region SetLeftImage
	public void SetLeftImage(DialogManager.Character leftImage)
	{
		LeftImage.spriteName = leftImage.ToString();
	}
	
	public void SetLeftImage(DialogManager.Character leftImage, bool Active)
	{
		LeftImage.spriteName = leftImage.ToString();
		LeftImage.color = Active ? Color.white : Color.gray;
	}
	
	public void SetLeftImage(bool Active)
	{
		LeftImage.color = Active ? Color.white : Color.gray;
	}
	#endregion

	#region SetRightImage
	public void SetRightImage(DialogManager.Character rightImage)
	{
		RightImage.spriteName = rightImage.ToString();
	}
	
	public void SetRightImage(DialogManager.Character rightImage, bool Active)
	{
		RightImage.spriteName = rightImage.ToString();
		RightImage.color = Active ? Color.white : Color.gray;
	}
	
	public void SetRightImage(bool Active)
	{
		RightImage.color = Active ? Color.white : Color.gray;
	}
	#endregion
	
	#endregion </Function>
	
}
