namespace ASCOM.LunaticAstroEQ
{
   partial class SetupDialogForm
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
         this.cmdOK = new System.Windows.Forms.Button();
         this.cmdCancel = new System.Windows.Forms.Button();
         this.label1 = new System.Windows.Forms.Label();
         this.picASCOM = new System.Windows.Forms.PictureBox();
         this.label2 = new System.Windows.Forms.Label();
         this.chkTrace = new System.Windows.Forms.CheckBox();
         this.comboBoxComPort = new System.Windows.Forms.ComboBox();
         this.label3 = new System.Windows.Forms.Label();
         this.label4 = new System.Windows.Forms.Label();
         this.startAltitudeTextBox = new System.Windows.Forms.TextBox();
         this.startAzimuthTextBox = new System.Windows.Forms.TextBox();
         ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
         this.SuspendLayout();
         // 
         // cmdOK
         // 
         this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.cmdOK.Location = new System.Drawing.Point(522, 220);
         this.cmdOK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
         this.cmdOK.Name = "cmdOK";
         this.cmdOK.Size = new System.Drawing.Size(88, 37);
         this.cmdOK.TabIndex = 0;
         this.cmdOK.Text = "OK";
         this.cmdOK.UseVisualStyleBackColor = true;
         this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
         // 
         // cmdCancel
         // 
         this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.cmdCancel.Location = new System.Drawing.Point(522, 266);
         this.cmdCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
         this.cmdCancel.Name = "cmdCancel";
         this.cmdCancel.Size = new System.Drawing.Size(88, 38);
         this.cmdCancel.TabIndex = 1;
         this.cmdCancel.Text = "Cancel";
         this.cmdCancel.UseVisualStyleBackColor = true;
         this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
         // 
         // label1
         // 
         this.label1.Location = new System.Drawing.Point(18, 14);
         this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(184, 48);
         this.label1.TabIndex = 2;
         this.label1.Text = "Construct your driver\'s setup dialog here.";
         // 
         // picASCOM
         // 
         this.picASCOM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
         this.picASCOM.Image = global::ASCOM.LunaticAstroEQ.Properties.Resources.ASCOM;
         this.picASCOM.Location = new System.Drawing.Point(538, 14);
         this.picASCOM.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
         this.picASCOM.Name = "picASCOM";
         this.picASCOM.Size = new System.Drawing.Size(48, 56);
         this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
         this.picASCOM.TabIndex = 3;
         this.picASCOM.TabStop = false;
         this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
         this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
         // 
         // label2
         // 
         this.label2.AutoSize = true;
         this.label2.Location = new System.Drawing.Point(18, 138);
         this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(88, 20);
         this.label2.TabIndex = 5;
         this.label2.Text = "Comm Port";
         // 
         // chkTrace
         // 
         this.chkTrace.AutoSize = true;
         this.chkTrace.Location = new System.Drawing.Point(163, 237);
         this.chkTrace.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
         this.chkTrace.Name = "chkTrace";
         this.chkTrace.Size = new System.Drawing.Size(97, 24);
         this.chkTrace.TabIndex = 5;
         this.chkTrace.Text = "Trace on";
         this.chkTrace.UseVisualStyleBackColor = true;
         // 
         // comboBoxComPort
         // 
         this.comboBoxComPort.FormattingEnabled = true;
         this.comboBoxComPort.Location = new System.Drawing.Point(163, 135);
         this.comboBoxComPort.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
         this.comboBoxComPort.Name = "comboBoxComPort";
         this.comboBoxComPort.Size = new System.Drawing.Size(133, 28);
         this.comboBoxComPort.TabIndex = 2;
         // 
         // label3
         // 
         this.label3.AutoSize = true;
         this.label3.Location = new System.Drawing.Point(18, 206);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(106, 20);
         this.label3.TabIndex = 8;
         this.label3.Text = "Start Azimuth";
         // 
         // label4
         // 
         this.label4.AutoSize = true;
         this.label4.Location = new System.Drawing.Point(18, 174);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(102, 20);
         this.label4.TabIndex = 9;
         this.label4.Text = "Start Altitude";
         // 
         // startAltitudeTextBox
         // 
         this.startAltitudeTextBox.Location = new System.Drawing.Point(163, 171);
         this.startAltitudeTextBox.Name = "startAltitudeTextBox";
         this.startAltitudeTextBox.Size = new System.Drawing.Size(100, 26);
         this.startAltitudeTextBox.TabIndex = 3;
         // 
         // startAzimuthTextBox
         // 
         this.startAzimuthTextBox.Location = new System.Drawing.Point(163, 203);
         this.startAzimuthTextBox.Name = "startAzimuthTextBox";
         this.startAzimuthTextBox.Size = new System.Drawing.Size(100, 26);
         this.startAzimuthTextBox.TabIndex = 4;
         // 
         // SetupDialogForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(625, 317);
         this.Controls.Add(this.startAzimuthTextBox);
         this.Controls.Add(this.startAltitudeTextBox);
         this.Controls.Add(this.label4);
         this.Controls.Add(this.label3);
         this.Controls.Add(this.comboBoxComPort);
         this.Controls.Add(this.chkTrace);
         this.Controls.Add(this.label2);
         this.Controls.Add(this.picASCOM);
         this.Controls.Add(this.label1);
         this.Controls.Add(this.cmdCancel);
         this.Controls.Add(this.cmdOK);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "SetupDialogForm";
         this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
         this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
         this.Text = "LunaticAstroEQ Setup";
         ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Button cmdOK;
      private System.Windows.Forms.Button cmdCancel;
      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.PictureBox picASCOM;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.CheckBox chkTrace;
      private System.Windows.Forms.ComboBox comboBoxComPort;
      private System.Windows.Forms.Label label3;
      private System.Windows.Forms.Label label4;
      private System.Windows.Forms.TextBox startAltitudeTextBox;
      private System.Windows.Forms.TextBox startAzimuthTextBox;
   }
}