namespace VirtualDisk
{
    partial class BaseForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseForm));
            menu = new ContextMenuStrip(components);
            menuUpdate = new ToolStripMenuItem();
            menuExit = new ToolStripMenuItem();
            icon = new NotifyIcon(components);
            saveBtn = new Button();
            cookieEdit = new TextBox();
            label1 = new Label();
            syncBtn = new Button();
            toolStripMenuItem1 = new ToolStripSeparator();
            menu.SuspendLayout();
            SuspendLayout();
            // 
            // menu
            // 
            menu.Items.AddRange(new ToolStripItem[] { menuUpdate, toolStripMenuItem1, menuExit });
            menu.Name = "menu";
            menu.Size = new Size(181, 76);
            menu.ItemClicked += Menu_Clicked;
            // 
            // menuUpdate
            // 
            menuUpdate.Name = "menuUpdate";
            menuUpdate.Size = new Size(180, 22);
            menuUpdate.Text = "更新";
            // 
            // menuExit
            // 
            menuExit.Name = "menuExit";
            menuExit.Size = new Size(180, 22);
            menuExit.Text = "退出";
            // 
            // icon
            // 
            icon.Icon = (Icon)resources.GetObject("icon.Icon");
            icon.Text = "CloudDisk";
            icon.Visible = true;
            icon.MouseClick += Icon_Clicked;
            icon.MouseDoubleClick += Icon_DoubleClick;
            // 
            // saveBtn
            // 
            saveBtn.Location = new Point(157, 167);
            saveBtn.Name = "saveBtn";
            saveBtn.Size = new Size(118, 37);
            saveBtn.TabIndex = 2;
            saveBtn.Text = "保存";
            saveBtn.UseVisualStyleBackColor = true;
            saveBtn.Click += Button_Save;
            // 
            // cookieEdit
            // 
            cookieEdit.Location = new Point(12, 29);
            cookieEdit.Multiline = true;
            cookieEdit.Name = "cookieEdit";
            cookieEdit.Size = new Size(263, 130);
            cookieEdit.TabIndex = 3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(49, 17);
            label1.TabIndex = 4;
            label1.Text = "Cookie";
            // 
            // syncBtn
            // 
            syncBtn.Location = new Point(12, 167);
            syncBtn.Name = "syncBtn";
            syncBtn.Size = new Size(118, 37);
            syncBtn.TabIndex = 5;
            syncBtn.Text = "更新";
            syncBtn.UseVisualStyleBackColor = true;
            syncBtn.Click += Button_Sync;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(177, 6);
            // 
            // BaseForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(287, 216);
            Controls.Add(syncBtn);
            Controls.Add(label1);
            Controls.Add(cookieEdit);
            Controls.Add(saveBtn);
            Name = "BaseForm";
            Text = "CloudDisk";
            FormClosing += BaseForm_Closing;
            Load += BaseForm_Load;
            menu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ContextMenuStrip menu;
        private NotifyIcon icon;
        private ToolStripMenuItem menuExit;
        private Button saveBtn;
        private TextBox cookieEdit;
        private Label label1;
        private Button syncBtn;
        private ToolStripMenuItem menuUpdate;
        private ToolStripSeparator toolStripMenuItem1;
    }
}