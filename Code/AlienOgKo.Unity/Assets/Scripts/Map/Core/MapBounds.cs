using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienOgKo
{
    public static class MapBounds
    {
        public static List<Place> FilterContained(
            IEnumerable<Place> places, double neLatitude, double neLongitude, double swLatitude, double swLongitude)
        {
            double latMin = Math.Min(neLatitude, swLatitude);
            double latMax = Math.Max(neLatitude, swLatitude);
            double lonMin = Math.Min(neLongitude, swLongitude);
            double lonMax = Math.Max(neLongitude, swLongitude);

            return places.Where(p => IsFullyContained(p, latMin, latMax, lonMin, lonMax)).ToList();
        }

        private static bool IsFullyContained(Place place, double latMin, double latMax, double lonMin, double lonMax)
        {
            double placeLatMin = Math.Min(place.UpperLeftX1, place.LowerRightX2);
            double placeLatMax = Math.Max(place.UpperLeftX1, place.LowerRightX2);
            double placeLonMin = Math.Min(place.UpperLeftY1, place.LowerRightY2);
            double placeLonMax = Math.Max(place.UpperLeftY1, place.LowerRightY2);

            return placeLatMin >= latMin && placeLatMax <= latMax
                && placeLonMin >= lonMin && placeLonMax <= lonMax;
        }
    }
}
