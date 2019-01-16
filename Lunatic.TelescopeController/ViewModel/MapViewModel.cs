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