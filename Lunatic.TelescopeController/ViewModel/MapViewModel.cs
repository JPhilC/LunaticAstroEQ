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

using ASCOM.LunaticAstroEQ.Core;
using Microsoft.Maps.MapControl.WPF;

namespace Lunatic.TelescopeController.ViewModel
{
   /// <summary>
   /// This class contains properties that the main View can data bind to.
   /// <para>
   /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
   /// </para>
   /// <para>
   /// You can also use Blend to data bind with the tool's support.
   /// </para>
   /// <para>
   /// See http://www.galasoft.ch/mvvm
   /// </para>
   /// </summary>
   public class MapViewModel : LunaticViewModelBase
   {


      #region Properties ....

      #region Site information ...
      private Site _OriginalSite;
      private Site _Site;
      public Site Site
      {
         get
         {
            return _Site;
         }
      }
      #endregion

      #endregion

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public MapViewModel(Site site)
      {
         _OriginalSite = site;
         PopProperties();
      }

      public void SetLocation(Location location)
      {
         Site.Latitude = location.Latitude;
         Site.Longitude = location.Longitude;
      }

      protected override bool OnSaveCommand()
      {
         PushProperties();
         return base.OnSaveCommand();
      }

      public void PopProperties()
      {
         _Site = new Site(_OriginalSite.Id) {
            SiteName = _OriginalSite.SiteName,
            Longitude = _OriginalSite.Longitude,
            Latitude = _OriginalSite.Latitude
         };

      }

      public void PushProperties()
      {
         _OriginalSite.Latitude = Site.Latitude;
         _OriginalSite.Longitude = Site.Longitude;

      }
   }
}