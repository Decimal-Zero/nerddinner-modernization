using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NetTopologySuite.Geometries;

namespace NerdDinner.ModelBinders
{
    // ASP.NET Core equivalent of the legacy app's DbGeographyModelBinder
    // (src/ModelBinders/DbGeographyModelBinder.cs). Same "lat,long" posted
    // hidden-field format (see Views/Shared/EditorTemplates/Point.cshtml),
    // same DL-015 fix already baked in (a malformed/empty posted value
    // binds to null rather than throwing) -- there's no equivalent of the
    // original array-index bug to reintroduce here since this binder is
    // new code, not a straight port of the pre-fix version.
    public class PointModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult != ValueProviderResult.None && !string.IsNullOrEmpty(valueProviderResult.FirstValue))
            {
                string[] latLongStr = valueProviderResult.FirstValue.Split(',');
                if (latLongStr.Length == 2 && latLongStr[0].Length > 0 && latLongStr[1].Length > 0
                    && double.TryParse(latLongStr[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                    && double.TryParse(latLongStr[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    // 4326 format puts LONGITUDE first then LATITUDE -- NTS
                    // Point's constructor is (x, y) i.e. (longitude, latitude).
                    bindingContext.Result = ModelBindingResult.Success(new Point(lng, lat) { SRID = 4326 });
                    return Task.CompletedTask;
                }
            }

            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }
    }

    public class PointModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(Point))
            {
                return new PointModelBinder();
            }
            return null;
        }
    }
}
