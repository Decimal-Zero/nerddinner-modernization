using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NerdDinner
{
    public class DbGeographyModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult != null && !string.IsNullOrEmpty(valueProviderResult.AttemptedValue))
            {
                // The posted value is "" whenever no location has been
                // geocoded/dragged onto the map yet (see the DbGeography.cshtml
                // editor template's empty-hidden-field fallback) -- Location
                // has no [Required] attribute, so treat anything that isn't a
                // well-formed "lat,long" pair as "no location" rather than
                // indexing into a one-element array.
                string[] latLongStr = valueProviderResult.AttemptedValue.Split(',');
                if (latLongStr.Length == 2 && latLongStr[0].Length > 0 && latLongStr[1].Length > 0)
                {
                    string point = string.Format("POINT ({0} {1})", latLongStr[1], latLongStr[0]);
                    //4326 format puts LONGITUDE first then LATITUDE
                    DbGeography result = DbGeography.FromText(point, 4326);
                    return result;
                }
            }
            return null;
        }
    }

    public class EFModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(Type modelType)
        {
            if (modelType == typeof(DbGeography))
            {
                return new DbGeographyModelBinder();
            }
            return null;
        }
    }
}