using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Runtime.InteropServices;

namespace ASCOM.LunaticAstroEQ.Core
{
   [ComVisible(false)]
   public abstract class LunaticViewModelBase : ViewModelBase
   {

      #region Actions ....
      public Action SaveAndCloseAction { get; set; }
      public Action CancelAndCloseAction { get; set; }
      #endregion

      #region Relay commands ....
      private RelayCommand _CancelChangesAndCloseCommand;

      /// <summary>
      /// Gets the SaveChangesAndCloseCommand.
      /// </summary>
      public RelayCommand CancelChangesAndCloseCommand
      {
         get
         {
            return _CancelChangesAndCloseCommand
                ?? (_CancelChangesAndCloseCommand = new RelayCommand(
                                      () => {
                                         if (OnCancelCommand())
                                         {
                                            if (this.CancelAndCloseAction != null)
                                            {
                                               CancelAndCloseAction();
                                            }
                                         }
                                      }));
         }
      }

      /// <summary>
      /// Override this method to perform cancel code.
      /// Note: The data object is restored in the base class code.
      /// </summary>
      /// <returns>True if you want the cacncel command to close the window.</returns>
      protected virtual bool OnCancelCommand()
      {
         return true;
      }

      private RelayCommand _SaveChangesAndCloseCommand;

      /// <summary>
      /// Gets the SaveChangesAndCloseCommand.
      /// </summary>
      public RelayCommand SaveChangesAndCloseCommand
      {
         get
         {
            return _SaveChangesAndCloseCommand
                ?? (_SaveChangesAndCloseCommand = new RelayCommand(
                                      () => {
                                         if (OnSaveCommand())
                                         {
                                            // Don't need to do anything here as the assumption is that the properties
                                            // and bound and therefore already saved.
                                            if (this.SaveAndCloseAction != null)
                                            {
                                               SaveAndCloseAction();
                                            }
                                         }
                                      }));
         }
      }

      /// <summary>
      /// Override this method to perform save code.
      /// </summary>
      /// <returns>True if you want the save command to close the window.</returns>
      protected virtual bool OnSaveCommand()
      {
         return true;
      }

      #endregion


   }
}
