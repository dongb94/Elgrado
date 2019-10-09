using GameData;

public class MissionData : GameData<MissionData>
{
    public int id { get; protected set; }
    public int Mission1 { get; protected set; }
    public int Var1 { get; protected set; }
    public int Mission2 { get; protected set; }
    public int Var2 { get; protected set; }
    public int Mission3 { get; protected set; }
    public int Var3 { get; protected set; }
    
    public static readonly string fileName = "Data/XML/Mission";

    public static MissionData GetData(int id)
    {
        if (dataMap.ContainsKey(id)) return dataMap[id];
        return null;
    }
}