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

      private void label1_Click(object sender, EventArgs e)
      {
         Type driverType = Type.GetTypeFromProgID("ASCOM.LunaticAstroEQ.Telescope");
         ITelescopeV3 driver = (ITelescopeV3)Activator.CreateInstance(driverType);
         //ITelescopeV3 driver = new Telescope() as ITelescopeV3;
         driver.SetupDialog();

      }


   }
}