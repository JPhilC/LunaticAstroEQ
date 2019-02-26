using ASCOM.DeviceInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ASCOM.LunaticAstroEQ
{
   public partial class frmMain : Form
   {
      delegate void SetTextCallback(string text);

      public frmMain()
      {
         InitializeComponent();
      }

      private void toolStripMenuItem1_Click(object sender, EventArgs e)
      {
         if (MessageBox.Show("Are you sure you want to exit the ASCOM driver. Doing so may cause any connected client software to behave in unexpected ways. You should only this option if you have closed all client applications and driver icon remains in the system tray.", "Lunatic AstroEQ ASCOM Driver - Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK)
         {
            this.Close();
         }
      }
   }
}