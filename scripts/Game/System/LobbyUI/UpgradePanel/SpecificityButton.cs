
using UnityEditor;
using UnityEngine;

public class SpecificityButton :MonoBehaviour
{
    public SpecificityManager.Specificity Specificity;
    public UILabel MaxLevel;
    public UILabel CurrentLevel;

    public void AwakeButton()
    {
        CurrentLevel.text = PlayerPrefs.GetInt(Specificity.ToString()).ToString();
        MaxLevel.text = SpecificityManager.GetInstance.MaxLevel[(int) Specificity].ToString();
    }

    public void OnClick()
    {
        // TODO Highlight This Button
        
        UpgradePanel.GetInstance.UpdateFocusedButton(this);
    }
}