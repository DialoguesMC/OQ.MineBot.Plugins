namespace ContainerViewerPlugin
{
    partial class ContainerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SelectedContainer = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SlotAmount = new System.Windows.Forms.Label();
            this.SlotID = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.DropSlot = new System.Windows.Forms.Button();
            this.ClickSlot = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.CloseWindow = new System.Windows.Forms.Button();
            this.HotbarGroup = new System.Windows.Forms.GroupBox();
            this.HotbarRightclick = new System.Windows.Forms.Button();
            this.HotbarSelect = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.HotbarGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // SelectedContainer
            // 
            this.SelectedContainer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SelectedContainer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SelectedContainer.FormattingEnabled = true;
            this.SelectedContainer.Location = new System.Drawing.Point(0, 0);
            this.SelectedContainer.Name = "SelectedContainer";
            this.SelectedContainer.Size = new System.Drawing.Size(884, 21);
            this.SelectedContainer.TabIndex = 0;
            this.SelectedContainer.SelectedIndexChanged += new System.EventHandler(this.SelectedContainerChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.SlotAmount);
            this.groupBox1.Controls.Add(this.SlotID);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.DropSlot);
            this.groupBox1.Controls.Add(this.ClickSlot);
            this.groupBox1.Location = new System.Drawing.Point(701, 27);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(171, 76);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Slot";
            // 
            // SlotAmount
            // 
            this.SlotAmount.AutoSize = true;
            this.SlotAmount.Location = new System.Drawing.Point(60, 29);
            this.SlotAmount.Name = "SlotAmount";
            this.SlotAmount.Size = new System.Drawing.Size(13, 13);
            this.SlotAmount.TabIndex = 5;
            this.SlotAmount.Text = "0";
            // 
            // SlotID
            // 
            this.SlotID.AutoSize = true;
            this.SlotID.Location = new System.Drawing.Point(60, 16);
            this.SlotID.Name = "SlotID";
            this.SlotID.Size = new System.Drawing.Size(13, 13);
            this.SlotID.TabIndex = 4;
            this.SlotID.Text = "0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Amount:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "ID:";
            // 
            // DropSlot
            // 
            this.DropSlot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.DropSlot.Location = new System.Drawing.Point(90, 47);
            this.DropSlot.Name = "DropSlot";
            this.DropSlot.Size = new System.Drawing.Size(75, 23);
            this.DropSlot.TabIndex = 1;
            this.DropSlot.Text = "Drop";
            this.DropSlot.UseVisualStyleBackColor = true;
            this.DropSlot.Click += new System.EventHandler(this.DropSlot_Click);
            // 
            // ClickSlot
            // 
            this.ClickSlot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ClickSlot.Location = new System.Drawing.Point(9, 47);
            this.ClickSlot.Name = "ClickSlot";
            this.ClickSlot.Size = new System.Drawing.Size(75, 23);
            this.ClickSlot.TabIndex = 0;
            this.ClickSlot.Text = "Click";
            this.ClickSlot.UseVisualStyleBackColor = true;
            this.ClickSlot.Click += new System.EventHandler(this.ClickSlot_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.CloseWindow);
            this.groupBox2.Location = new System.Drawing.Point(701, 109);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(171, 52);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Window";
            // 
            // CloseWindow
            // 
            this.CloseWindow.Location = new System.Drawing.Point(6, 19);
            this.CloseWindow.Name = "CloseWindow";
            this.CloseWindow.Size = new System.Drawing.Size(75, 23);
            this.CloseWindow.TabIndex = 0;
            this.CloseWindow.Text = "Close";
            this.CloseWindow.UseVisualStyleBackColor = true;
            this.CloseWindow.Click += new System.EventHandler(this.CloseWindow_Click);
            // 
            // HotbarGroup
            // 
            this.HotbarGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HotbarGroup.Controls.Add(this.HotbarRightclick);
            this.HotbarGroup.Controls.Add(this.HotbarSelect);
            this.HotbarGroup.Location = new System.Drawing.Point(701, 167);
            this.HotbarGroup.Name = "HotbarGroup";
            this.HotbarGroup.Size = new System.Drawing.Size(171, 52);
            this.HotbarGroup.TabIndex = 3;
            this.HotbarGroup.TabStop = false;
            this.HotbarGroup.Text = "Hotbar";
            this.HotbarGroup.Visible = false;
            // 
            // HotbarRightclick
            // 
            this.HotbarRightclick.Location = new System.Drawing.Point(90, 19);
            this.HotbarRightclick.Name = "HotbarRightclick";
            this.HotbarRightclick.Size = new System.Drawing.Size(75, 23);
            this.HotbarRightclick.TabIndex = 1;
            this.HotbarRightclick.Text = "Right click";
            this.HotbarRightclick.UseVisualStyleBackColor = true;
            this.HotbarRightclick.Click += new System.EventHandler(this.HotbarRightclick_Click);
            // 
            // HotbarSelect
            // 
            this.HotbarSelect.Location = new System.Drawing.Point(6, 19);
            this.HotbarSelect.Name = "HotbarSelect";
            this.HotbarSelect.Size = new System.Drawing.Size(75, 23);
            this.HotbarSelect.TabIndex = 0;
            this.HotbarSelect.Text = "Select";
            this.HotbarSelect.UseVisualStyleBackColor = true;
            this.HotbarSelect.Click += new System.EventHandler(this.HotbarSelect_Click);
            // 
            // ContainerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 861);
            this.Controls.Add(this.HotbarGroup);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.SelectedContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ContainerForm";
            this.ShowIcon = false;
            this.Text = "Bots containers";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.HotbarGroup.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox SelectedContainer;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button DropSlot;
        private System.Windows.Forms.Button ClickSlot;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button CloseWindow;
        private System.Windows.Forms.Label SlotAmount;
        private System.Windows.Forms.Label SlotID;
        private System.Windows.Forms.GroupBox HotbarGroup;
        private System.Windows.Forms.Button HotbarRightclick;
        private System.Windows.Forms.Button HotbarSelect;
    }
}

