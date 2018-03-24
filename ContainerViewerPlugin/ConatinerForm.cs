using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ContainerViewerPlugin;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Window;
using OQ.MineBot.PluginBase.Classes.Window.Containers;

namespace ContainerViewerPlugin
{
    public partial class ContainerForm : Form
    {
        private const int Y_OFFSET = 21;
        private const int SIZE = 48;

        private static Image IMAGE_BACKGROUND;
        private static Image IMAGE_BACKGROUNDSELECTED;

        private static Dictionary<string, Image> IMAGE_ITEMS = new Dictionary<string, Image>();

        private IPlayer      player;
        private IWindow      selectedWinow;
        private RenderedSlot selectedSlot;

        public static void LoadImages() {
            IMAGE_BACKGROUND = new Bitmap(Properties.Resources.Slot);
            IMAGE_BACKGROUNDSELECTED = new Bitmap(Properties.Resources.SlotSelected);

            var ITEMS_IMAGE_DATA = new Bitmap(Properties.Resources.items_28);
            var data = Properties.Resources.ImageData.Split('\n');
            for (int i = 0; i < data.Length; i++) {
                var words = data[i].Split(' ');

                // Get id:
                int id = -1;
                int subid = -1;

                if (words[0].Contains(':')) {
                    id = int.Parse(words[0].Split(':')[0]);
                    subid = int.Parse(words[0].Split(':')[1]);
                }
                else id = int.Parse(words[0]);

                // Get image offset:
                var x = int.Parse(words[8].Replace("px", ""));
                var y = int.Parse(words[9].Replace("px", ""));

                Image imgsrc = ITEMS_IMAGE_DATA;
                Image imgdst = new Bitmap(32, 32);
                using (Graphics gr = Graphics.FromImage(imgdst)) {
                    gr.DrawImage(imgsrc,
                        new RectangleF(0, 0, imgdst.Width, imgdst.Height),
                        new RectangleF(Math.Abs(x), Math.Abs(y), 32, 32),
                        GraphicsUnit.Pixel);
                }

                IMAGE_ITEMS.Add(id + (subid > 0 ? ":" + subid : ""), imgdst);
            }
        }

        public ContainerForm(IPlayer player) {
            InitializeComponent();
            this.player = player;
            this.Text = player.status.username + "'s opened containers";

            // Register dc event.
            player.events.onDisconnected += (player1, reason) => { InvokeIfRequired(this, this.Close); };

            // Register add/remove window event.
            player.status.containers.onWindowAddedEvent += window => {
                if (window.windowType == "minecraft:container" || window.windowTitle == "minecraft:chest") {
                    InvokeIfRequired(this, () => {
                        this.SelectedContainer.Items.Add(window);
                    });
                }
            };
            player.status.containers.onWindowRemovedEvent += id => {
                if (id == 0) return;
                InvokeIfRequired(this, () => {
                    for (int i = 0; i < this.SelectedContainer.Items.Count; i++)
                        if (((IWindow) this.SelectedContainer.Items[i]).id == id) {
                            this.SelectedContainer.Items.RemoveAt(i);
                            break;
                        }

                    // Check if we were looking at this conatiner.
                    if (id == selectedWinow.id) Select(player.status.containers.inventory);
                });
            };

            // Select inventory by default.
            this.SelectedContainer.Items.Add(player.status.containers.inventory);
            Select(player.status.containers.inventory);
            player.status.containers.inventory.onSlotChanged += SelectedWinowOnOnSlotChanged;
        }

        public void Select(IWindow window) {
            container?.Dispose();
            inventory?.Dispose();
            hotbar?.Dispose();
            yoff = 0;
            HotbarGroup.Visible = false;

            if (this.selectedWinow!=null && this.selectedWinow != player.status.containers.inventory) this.selectedWinow.onSlotChanged -= SelectedWinowOnOnSlotChanged;
            this.selectedWinow = window;
            if(this.selectedWinow != player.status.containers.inventory) this.selectedWinow.onSlotChanged += SelectedWinowOnOnSlotChanged;

            Draw();
        }

        private void SelectedWinowOnOnSlotChanged(ISlot slot) {
            DrawGroups();
        }

        private void Draw() {
            if (selectedWinow is IInventory) DrawInventory();
            else {
                DrawContainer();
                DrawInventory();
            }

            DrawGroups();
        }

        private SlotGroup container;
        private int yoff = 0;
        private void DrawContainer() {

            // Dispose of old information.
            container?.Dispose();
            container = new SlotGroup(new RenderedSlot[selectedWinow.slotCount], SlotGroupType.chest, selectedWinow);
            // Draw grid.
            for (int y = 0; y < selectedWinow.slotCount/9; y++)
                for (int x = 0; x < 9; x++) {
                    yoff = Y_OFFSET + y*SIZE;
                    container.Add(CreateSlot(x*SIZE, yoff, (byte) (y*9 + x)));
                }
        }

        private SlotGroup inventory;
        private SlotGroup hotbar;
        private void DrawInventory() {

            inventory?.Dispose();
            hotbar?.Dispose();
            inventory = new SlotGroup(new RenderedSlot[3*9], SlotGroupType.inventory, player.status.containers.inventory);
            hotbar = new SlotGroup(new RenderedSlot[9], SlotGroupType.hotbar, player.status.containers.inventory);

            // Draw inventory grid.
            if (yoff != 0) yoff += 20 + SIZE;
            else yoff = Y_OFFSET;

            for(int y = 0; y < inventory.slots.Length/9; y++)
                for (int x = 0; x < 9; x++) {
                    inventory.Add(CreateSlot(x * SIZE, yoff + y*SIZE, (byte)(y * 9 + x + 9)));
                }
            yoff += (inventory.slots.Length/9-1)*SIZE + 20;
            // Draw hotbar grid.
            for (int x = 0; x < 9; x++) {
                hotbar.Add(CreateSlot(x * SIZE, yoff + SIZE, (byte)(9*3+9 + x)));
            }
        }

        private void DrawGroups() {
            if(container?.slots != null) DrawGroup(container);
            if (inventory?.slots != null) DrawGroup(inventory);
            if (hotbar?.slots != null) DrawGroup(hotbar);
        }
        private void DrawGroup(SlotGroup group) {
            for (int i = 0; i < group.slots.Length; i++) {
                var slot = group.window.GetAt(group.slots[i].index);
                if (slot != null && slot.id != 0 && slot.id != -1) {
                    group.slots[i].picture.Image = GetImage(slot.id, slot.damage);
                    RegisterClickEvent(group, group.slots[i]);
                }
            }
        }

        private void RegisterClickEvent(SlotGroup group, RenderedSlot slot) {
            if (slot.picture == null) return;
            slot.picture.Click += (sender, args) => {
                // Deselect old slot.
                if(selectedSlot != null) {
                    selectedSlot.background.Image = IMAGE_BACKGROUND;
                    selectedSlot.background.SendToBack();
                    selectedSlot.picture.Redraw();
                }

                // Select new slot.
                selectedSlot = slot;
                slot.background.Image = IMAGE_BACKGROUNDSELECTED;
                slot.background.SendToBack();

                var data = group.window.GetAt(slot.index);
                HotbarGroup.Visible = false;
                if (data != null) { 
                    SlotID.Text = data.id.ToString();
                    SlotAmount.Text = data.count.ToString();
                    if (selectedWinow is IInventory && slot.hotbar) {
                        HotbarGroup.Visible = true;
                    }
                }
            };
        }

        private Image GetImage(int id, int subid) {
            if (IMAGE_ITEMS.ContainsKey(id + ":" + subid)) return IMAGE_ITEMS[id + ":" + subid];
            else if (IMAGE_ITEMS.ContainsKey(id.ToString())) return IMAGE_ITEMS[id.ToString()];
            else return IMAGE_ITEMS["1"];
        }
        private RenderedSlot CreateSlot(int x, int y, byte index) {

            var slot = new RenderedSlot()
            {
                index = index,
                background = new PictureBox() {
                    Size = new Size(SIZE, SIZE),
                    Image = IMAGE_BACKGROUND,
                    Location = new Point(x, y),
                    SizeMode = PictureBoxSizeMode.StretchImage
                },
                picture = new TransparentControl() {
                    Size = new Size(SIZE, SIZE),
                    Location = new Point(x, y),
                }
            };

            this.Controls.Add(slot.picture);
            this.Controls.Add(slot.background);

            return slot;
        }

        private void SelectedContainerChanged(object sender, EventArgs e) {
            this.Select((IWindow) this.SelectedContainer.SelectedItem);
        }

        private static void InvokeIfRequired(ISynchronizeInvoke obj, MethodInvoker action) {
            if (obj.InvokeRequired) {
                var args = new object[0];
                obj.Invoke(action, args);
            }
            else {
                action();
            }
        }

        private void CloseWindow_Click(object sender, EventArgs e)
        {
            if(selectedWinow != null) player.functions.CloseContainer(selectedWinow.id);
            Select(player.status.containers.inventory);
        }

        private void DropSlot_Click(object sender, EventArgs e) {
            if (selectedSlot != null) selectedWinow.DropItemStackAsync(selectedSlot.index, b => { });
        }

        private void ClickSlot_Click(object sender, EventArgs e) {
            if(selectedSlot != null) player.functions.ClickContainerSlot(selectedSlot.index, GameWindowButton.Left);
        }

        private void HotbarSelect_Click(object sender, EventArgs e) {
            player.functions.SetHotbarSlot((byte)(selectedSlot.index - 36));
        }

        private void HotbarRightclick_Click(object sender, EventArgs e) {
            player.functions.SetHotbarSlot((byte)(selectedSlot.index - 36));
            player.tickManager.Register(1, () => {
                player.functions.UseSelectedItem();
            });
        }
    }
}

public class TransparentControl : Control
    {
        private readonly Timer refresher;
        private Image _image;

        public TransparentControl()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20;
                return cp;
            }
        }

        protected override void OnMove(EventArgs e)
        {
            RecreateHandle();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            if (_image != null)
            {
                e.Graphics.DrawImage(_image, (Width / 2) - (_image.Width / 2), (Height / 2) - (_image.Height / 2));
            }
        }

        public void Redraw()
        {
            RecreateHandle();
        }

    protected override void OnPaintBackground(PaintEventArgs e)
        {
            //Do not paint background
        }

        public Image Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
        }
    }

public class SlotGroup
{
    public IWindow window;
    public RenderedSlot[] slots;
    public SlotGroupType  type;

    public SlotGroup(RenderedSlot[] slots, SlotGroupType type, IWindow window) {
        this.window = window;
        this.slots = slots;
        this.type  = type;
    }

    private int index;
    public void Add(RenderedSlot slot) {
        this.slots[index++] = slot;
        if (type == SlotGroupType.inventory) slot.inventory = true;
        else if (type == SlotGroupType.hotbar) slot.hotbar = true;
    }

    public void Dispose() {
        if (this.slots == null) return;
        index = 0;
        for (int i = 0; i < slots.Length; i++) {
            slots[i].picture.Dispose();
            slots[i].background.Dispose();
        }
        this.slots = null;
    }
}

public class RenderedSlot
{
    public byte index;

    public TransparentControl picture;
    public PictureBox         background;

    public bool inventory;
    public bool hotbar;
}

public enum SlotGroupType
{
    chest,
    inventory,
    hotbar
}