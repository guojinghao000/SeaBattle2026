namespace Server2026
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            treeView1 = new TreeView();
            groupBox2 = new GroupBox();
            listBox1 = new ListBox();
            groupBox3 = new GroupBox();
            pictureBox1 = new PictureBox();
            menuStrip1 = new MenuStrip();
            系统ToolStripMenuItem = new ToolStripMenuItem();
            StartToolStripMenuItem = new ToolStripMenuItem();
            StopToolStripMenuItem = new ToolStripMenuItem();
            IPToolStripMenuItem = new ToolStripMenuItem();
            SaveToolStripMenuItem = new ToolStripMenuItem();
            QuitToolStripMenuItem = new ToolStripMenuItem();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBox1.Controls.Add(treeView1);
            groupBox1.Location = new Point(12, 28);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(242, 521);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "舰队信息";
            // 
            // treeView1
            // 
            treeView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            treeView1.Location = new Point(6, 22);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(230, 493);
            treeView1.TabIndex = 0;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBox2.Controls.Add(listBox1);
            groupBox2.Location = new Point(260, 28);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(242, 521);
            groupBox2.TabIndex = 0;
            groupBox2.TabStop = false;
            groupBox2.Text = "系统信息";
            // 
            // listBox1
            // 
            listBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            listBox1.FormattingEnabled = true;
            listBox1.HorizontalScrollbar = true;
            listBox1.ItemHeight = 17;
            listBox1.Location = new Point(6, 22);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(230, 497);
            listBox1.TabIndex = 0;
            // 
            // groupBox3
            // 
            groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox3.Controls.Add(pictureBox1);
            groupBox3.Location = new Point(508, 28);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(664, 521);
            groupBox3.TabIndex = 0;
            groupBox3.TabStop = false;
            groupBox3.Text = "航海图";
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox1.BackColor = Color.DeepSkyBlue;
            pictureBox1.Location = new Point(6, 22);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(652, 493);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { 系统ToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1184, 25);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // 系统ToolStripMenuItem
            // 
            系统ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { StartToolStripMenuItem, StopToolStripMenuItem, IPToolStripMenuItem, SaveToolStripMenuItem, QuitToolStripMenuItem });
            系统ToolStripMenuItem.Name = "系统ToolStripMenuItem";
            系统ToolStripMenuItem.Size = new Size(44, 21);
            系统ToolStripMenuItem.Text = "系统";
            // 
            // StartToolStripMenuItem
            // 
            StartToolStripMenuItem.Name = "StartToolStripMenuItem";
            StartToolStripMenuItem.Size = new Size(100, 22);
            StartToolStripMenuItem.Text = "启动";
            StartToolStripMenuItem.Click += StartToolStripMenuItem_Click;
            // 
            // StopToolStripMenuItem
            // 
            StopToolStripMenuItem.Enabled = false;
            StopToolStripMenuItem.Name = "StopToolStripMenuItem";
            StopToolStripMenuItem.Size = new Size(100, 22);
            StopToolStripMenuItem.Text = "停止";
            StopToolStripMenuItem.Click += StopToolStripMenuItem_Click;
            // 
            // IPToolStripMenuItem
            // 
            IPToolStripMenuItem.Name = "IPToolStripMenuItem";
            IPToolStripMenuItem.Size = new Size(100, 22);
            IPToolStripMenuItem.Text = "地址";
            IPToolStripMenuItem.Click += IPToolStripMenuItem_Click;
            // 
            // SaveToolStripMenuItem
            // 
            SaveToolStripMenuItem.Name = "SaveToolStripMenuItem";
            SaveToolStripMenuItem.Size = new Size(100, 22);
            SaveToolStripMenuItem.Text = "保存";
            SaveToolStripMenuItem.Click += SaveToolStripMenuItem_Click;
            // 
            // QuitToolStripMenuItem
            // 
            QuitToolStripMenuItem.Name = "QuitToolStripMenuItem";
            QuitToolStripMenuItem.Size = new Size(100, 22);
            QuitToolStripMenuItem.Text = "退出";
            QuitToolStripMenuItem.Click += QuitToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 561);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "大海战 服务端 0.0.1";
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private TreeView treeView1;
        private GroupBox groupBox2;
        private ListBox listBox1;
        private GroupBox groupBox3;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem 系统ToolStripMenuItem;
        private ToolStripMenuItem StartToolStripMenuItem;
        private ToolStripMenuItem StopToolStripMenuItem;
        private ToolStripMenuItem IPToolStripMenuItem;
        private ToolStripMenuItem SaveToolStripMenuItem;
        private PictureBox pictureBox1;
        private ToolStripMenuItem QuitToolStripMenuItem;
    }
}
