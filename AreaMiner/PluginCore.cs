using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AreaMiner.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.Protocols.Classes.Base;

namespace AreaMiner
{
    [Plugin(1, "Area miner", "Mines the area that is selected by the user.")]
    public class PluginCore : IStartPlugin
    {
        private static readonly ShareManager shares = new ShareManager();

        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[6];
            Setting[0] = new StringSetting("Start x y z", "", "0 0 0");
            Setting[1] = new StringSetting("End x y z", "", "0 0 0");
            Setting[2] = new StringSetting("Macro on inventory full", "Starts the macro when the bots inventory is full.", "");
            Setting[3] = new ComboSetting("Speed mode", null, new string[] {"Accurate", "Fast"}, 0);
            Setting[4] = new StringSetting("Ignore ids", "What blocks should be ignored.", "");
            Setting[5] = new ComboSetting("Path mode", null, new string[] {"Advanced (mining & building)", "Basic"}, 0);
        }

        public override PluginResponse OnEnable(IBotSettings botSettings) {
            if (!botSettings.loadWorld) return new PluginResponse(false, "'Load world' must be enabled.");
            if(string.IsNullOrWhiteSpace(Setting[0].Get<string>()) ||
               string.IsNullOrWhiteSpace(Setting[1].Get<string>())  ) return new PluginResponse(false, "No coordinates have been entered.");
            if (!Setting[0].Get<string>().Contains(' ') || !Setting[1].Get<string>().Contains(' ')) return new PluginResponse(false, "Invalid coordinates (does not contain ' ').");
            var startSplit = Setting[0].Get<string>().Split(' ');
            var endSplit = Setting[1].Get<string>().Split(' ');
            if (startSplit.Length != 3 || endSplit.Length != 3) return new PluginResponse(false, "Invalid coordinates (must be x y z).");

            return new PluginResponse(true);
        }
        public override void OnDisable() { shares?.Clear(); }

        public override void OnStart() {

            // Split the ids.
            var ids = Setting[4].Get<string>().Split(' ');
            List<ushort> ignoreIdList = new List<ushort>();
            for (int i = 0; i < ids.Length; i++)
            {
                ushort id;
                if (!ushort.TryParse(ids[i], out id))
                    continue;
                ignoreIdList.Add(id);
            }

            var startSplit = Setting[0].Get<string>().Split(' ');
            var endSplit = Setting[1].Get<string>().Split(' ');

            var macro = new MacroSync();

            RegisterTask(new InventoryMonitor(Setting[2].Get<string>(), macro));
            RegisterTask(new Path(shares,
                                  new Location(int.Parse(startSplit[0]), int.Parse(startSplit[1]), int.Parse(startSplit[2])),
                                  new Location(int.Parse(endSplit[0]), int.Parse(endSplit[1]), int.Parse(endSplit[2])),
                                  (PathMode) Setting[5].Get<int>(), macro
                                 ));
            RegisterTask(new Mine(shares, (Mode)Setting[3].Get<int>(), (PathMode)Setting[5].Get<int>(), ignoreIdList.ToArray(), macro));
        }
    }
}

public class ShareManager
{
    private readonly ConcurrentDictionary<IPlayer, SharedRadiusState> zones = new ConcurrentDictionary<IPlayer, SharedRadiusState>();
    private IRadius  radius;

    public void SetArea(IRadius radius) {
        this.radius = radius;
    }
    public void Add(IPlayer player, IRadius radius) {
        zones.TryAdd(player, new SharedRadiusState(radius));
        Calculate();
    }
    public void Clear() {
        zones.Clear();
    }

    public IRadius Get(IPlayer player) {
        SharedRadiusState state;
        if (!zones.TryGetValue(player, out state)) return null;
        return state.radius;
    }

    public void RegisterReached(IPlayer player) {
        SharedRadiusState state;
        if (!zones.TryGetValue(player, out state)) return;
        state.reached = true;
    }

    public void Calculate() {

        var zones = this.zones.ToArray();
        var count = zones.Length;

        int x, z;
        int l;
        if (radius.xSize > radius.zSize) {
            x = (int)Math.Ceiling((double)radius.xSize / (double)count);
            l = radius.xSize;
            z = radius.zSize;
            for (int i = 0; i < zones.Length; i++)
                zones[i].Value.Update(new Location(radius.start.x + x * i, 0, radius.start.z),
                                      new Location(radius.start.x + (x * (i + 1)) + (i == zones.Length - 1 ? l - (x * (i + 1)) : 0), 0, radius.start.z + z));
        }
        else {
            x = radius.xSize;
            z = (int)Math.Ceiling((double)radius.zSize / (double)count);
            l = radius.zSize;
            for (int i = 0; i < zones.Length; i++)
                zones[i].Value.Update(new Location(radius.start.x, 0, radius.start.z + z * i),
                                      new Location(radius.start.x + x, 0, radius.start.z + z * (i + 1) + (i == zones.Length - 1 ? l - (z * (i + 1)) : 0)));
        }
    }

    public bool AllReached() {

        var temp= zones.ToArray();
        for(int i = 0; i < temp.Length; i++)
            if (!temp[i].Value.reached) return false;
        return true;
    }
    public bool MeReached(IPlayer player) {

        SharedRadiusState state;
        if (!zones.TryGetValue(player, out state)) return false;
        return state.reached;
    }
}

public class SharedRadiusState
{
    public bool    reached = false;
    public IRadius radius  = null;

    public SharedRadiusState(IRadius radius) {
        this.radius = radius;
    }

    public void Update(ILocation loc, ILocation loc2) {
        reached = false;
        radius.UpdateHorizontal(loc, loc2);
    }
}

public class MacroSync
{
    private Task macroTask;

    public bool IsMacroRunning() {
        //Check if there is an instance of the task.
        if (macroTask == null) return false;
        //Check completion state.
        return !macroTask.IsCompleted && !macroTask.IsCanceled && !macroTask.IsFaulted;
    }

    public void Run(IPlayer player, string name) {
        macroTask = player.functions.StartMacro(name);
    }
}