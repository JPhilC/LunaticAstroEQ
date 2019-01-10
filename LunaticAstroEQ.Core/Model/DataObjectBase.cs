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
