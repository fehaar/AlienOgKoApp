using NUnit.Framework;
using System.Collections.Generic;

namespace AlienOgKo.Tests
{
    public class MapBoundsTests
    {
        // Docs/mapdata.txt:
        // Kort NE 55.62350, 12.07042
        // Kort SW 55.61706, 12.08611
        const double NeLat = 55.62350, NeLon = 12.07042, SwLat = 55.61706, SwLon = 12.08611;

        [Test]
        public void FilterContained_KeepsPlaceFullyInsideBox()
        {
            var apollo = new Place
            {
                Tag = "Apollo",
                UpperLeftX1 = 55.621252,
                UpperLeftY1 = 12.071597,
                LowerRightX2 = 55.620282,
                LowerRightY2 = 12.073561
            };

            var result = MapBounds.FilterContained(new List<Place> { apollo }, NeLat, NeLon, SwLat, SwLon);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Apollo", result[0].Tag);
        }

        [Test]
        public void FilterContained_DropsPlaceThatOnlyOverlaps()
        {
            var roskildeFestival = new Place
            {
                Tag = "Roskilde Festival",
                UpperLeftX1 = 55.62291,
                UpperLeftY1 = 12.064422,
                LowerRightX2 = 55.60926,
                LowerRightY2 = 12.098569
            };

            var result = MapBounds.FilterContained(new List<Place> { roskildeFestival }, NeLat, NeLon, SwLat, SwLon);

            Assert.AreEqual(0, result.Count);
        }
    }
}
