using UnityEngine;

public class SkillSelectButton : MonoBehaviour
{
    public ButtonKind Kind;
    public int SkillSlotNum;

    public enum ButtonKind
    {
        Slot,
        Selector
    }

    private void Awake()
    {
        if (Kind == ButtonKind.Selector &&
            SkillSlotNum >= PlayerChampionHandler.Handle.SpellGroup.Count)
            transform.localPosition += Vector3.up*Screen.height;
    }
}