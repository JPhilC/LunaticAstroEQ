/*
BSD 2-Clause License

Copyright (c) 2019, Philip Crompton
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

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
