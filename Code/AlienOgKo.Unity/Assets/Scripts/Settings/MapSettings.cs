using UnityEngine;

namespace AlienOgKo
{
    [CreateAssetMenu(fileName = "MapSettings", menuName = "AlienOgKo/Map Settings")]
    public class MapSettings : ScriptableObject
    {
        public const string ResourcePath = "MapSettings";

        [SerializeField] private string baseUrl = "https://hvor-fanden-er-fanen.osc-fr1.scalingo.io";
        [SerializeField] private string apiKey = "";
        [SerializeField] private double neLatitude = 55.62350;
        [SerializeField] private double neLongitude = 12.07042;
        [SerializeField] private double swLatitude = 55.61706;
        [SerializeField] private double swLongitude = 12.08611;

        public string BaseUrl => baseUrl;
        public string ApiKey => apiKey;
        public double NeLatitude => neLatitude;
        public double NeLongitude => neLongitude;
        public double SwLatitude => swLatitude;
        public double SwLongitude => swLongitude;
    }
}
