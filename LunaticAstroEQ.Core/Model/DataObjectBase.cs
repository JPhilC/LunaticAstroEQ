/*
BSD 2-Clause License

Copyright (c) 2019, Philip Crompton, Email: phil@lunaticsoftware.org
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

using GalaSoft.MvvmLight;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Model
{
   [ComVisible(false)]
   public abstract class DataObjectBase : ObservableObject, INotifyDataErrorInfo
   {
      #region Notify data error
      public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
      private Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

      // get errors by property
      public IEnumerable GetErrors(string propertyName)
      {
         if (!string.IsNullOrWhiteSpace(propertyName)
               && this._errors.ContainsKey(propertyName))
         {
            return this._errors[propertyName];
         }
         return null;
      }

      // has errors
      public bool HasErrors
      {
         get { return (this._errors.Count > 0); }
      }

      public void AddError(string propertyName, string error)
      {
         // Add error to list
         this._errors[propertyName] = new List<string>() { error };
         this.NotifyErrorsChanged(propertyName);
      }

      public void RemoveError(string propertyName)
      {
         // remove error
         if (this._errors.ContainsKey(propertyName))
            this._errors.Remove(propertyName);
         this.NotifyErrorsChanged(propertyName);
      }

      public void NotifyErrorsChanged(string propertyName)
      {
         EventHandler<DataErrorsChangedEventArgs> handler = this.ErrorsChanged;
         // Notify
         if (handler != null)
            handler(this, new DataErrorsChangedEventArgs(propertyName));
      }
      #endregion

   }
}
