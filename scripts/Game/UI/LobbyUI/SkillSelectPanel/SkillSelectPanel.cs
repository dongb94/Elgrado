
using System;
using GameData;
using UnityEngine;

public class SkillSelectPanel : Singleton<SkillSelectPanel>
{
    public UIButton Skill1;
    public UIButton Skill2;

    public UISprite ExplanationSprite;
    public UILabel ExplanationLabel;
    
    private SkillSelectButton _focus;

    protected override void Initialize()
    {
        base.Initialize();
        Skill1.normalSprite = PlayerPrefs.GetString("Skill1 Name");
        Skill2.normalSprite = PlayerPrefs.GetString("Skill2 Name");
    }

    public void UpdateSkill(SkillSelectButton focus)
    {
        if(_focus == null)
            Focus = focus;
        else
        {
            SkillSelectButton slot,selector;
            switch (focus.Kind)
            {
                case SkillSelectButton.ButtonKind.Slot:
                    if (_focus.Kind == SkillSelectButton.ButtonKind.Slot)
                    {
                        Focus = focus;
                        return;
                    }
                    slot = focus;
                    selector = _focus;
                    break;
                case SkillSelectButton.ButtonKind.Selector:
                    if (_focus.Kind == SkillSelectButton.ButtonKind.Selector)
                    {
                        Focus = focus;
                        return;
                    }
                    slot = _focus;
                    selector = focus;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (slot.SkillSlotNum == 1)
            {
                Skill1.normalSprite = selector.GetComponent<UISprite>().spriteName;
                PlayerPrefs.SetString("Skill1 Name",Skill1.normalSprite);
                PlayerPrefs.SetInt("Skill1",selector.SkillSlotNum);
                PlayerChampionHandler.Handle.SetSpellAt(ActionButtonTriggerCluster.ActionTriggerType.PrimaryAction,selector.SkillSlotNum,true);
            }else if (slot.SkillSlotNum == 2)
            {
                Skill2.normalSprite = selector.GetComponent<UISprite>().spriteName;
                PlayerPrefs.SetString("Skill2 Name",Skill2.normalSprite);
                PlayerPrefs.SetInt("Skill2",selector.SkillSlotNum);
                PlayerChampionHandler.Handle.SetSpellAt(ActionButtonTriggerCluster.ActionTriggerType.SecondaryAction,selector.SkillSlotNum,true);
            }

            PlayerChampionHandler.Handle.InitiateSpell();
            Focus = null;
        }
    }
    
    public SkillSelectButton Focus
    {
        get => _focus;
        set
        {
            _focus = value;
            if (_focus == null) return; //TODO set highlight
            //TODO set highlight
            
            if(_focus.Kind==SkillSelectButton.ButtonKind.Selector) UpdateInformation(_focus.SkillSlotNum);
        }
    }

    public void UpdateInformation(int num)
    {
        var data = SkillData.GetData(num);
        ExplanationLabel.text = data.Explanation;
        ExplanationSprite.spriteName = _focus.GetComponent<UISprite>().spriteName;
    }
}