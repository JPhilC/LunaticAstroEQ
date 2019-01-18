using ASCOM.LunaticAstroEQ.Core.Geometry;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ASCOM.LunaticAstroEQ
{
   [ComVisible(false)]              // Form not registered for COM!
   public partial class SetupDialogForm : Form
   {

      private Telescope  _Telescope = null;

      public SetupDialogForm(Telescope telescope)
      {
         _Telescope = telescope;
         InitializeComponent();
         // Initialise current values of user settings from the ASCOM Profile
         InitUI();
      }

      private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
      {
         // Place any validation constraint checks here
         // Update the state variables with results from the dialogue
         _Telescope.Settings.COMPort = (string)comboBoxComPort.SelectedItem;

         _Telescope.TraceState = chkTrace.Checked;    // The property will update the Settings object.

         _Telescope = null;
      }

      private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
      {
         _Telescope = null;   // Remove circular reference
         Close();
      }

      private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
      {
         try
         {
            System.Diagnostics.Process.Start("http://ascom-standards.org/");
         }
         catch (System.ComponentModel.Win32Exception noBrowser)
         {
            if (noBrowser.ErrorCode == -2147467259)
               MessageBox.Show(noBrowser.Message);
         }
         catch (System.Exception other)
         {
            MessageBox.Show(other.Message);
         }
      }

      private void InitUI()
      {
         chkTrace.Checked = _Telescope.TraceState;
         // set the list of com ports to those that are currently available
         comboBoxComPort.Items.Clear();
         comboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());      // use System.IO because it's static
                                                                                         // select the current port if possible
         if (comboBoxComPort.Items.Contains(_Telescope.Settings.COMPort))
         {
            comboBoxComPort.SelectedItem = _Telescope.Settings.COMPort;
         }
      }
   }
}