
using GameData;

public class SkillData : GameData<SkillData>
{
    public int id { get; protected set; }
    public string Name { get; protected set; }
    public string Explanation { get; protected set; }
    
    public static readonly string fileName = "Data/XML/SkillInformation";

    public static SkillData GetData(int id)
    {
        if (dataMap.ContainsKey(id)) return dataMap[id];
        return null;
    }
}
