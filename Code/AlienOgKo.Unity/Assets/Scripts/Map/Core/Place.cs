using Newtonsoft.Json;
using System;

namespace AlienOgKo
{
    public class Place
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("TS")]
        public DateTime Ts { get; set; }

        [JsonProperty("fixed")]
        public bool Fixed { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("upperLeftX1")]
        public double UpperLeftX1 { get; set; }

        [JsonProperty("upperLeftY1")]
        public double UpperLeftY1 { get; set; }

        [JsonProperty("lowerRightX2")]
        public double LowerRightX2 { get; set; }

        [JsonProperty("lowerRightY2")]
        public double LowerRightY2 { get; set; }
    }
}
