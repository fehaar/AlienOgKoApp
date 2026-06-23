using AlienOgKo;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AlienOgKo.Tests
{
    public class PlaceTests
    {
        [Test]
        public void Deserialize_ParsesRealApiShape()
        {
            const string json = @"{
                ""ID"": 15,
                ""TS"": ""2026-02-14T21:39:43.000Z"",
                ""fixed"": true,
                ""lowerRightX2"": 55.620282,
                ""lowerRightY2"": 12.073561,
                ""tag"": ""Apollo"",
                ""upperLeftX1"": 55.621252,
                ""upperLeftY1"": 12.071597
            }";

            var place = JsonConvert.DeserializeObject<Place>(json);

            Assert.AreEqual(15, place.Id);
            Assert.AreEqual("Apollo", place.Tag);
            Assert.IsTrue(place.Fixed);
            Assert.AreEqual(55.621252, place.UpperLeftX1, 1e-6);
            Assert.AreEqual(12.071597, place.UpperLeftY1, 1e-6);
            Assert.AreEqual(55.620282, place.LowerRightX2, 1e-6);
            Assert.AreEqual(12.073561, place.LowerRightY2, 1e-6);
        }
    }
}
