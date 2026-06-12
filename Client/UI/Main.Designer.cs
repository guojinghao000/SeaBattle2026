namespace Client
{
    partial class Main
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pbBattlefield = new PictureBox();
            gbLogin = new GroupBox();
            btnConnect = new Button();
            txtCrew = new TextBox();
            lblCrew = new Label();
            txtCaptain = new TextBox();
            lblCaptain = new Label();
            txtShipName = new TextBox();
            lblShipName = new Label();
            txtIP = new TextBox();
            lblIP = new Label();
            gbHints = new GroupBox();
            lblHint1 = new Label();
            lblHint2 = new Label();
            lblHint3 = new Label();
            gbScore = new GroupBox();
            lstScore = new ListBox();
            lblStatus = new Label();
            btnDisconnect = new Button();
            btnToggleMode = new Button();
            lblShipStatus = new Label();
            cbAutoBattle = new CheckBox();
            pbMinimap = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pbBattlefield).BeginInit();
            gbLogin.SuspendLayout();
            gbHints.SuspendLayout();
            gbScore.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbMinimap).BeginInit();
            SuspendLayout();
            // 
            // pbBattlefield
            // 
            pbBattlefield.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pbBattlefield.BackColor = Color.FromArgb(16, 40, 72);
            pbBattlefield.Location = new Point(9, 10);
            pbBattlefield.Margin = new Padding(2, 3, 2, 3);
            pbBattlefield.Name = "pbBattlefield";
            pbBattlefield.Size = new Size(467, 528);
            pbBattlefield.TabIndex = 0;
            pbBattlefield.TabStop = false;
            pbBattlefield.Click += pbBattlefield_Click;
            // 
            // gbLogin
            // 
            gbLogin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            gbLogin.Controls.Add(btnConnect);
            gbLogin.Controls.Add(btnBroadcast);
            gbLogin.Controls.Add(txtCrew);
            gbLogin.Controls.Add(lblCrew);
            gbLogin.Controls.Add(txtCaptain);
            gbLogin.Controls.Add(lblCaptain);
            gbLogin.Controls.Add(txtShipName);
            gbLogin.Controls.Add(lblShipName);
            gbLogin.Controls.Add(txtIP);
            gbLogin.Controls.Add(lblIP);
            gbLogin.Location = new Point(485, 10);
            gbLogin.Margin = new Padding(2, 3, 2, 3);
            gbLogin.Name = "gbLogin";
            gbLogin.Padding = new Padding(2, 3, 2, 3);
            gbLogin.Size = new Size(311, 196);
            gbLogin.TabIndex = 1;
            gbLogin.TabStop = false;
            gbLogin.Text = "登录";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(117, 161);
            btnConnect.Margin = new Padding(2, 3, 2, 3);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(78, 26);
            btnConnect.TabIndex = 8;
            btnConnect.Text = "连接";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += BtnConnect_Click;
            // 
            // btnBroadcast
            // 
            btnBroadcast = new Button();
            btnBroadcast.Location = new Point(200, 161);
            btnBroadcast.Margin = new Padding(2, 3, 2, 3);
            btnBroadcast.Name = "btnBroadcast";
            btnBroadcast.Size = new Size(95, 26);
            btnBroadcast.TabIndex = 9;
            btnBroadcast.TabStop = false;
            btnBroadcast.Text = "发送广播";
            btnBroadcast.UseVisualStyleBackColor = true;
            btnBroadcast.Click += BtnBroadcast_Click;
            // 
            // txtCrew
            // 
            txtCrew.Location = new Point(61, 132);
            txtCrew.Margin = new Padding(2, 3, 2, 3);
            txtCrew.Name = "txtCrew";
            txtCrew.Size = new Size(238, 23);
            txtCrew.TabIndex = 7;
            txtCrew.Text = "水手1,水手2";
            // 
            // lblCrew
            // 
            lblCrew.AutoSize = true;
            lblCrew.Location = new Point(6, 134);
            lblCrew.Margin = new Padding(2, 0, 2, 0);
            lblCrew.Name = "lblCrew";
            lblCrew.Size = new Size(56, 17);
            lblCrew.TabIndex = 6;
            lblCrew.Text = "船员列表";
            // 
            // txtCaptain
            // 
            txtCaptain.Location = new Point(61, 102);
            txtCaptain.Margin = new Padding(2, 3, 2, 3);
            txtCaptain.Name = "txtCaptain";
            txtCaptain.Size = new Size(238, 23);
            txtCaptain.TabIndex = 5;
            txtCaptain.Text = "船长";
            // 
            // lblCaptain
            // 
            lblCaptain.AutoSize = true;
            lblCaptain.Location = new Point(6, 105);
            lblCaptain.Margin = new Padding(2, 0, 2, 0);
            lblCaptain.Name = "lblCaptain";
            lblCaptain.Size = new Size(56, 17);
            lblCaptain.TabIndex = 4;
            lblCaptain.Text = "船长名称";
            // 
            // txtShipName
            // 
            txtShipName.Location = new Point(61, 72);
            txtShipName.Margin = new Padding(2, 3, 2, 3);
            txtShipName.Name = "txtShipName";
            txtShipName.Size = new Size(238, 23);
            txtShipName.TabIndex = 3;
            txtShipName.Text = "我的舰队";
            // 
            // lblShipName
            // 
            lblShipName.AutoSize = true;
            lblShipName.Location = new Point(6, 75);
            lblShipName.Margin = new Padding(2, 0, 2, 0);
            lblShipName.Name = "lblShipName";
            lblShipName.Size = new Size(56, 17);
            lblShipName.TabIndex = 2;
            lblShipName.Text = "舰队名称";
            // 
            // txtIP
            // 
            txtIP.Location = new Point(61, 41);
            txtIP.Margin = new Padding(2, 3, 2, 3);
            txtIP.Name = "txtIP";
            txtIP.Size = new Size(238, 23);
            txtIP.TabIndex = 1;
            txtIP.Text = "127.0.0.1";
            // 
            // lblIP
            // 
            lblIP.AutoSize = true;
            lblIP.Location = new Point(6, 43);
            lblIP.Margin = new Padding(2, 0, 2, 0);
            lblIP.Name = "lblIP";
            lblIP.Size = new Size(59, 17);
            lblIP.TabIndex = 0;
            lblIP.Text = "服务器 IP";
            // 
            // gbHints
            // 
            gbHints.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            gbHints.Controls.Add(lblHint1);
            gbHints.Controls.Add(lblHint2);
            gbHints.Controls.Add(lblHint3);
            gbHints.Location = new Point(485, 211);
            gbHints.Margin = new Padding(2, 3, 2, 3);
            gbHints.Name = "gbHints";
            gbHints.Padding = new Padding(2, 3, 2, 3);
            gbHints.Size = new Size(311, 68);
            gbHints.TabIndex = 6;
            gbHints.TabStop = false;
            gbHints.Text = "操作提示";
            // 
            // lblHint1
            // 
            lblHint1.Location = new Point(5, 17);
            lblHint1.Margin = new Padding(2, 0, 2, 0);
            lblHint1.Name = "lblHint1";
            lblHint1.Size = new Size(302, 17);
            lblHint1.TabIndex = 0;
            lblHint1.Text = "移动: W/A/S/D 或 方向键    空格: 回到自己";
            lblHint1.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblHint2
            //
            lblHint2.AutoSize = true;
            lblHint2.Location = new Point(5, 34);
            lblHint2.Margin = new Padding(2, 0, 2, 0);
            lblHint2.Name = "lblHint2";
            lblHint2.Size = new Size(211, 17);
            lblHint2.TabIndex = 1;
            lblHint2.Text = "开火: J    鼠标点击: 选中目标攻击";
            //
            // lblHint3
            //
            lblHint3.AutoSize = true;
            lblHint3.Location = new Point(5, 49);
            lblHint3.Margin = new Padding(2, 0, 2, 0);
            lblHint3.Name = "lblHint3";
            lblHint3.Size = new Size(248, 17);
            lblHint3.TabIndex = 2;
            lblHint3.Text = "拖拽: 移动视角  滚轮: 缩放  F1: 自动战斗";
            // 
            // gbScore
            // 
            gbScore.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            gbScore.Controls.Add(lstScore);
            gbScore.Location = new Point(485, 282);
            gbScore.Margin = new Padding(2, 3, 2, 3);
            gbScore.Name = "gbScore";
            gbScore.Padding = new Padding(2, 3, 2, 3);
            gbScore.Size = new Size(311, 170);
            gbScore.TabIndex = 2;
            gbScore.TabStop = false;
            gbScore.Text = "击沉数排名";
            // 
            // lstScore
            // 
            lstScore.Dock = DockStyle.Fill;
            lstScore.Font = new Font("Consolas", 9F);
            lstScore.FormattingEnabled = true;
            lstScore.ItemHeight = 14;
            lstScore.Location = new Point(2, 19);
            lstScore.Margin = new Padding(2, 3, 2, 3);
            lstScore.Name = "lstScore";
            lstScore.SelectionMode = SelectionMode.None;
            lstScore.Size = new Size(307, 148);
            lstScore.TabIndex = 0;
            lstScore.TabStop = false;
            //
            // lblStatus
            //
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(485, 458);
            lblStatus.Margin = new Padding(2, 0, 2, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(44, 17);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "未连接";
            //
            // lblShipStatus
            //
            lblShipStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblShipStatus.Location = new Point(485, 476);
            lblShipStatus.Margin = new Padding(2, 0, 2, 0);
            lblShipStatus.Name = "lblShipStatus";
            lblShipStatus.Size = new Size(311, 44);
            lblShipStatus.TabIndex = 5;
            lblShipStatus.TextAlign = ContentAlignment.MiddleLeft;
            //
            // btnDisconnect
            //
            btnDisconnect.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(718, 514);
            btnDisconnect.Margin = new Padding(2, 3, 2, 3);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(78, 26);
            btnDisconnect.TabIndex = 4;
            btnDisconnect.TabStop = false;
            btnDisconnect.Text = "断开连接";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += BtnDisconnect_Click;
            //
            // btnToggleMode
            //
            btnToggleMode.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnToggleMode.Location = new Point(638, 514);
            btnToggleMode.Margin = new Padding(2, 3, 2, 3);
            btnToggleMode.Name = "btnToggleMode";
            btnToggleMode.Size = new Size(75, 26);
            btnToggleMode.TabIndex = 11;
            btnToggleMode.TabStop = false;
            btnToggleMode.Text = "完整模式";
            btnToggleMode.UseVisualStyleBackColor = true;
            btnToggleMode.Click += BtnToggleMode_Click;
            //
            // cbAutoBattle
            //
            cbAutoBattle.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cbAutoBattle.AutoSize = true;
            cbAutoBattle.Location = new Point(485, 517);
            cbAutoBattle.Margin = new Padding(2, 3, 2, 3);
            cbAutoBattle.Name = "cbAutoBattle";
            cbAutoBattle.Size = new Size(75, 21);
            cbAutoBattle.TabIndex = 12;
            cbAutoBattle.TabStop = false;
            cbAutoBattle.Text = "自动战斗";
            cbAutoBattle.UseVisualStyleBackColor = true;
            // 
            // pbMinimap
            // 
            pbMinimap.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pbMinimap.BackColor = Color.FromArgb(180, 10, 30, 60);
            pbMinimap.Location = new Point(0, 0);
            pbMinimap.Margin = new Padding(2, 3, 2, 3);
            pbMinimap.Name = "pbMinimap";
            pbMinimap.Size = new Size(117, 128);
            pbMinimap.TabIndex = 10;
            pbMinimap.TabStop = false;
            pbMinimap.MouseClick += PbMinimap_MouseClick;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(824, 560);
            Controls.Add(pbMinimap);
            Controls.Add(lblShipStatus);
            Controls.Add(btnDisconnect);
            Controls.Add(btnToggleMode);
            Controls.Add(cbAutoBattle);
            Controls.Add(lblStatus);
            Controls.Add(gbScore);
            Controls.Add(gbHints);
            Controls.Add(gbLogin);
            Controls.Add(pbBattlefield);
            DoubleBuffered = true;
            KeyPreview = true;
            Margin = new Padding(2, 3, 2, 3);
            MinimumSize = new Size(704, 546);
            Name = "Main";
            Text = "SeaBattle 2026 - 客户端";
            FormClosing += Main_FormClosing;
            KeyDown += Main_KeyDown;
            KeyUp += Main_KeyUp;
            ((System.ComponentModel.ISupportInitialize)pbBattlefield).EndInit();
            gbLogin.ResumeLayout(false);
            gbLogin.PerformLayout();
            gbHints.ResumeLayout(false);
            gbHints.PerformLayout();
            gbScore.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbMinimap).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private PictureBox pbBattlefield;
        private PictureBox pbMinimap;
        private GroupBox gbLogin;
        private Button btnConnect;
        private Button btnBroadcast;
        private TextBox txtCrew;
        private Label lblCrew;
        private TextBox txtCaptain;
        private Label lblCaptain;
        private TextBox txtShipName;
        private Label lblShipName;
        private TextBox txtIP;
        private Label lblIP;
        private GroupBox gbScore;
        private ListBox lstScore;
        private GroupBox gbHints;
        private Label lblHint1;
        private Label lblHint2;
        private Label lblHint3;
        private Label lblStatus;
        private Button btnDisconnect;
        private Button btnToggleMode;
        private Label lblShipStatus;
        private CheckBox cbAutoBattle;
    }
}
