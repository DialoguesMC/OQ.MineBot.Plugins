namespace ChatSpyPlugin
{
    partial class ChatForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.Chat_Message = new System.Windows.Forms.TextBox();
            this.Chat_Send = new System.Windows.Forms.Button();
            this.Chat_Box = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // Chat_Message
            // 
            this.Chat_Message.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Chat_Message.Location = new System.Drawing.Point(12, 349);
            this.Chat_Message.MaxLength = 256;
            this.Chat_Message.Name = "Chat_Message";
            this.Chat_Message.Size = new System.Drawing.Size(459, 20);
            this.Chat_Message.TabIndex = 1;
            this.Chat_Message.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Chat_Box_KeyDown);
            // 
            // Chat_Send
            // 
            this.Chat_Send.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Chat_Send.Location = new System.Drawing.Point(477, 348);
            this.Chat_Send.Name = "Chat_Send";
            this.Chat_Send.Size = new System.Drawing.Size(75, 22);
            this.Chat_Send.TabIndex = 2;
            this.Chat_Send.Text = "Send";
            this.Chat_Send.UseVisualStyleBackColor = true;
            this.Chat_Send.Click += new System.EventHandler(this.Chat_Send_Click);
            // 
            // Chat_Box
            // 
            this.Chat_Box.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Chat_Box.Location = new System.Drawing.Point(12, 12);
            this.Chat_Box.Name = "Chat_Box";
            this.Chat_Box.ReadOnly = true;
            this.Chat_Box.Size = new System.Drawing.Size(540, 330);
            this.Chat_Box.TabIndex = 3;
            this.Chat_Box.Text = "";
            // 
            // ChatForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(564, 381);
            this.Controls.Add(this.Chat_Box);
            this.Controls.Add(this.Chat_Send);
            this.Controls.Add(this.Chat_Message);
            this.Name = "ChatForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox Chat_Message;
        private System.Windows.Forms.Button Chat_Send;
        private System.Windows.Forms.RichTextBox Chat_Box;
    }
}