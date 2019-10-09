
using System;
using System.Xml;
using UnityEngine;

public class UpgradePanel : Singleton<UpgradePanel>
{
    public SpecificityButton FocusedButton;
    public SpecificityManager.Specificity FocusedSpecificity;

    public SpecificityButton[] ButtonGroup;

    // explanation of FocusedSpecificity
    public UISprite Icon;
    public UILabel Name;
    public UILabel Explanation;
    
    private string[] _name;
    private string[] _explanation;

    protected override void Initialize()
    {
        base.Initialize();
        _name = new string[(int)SpecificityManager.Specificity.Count];
        _explanation = new string[(int)SpecificityManager.Specificity.Count];
        readXML();
        foreach (var button in ButtonGroup)
        {
            button.AwakeButton();
        }
    }

    public void readXML()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/XML/Specificity");
        XmlDocument document = new XmlDocument();
        document.LoadXml(textAsset.text);

        XmlNodeList nodes = document.SelectNodes("/Specificity");

        foreach (XmlNode Specificity in nodes)
        {
            foreach (XmlNode classification in Specificity.ChildNodes)
            {
                foreach (XmlNode level in classification.ChildNodes)
                {
                    var index = Enum.Parse(typeof(SpecificityManager.Specificity),level.SelectSingleNode("enum").InnerText);
                    
                    _name[(int)index] = level.SelectSingleNode("name")?.InnerText;
                    _explanation[(int)index] = level.SelectSingleNode("explanation")?.InnerText;
                    SpecificityManager.GetInstance.MaxLevel[(int) index] = int.Parse(level.SelectSingleNode("maxLevel")?.InnerText);
                    SpecificityManager.GetInstance.Cost[(int) index] = int.Parse(level.SelectSingleNode("cost")?.InnerText);
                    SpecificityManager.GetInstance.Var1[(int) index] = float.Parse(level.SelectSingleNode("var1")?.InnerText);
                    SpecificityManager.GetInstance.Var2[(int) index] = float.Parse(level.SelectSingleNode("var2")?.InnerText);

                }
            }
        }
    }

    public void UpdateFocusedButton(SpecificityButton specificity)
    {
        FocusedButton = specificity;
        FocusedSpecificity = specificity.Specificity;
        var index = (int) FocusedSpecificity;
        
        Name.text = _name[index];
        Explanation.text = _explanation[index];
    }

    #region <ButtonEvenet>

    public void IncreaseSpecificityLevel()
    {
        var result = SpecificityManager.GetInstance.ApplySpecificity(FocusedSpecificity, 
            SpecificityManager.GetInstance.CurrentLevel[(int)FocusedSpecificity]+1);
        if (result == -1) return;
        FocusedButton.CurrentLevel.text = result.ToString();
        PlayerPrefs.SetInt(FocusedSpecificity.ToString(), result);

    }
    
    public void DecreaseSpecificityLevel()
    {
        var result = SpecificityManager.GetInstance.ApplySpecificity(FocusedSpecificity, 
            SpecificityManager.GetInstance.CurrentLevel[(int)FocusedSpecificity]-1);
        if (result == -1) return;
        FocusedButton.CurrentLevel.text = result.ToString();
        PlayerPrefs.SetInt(FocusedSpecificity.ToString(), result);
    }

    public void ResetSpecificityLevel()
    {
        var result = SpecificityManager.GetInstance.ApplySpecificity(FocusedSpecificity, 0);
        if (result == -1) return;
        FocusedButton.CurrentLevel.text = result.ToString();
        PlayerPrefs.SetInt(FocusedSpecificity.ToString(), result);
    }

    #endregion
    
}