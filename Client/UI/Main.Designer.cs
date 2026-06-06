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
            gbScore = new GroupBox();
            lstScore = new ListBox();
            lblStatus = new Label();
            btnDisconnect = new Button();
            lblShipStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)pbBattlefield).BeginInit();
            gbLogin.SuspendLayout();
            gbScore.SuspendLayout();
            SuspendLayout();
            // 
            // pbBattlefield
            // 
            pbBattlefield.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pbBattlefield.BackColor = Color.FromArgb(16, 40, 72);
            pbBattlefield.Location = new Point(12, 12);
            pbBattlefield.Name = "pbBattlefield";
            pbBattlefield.Size = new Size(600, 588);
            pbBattlefield.TabIndex = 0;
            pbBattlefield.TabStop = false;
            pbBattlefield.Paint += PbBattlefield_Paint;
            pbBattlefield.MouseDown += PbBattlefield_MouseDown;
            // 
            // gbLogin
            // 
            gbLogin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            gbLogin.Controls.Add(btnConnect);
            gbLogin.Controls.Add(txtCrew);
            gbLogin.Controls.Add(lblCrew);
            gbLogin.Controls.Add(txtCaptain);
            gbLogin.Controls.Add(lblCaptain);
            gbLogin.Controls.Add(txtShipName);
            gbLogin.Controls.Add(lblShipName);
            gbLogin.Controls.Add(txtIP);
            gbLogin.Controls.Add(lblIP);
            gbLogin.Location = new Point(624, 12);
            gbLogin.Name = "gbLogin";
            gbLogin.Size = new Size(280, 230);
            gbLogin.TabIndex = 1;
            gbLogin.TabStop = false;
            gbLogin.Text = "登录";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(89, 190);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 30);
            btnConnect.TabIndex = 8;
            btnConnect.Text = "连接";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += BtnConnect_Click;
            // 
            // txtCrew
            // 
            txtCrew.Location = new Point(79, 155);
            txtCrew.Name = "txtCrew";
            txtCrew.Size = new Size(185, 27);
            txtCrew.TabIndex = 7;
            txtCrew.Text = "水手1,水手2";
            // 
            // lblCrew
            // 
            lblCrew.AutoSize = true;
            lblCrew.Location = new Point(8, 158);
            lblCrew.Name = "lblCrew";
            lblCrew.Size = new Size(63, 20);
            lblCrew.TabIndex = 6;
            lblCrew.Text = "船员列表";
            // 
            // txtCaptain
            // 
            txtCaptain.Location = new Point(79, 120);
            txtCaptain.Name = "txtCaptain";
            txtCaptain.Size = new Size(185, 27);
            txtCaptain.TabIndex = 5;
            txtCaptain.Text = "船长";
            // 
            // lblCaptain
            // 
            lblCaptain.AutoSize = true;
            lblCaptain.Location = new Point(8, 123);
            lblCaptain.Name = "lblCaptain";
            lblCaptain.Size = new Size(63, 20);
            lblCaptain.TabIndex = 4;
            lblCaptain.Text = "船长名称";
            // 
            // txtShipName
            // 
            txtShipName.Location = new Point(79, 85);
            txtShipName.Name = "txtShipName";
            txtShipName.Size = new Size(185, 27);
            txtShipName.TabIndex = 3;
            txtShipName.Text = "我的舰队";
            // 
            // lblShipName
            // 
            lblShipName.AutoSize = true;
            lblShipName.Location = new Point(8, 88);
            lblShipName.Name = "lblShipName";
            lblShipName.Size = new Size(63, 20);
            lblShipName.TabIndex = 2;
            lblShipName.Text = "舰队名称";
            // 
            // txtIP
            // 
            txtIP.Location = new Point(79, 48);
            txtIP.Name = "txtIP";
            txtIP.Size = new Size(185, 27);
            txtIP.TabIndex = 1;
            txtIP.Text = "127.0.0.1";
            // 
            // lblIP
            // 
            lblIP.AutoSize = true;
            lblIP.Location = new Point(8, 51);
            lblIP.Name = "lblIP";
            lblIP.Size = new Size(67, 20);
            lblIP.TabIndex = 0;
            lblIP.Text = "服务器 IP";
            // 
            // gbScore
            // 
            gbScore.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            gbScore.Controls.Add(lstScore);
            gbScore.Location = new Point(624, 300);
            gbScore.Name = "gbScore";
            gbScore.Size = new Size(280, 200);
            gbScore.TabIndex = 2;
            gbScore.TabStop = false;
            gbScore.Text = "击沉数排名";
            // 
            // lstScore
            // 
            lstScore.Dock = DockStyle.Fill;
            lstScore.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lstScore.FormattingEnabled = true;
            lstScore.ItemHeight = 18;
            lstScore.Location = new Point(3, 23);
            lstScore.Name = "lstScore";
            lstScore.Size = new Size(274, 174);
            lstScore.TabIndex = 0;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(624, 503);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(63, 20);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "未连接";
            // 
            // btnDisconnect
            // 
            btnDisconnect.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(802, 570);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(100, 30);
            btnDisconnect.TabIndex = 4;
            btnDisconnect.Text = "断开连接";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += BtnDisconnect_Click;
            // 
            // lblShipStatus
            // 
            lblShipStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblShipStatus.Location = new Point(624, 530);
            lblShipStatus.Name = "lblShipStatus";
            lblShipStatus.Size = new Size(280, 30);
            lblShipStatus.TabIndex = 5;
            lblShipStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(916, 612);
            Controls.Add(lblShipStatus);
            Controls.Add(btnDisconnect);
            Controls.Add(lblStatus);
            Controls.Add(gbScore);
            Controls.Add(gbLogin);
            Controls.Add(pbBattlefield);
            DoubleBuffered = true;
            KeyPreview = true;
            MinimumSize = new Size(800, 600);
            Name = "Main";
            Text = "SeaBattle 2026 - 客户端";
            FormClosing += Main_FormClosing;
            KeyDown += Main_KeyDown;
            KeyUp += Main_KeyUp;
            ((System.ComponentModel.ISupportInitialize)pbBattlefield).EndInit();
            gbLogin.ResumeLayout(false);
            gbLogin.PerformLayout();
            gbScore.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private PictureBox pbBattlefield;
        private GroupBox gbLogin;
        private Button btnConnect;
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
        private Label lblStatus;
        private Button btnDisconnect;
        private Label lblShipStatus;
    }
}
