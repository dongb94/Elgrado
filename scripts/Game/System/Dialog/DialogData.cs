
using System.Collections.Generic;

public class DialogData
{
    public string EventName;
    public DialogManager.Character RightCharacter;
    public DialogManager.Character LeftCharacter;
    public bool RightActive;
    public bool LeftActive;
    public string Name;
    public Queue<string> Text;

    public DialogData(Queue<string> text)
    {
        Text = text;
    }
}
