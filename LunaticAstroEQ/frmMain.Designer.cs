using System;

namespace ASCOM.LunaticAstroEQ
{
   partial class frmMain
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
         this.components = new System.ComponentModel.Container();
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
         this.label1 = new System.Windows.Forms.Label();
         this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
         this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
         this.contextMenuStrip1.SuspendLayout();
         this.SuspendLayout();
         // 
         // label1
         // 
         this.label1.Location = new System.Drawing.Point(12, 10);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(199, 33);
         this.label1.TabIndex = 0;
         this.label1.Text = "This is an ASCOM driver, not a program for you to use.";
         // 
         // notifyIcon1
         // 
         this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
         this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
         this.notifyIcon1.Text = "Lunatic AstroEQ ASCOM Driver";
         this.notifyIcon1.Visible = true;
         // 
         // contextMenuStrip1
         // 
         this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
         this.contextMenuStrip1.Name = "contextMenuStrip1";
         this.contextMenuStrip1.Size = new System.Drawing.Size(181, 48);
         // 
         // toolStripMenuItem1
         // 
         this.toolStripMenuItem1.Name = "toolStripMenuItem1";
         this.toolStripMenuItem1.Size = new System.Drawing.Size(180, 22);
         this.toolStripMenuItem1.Text = "E&xit";
         this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
         // 
         // frmMain
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(233, 52);
         this.Controls.Add(this.label1);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
         this.Name = "frmMain";
         this.Text = "LunaticAstroEQ Driver Server";
         this.contextMenuStrip1.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.NotifyIcon notifyIcon1;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
   }
}

