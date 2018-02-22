using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OQ.MineBot.PluginBase;

namespace OreCounterPlugin
{
    public partial class OreCounterForm : Form
    {
        public static Color COLOR_ZERO = Color.DarkGray;
        public static Color COLOR_POSITIVE = Color.Gray;

        private Label[] indexedLabels;
        private Dictionary<string, PlayerStats> playerStats = new Dictionary<string, PlayerStats>();

        private Dictionary<ushort, ushort> idTable = new Dictionary<ushort, ushort>()
        {
            { 264, 0 },
            { 56,  0 },

            { 388, 1 },
            { 129, 1 },

            { 331, 2 },
            { 73,  2 },

            { 351, 3 },
            { 21,  3 },

            { 266, 4 },
            { 14,  4 },

            { 265, 5 },
            { 15,  5 },

            { 263, 6 },
            { 16,  6 },
        };

        private PlayerStats globalStats;
        private PlayerStats selectedStats;

        public OreCounterForm()
        {
            InitializeComponent();
            RegisterLabels();
            globalStats = new PlayerStats("global", indexedLabels.Length);
            selectedStats = globalStats;
        }

        #region Display

        private void RegisterLabels() {
            indexedLabels = new[]
            {
                LabelDiamond,
                LabelEmerald,
                LabelRedstone,
                LabelLapis,
                LabelGold,
                LabelIron,
                LabelCoal
            };
        }

        public void SetValue(int index, int value) {

            Color color = COLOR_POSITIVE;
            if (value <= 0) color = COLOR_ZERO;

            this.Invoke((MethodInvoker) (() => {
                indexedLabels[index].Text = value.ToString();
                indexedLabels[index].ForeColor = color;
            }));
        }

        public int ConvertId(ushort id) {

            if (!idTable.ContainsKey(id)) return -1;
            return idTable[id];
        }

        private void UpdateRender() {
            
            for(int i = 0; i < selectedStats.values.Length; i++)
                SetValue(i, selectedStats.values[i]);
        }

        #endregion

        #region Player

        public void AddPlayer(IPlayer player) {

            if (player == null) return;
            if (playerStats.ContainsKey(player.status.username)) {
                playerStats[player.status.username].Disconnected = false;
                return;
            }

            var stats = new PlayerStats(player.status.username, indexedLabels.Length);
            playerStats.Add(player.status.username, stats);

            this.Invoke((MethodInvoker) (() => {
                AccountList.Items.Add(stats);
            }));
        }

        public void DisconnectedPlayer(IPlayer player) {

            if (player == null || !playerStats.ContainsKey(player.status.username)) return;
            playerStats[player.status.username].Disconnected = true;
        }

        #endregion

        private void AccountList_SelectedIndexChanged(object sender, EventArgs e) {

            var item = AccountList.SelectedItem as PlayerStats;
            if (item == null) {
                item = globalStats;
            }

            selectedStats = item;
            this.UpdateRender();
        }

        public void InventoryIncrease(IPlayer player, ushort id, int countDifference) {

            int converted = ConvertId(id);
            if (converted == -1) return;
            if (!playerStats.ContainsKey(player.status.username)) return;

            playerStats[player.status.username].values[converted] += countDifference;
            globalStats.values[converted] += countDifference;

            // Check if the current player is selected,
            // or global is selected as then we need to
            // update the gui.
            if(selectedStats == globalStats || selectedStats.Name == player.status.username)
                this.UpdateRender();
        }
    }

    public class PlayerStats
    {
        public string Name;
        public bool Disconnected;
        public int[] values;

        public PlayerStats(string name, int count) {
            this.Name = name;
            this.values = new int[count];
        }

        public override string ToString() {
            return Disconnected?"[DC] " + Name : Name;
        }
    }
}
