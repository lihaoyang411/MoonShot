using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TeamBlack.MoonShot.Networking;

public class GameEndPacket
{
    public bool Win;
    public List<GameStats> stats;

    public GameEndPacket(byte[] fromByteArray)
    {
        var reader = new BinaryReader(new MemoryStream(fromByteArray));

        int length = reader.ReadInt32();
        stats = new List<GameStats>();
        for(int i = 0; i < length; i++)
        {   
            stats.Add(new GameStats(reader));
        }
    }
    public GameEndPacket(IEnumerable<GameStats> s) 
    {
        stats = new List<GameStats>(s);
    }
    
    public byte[] ByteArray()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        
        writer.Write(stats.Count);
        
        foreach (var stat in stats) 
            writer.Write(stat.ToByteArray());

        writer.Close();
        return stream.ToArray();
    }
}

public class GameStats
{
    public struct TimeData<T> 
    {
        public long Frame;
        public T Val;

        public TimeData(long frame, T val)
        {
            Frame = frame;
            Val = val;
        }
    }

    public ServerFactionManager faction;

    public GameStats(ServerFactionManager fac) {
        faction = fac;
    }

    public List<TimeData<int>> NumSoldiers = new List<TimeData<int>>();
    public List<TimeData<int>> NumMiners = new List<TimeData<int>>();
    public List<TimeData<int>> NumDiggers = new List<TimeData<int>>();
    public List<TimeData<int>> NumHaulers = new List<TimeData<int>>();
    public List<TimeData<int>> NumDeployables = new List<TimeData<int>>();
    public List<TimeData<int>> NumCredits = new List<TimeData<int>>();
    
    public GameStats(byte[] v)
    {
        var reader = new BinaryReader(new MemoryStream(v));

        NumSoldiers = HelperFromByteArray(reader);
        NumMiners = HelperFromByteArray(reader);
        NumDiggers = HelperFromByteArray(reader);
        NumHaulers = HelperFromByteArray(reader);
        NumDeployables = HelperFromByteArray(reader);
        NumCredits = HelperFromByteArray(reader);

        reader.Close();
    }
    public GameStats(BinaryReader reader)
    {
        NumSoldiers = HelperFromByteArray(reader);
        NumMiners = HelperFromByteArray(reader);
        NumDiggers = HelperFromByteArray(reader);
        NumHaulers = HelperFromByteArray(reader);
        NumDeployables = HelperFromByteArray(reader);
        NumCredits = HelperFromByteArray(reader);
    }

    public byte[] ToByteArray()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        
        HelperToByteArray(writer, NumSoldiers);
        HelperToByteArray(writer, NumMiners);
        HelperToByteArray(writer, NumDiggers);
        HelperToByteArray(writer, NumHaulers);
        HelperToByteArray(writer, NumDeployables);
        HelperToByteArray(writer, NumCredits);

        writer.Close();

        return stream.ToArray();
    }
    private void HelperToByteArray(BinaryWriter writer, List<TimeData<int>> list) 
    {
        writer.Write(list.Count);

        foreach (TimeData<int> c in list)
        {
            writer.Write(c.Frame);
            writer.Write(c.Val);
        }

    }

    private List<TimeData<int>> HelperFromByteArray(BinaryReader reader)
    {
        int length = reader.ReadInt32();

        List<TimeData<int>> TimeDataValues = new List<TimeData<int>>();

        for (int i = 0; i < length; i++)
        {
            TimeDataValues.Add(
                new TimeData<int>(
                    reader.ReadInt64(),
                    reader.ReadInt32()
                ));
        }
        return TimeDataValues;
    }

    public void Scan(long frame) 
    {
        int prevNumSoldiers =    (NumSoldiers.Count == 0)    ? -1 : NumSoldiers[NumSoldiers.Count -1].Val;
        int prevNumMiners =      (NumMiners.Count == 0)      ? -1 : NumMiners[NumMiners.Count -1].Val;
        int prevNumDiggers =     (NumDiggers.Count == 0)     ? -1 : NumDiggers[NumDiggers.Count -1].Val;
        int prevNumHaulers =     (NumHaulers.Count == 0)     ? -1 : NumHaulers[NumHaulers.Count -1].Val;
        int prevNumDeployables = (NumDeployables.Count == 0) ? -1 : NumDeployables[NumDeployables.Count -1].Val;
        int prevNumCredit =      (NumCredits.Count == 0)     ? -1 : NumCredits[NumCredits.Count -1].Val;
        
        int curNumSoldiers =    0;
        int curNumMiners =      0; 
        int curNumDiggers =     0;
        int curNumHaulers =     0; 
        int curNumDeployables = 0;
        int curNumCredit =      faction.Credits; 

        
        for(int i = 0; i < faction.Entities.Length; i++)
        {
            if(faction.Entities[i] == null)
                continue;

            Soldier s = faction.Entities[i] as Soldier;
            if(s != null)
            {
                curNumSoldiers++;
                continue;
            }
            
            Miner m = faction.Entities[i] as Miner;
            if(m != null)
            {
                curNumMiners++;
                continue;
            }
            Hauler h = faction.Entities[i] as Hauler;
            if(h != null)
            {
                curNumHaulers++;
                continue;
            }
            Digger d = faction.Entities[i] as Digger;
            if(d != null)
            {
                curNumDiggers++;
                continue;
            }
            Deployable dep = faction.Entities[i] as Deployable;
            if(dep != null)
            {
                curNumDeployables++;
                continue;
            }
        }
        
        if (prevNumSoldiers != curNumSoldiers) 
            NumSoldiers.Add(new TimeData<int>(frame, curNumSoldiers));
        if (prevNumMiners != curNumMiners) 
            NumMiners.Add(new TimeData<int>(frame, curNumMiners));
        if (prevNumHaulers != curNumHaulers) 
            NumHaulers.Add(new TimeData<int>(frame, curNumHaulers));
        if (prevNumDiggers != curNumDiggers) 
            NumDiggers.Add(new TimeData<int>(frame, curNumDiggers));
        if (prevNumDeployables != curNumDeployables) 
            NumSoldiers.Add(new TimeData<int>(frame, curNumDeployables));
        if (prevNumCredit != curNumCredit) 
            NumSoldiers.Add(new TimeData<int>(frame, curNumCredit));
    }
}
