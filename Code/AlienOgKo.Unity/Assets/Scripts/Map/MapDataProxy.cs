using Best.HTTP;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using PureMVC.Patterns.Proxy;
using System.Collections.Generic;
using UnityEngine;

namespace AlienOgKo
{
    public class MapDataProxy : Proxy
    {
        public static new string NAME = "MapDataProxy";

        public static class Notifications
        {
            public const string PlacesLoaded = "MapDataProxy.PlacesLoaded";
            public const string LoadPlacesFailed = "MapDataProxy.LoadPlacesFailed";
        }

        private readonly MapSettings settings;

        public IReadOnlyList<Place> Places { get; private set; } = new List<Place>();

        public MapDataProxy(MapSettings settings) : base(NAME)
        {
            this.settings = settings;
        }

        public override void OnRegister()
        {
            base.OnRegister();
            LoadPlaces().Forget();
        }

        public async UniTaskVoid LoadPlaces()
        {
            var request = HTTPRequest.CreateGet($"{settings.BaseUrl}/places/all");
            request.SetHeader("Authorization", settings.ApiKey);
            await request.Send().WithCancellation(Application.exitCancellationToken);

            if (request.State != HTTPRequestStates.Finished || !request.Response.IsSuccess)
            {
                Debug.LogError($"Could not load places. {request.State} {request.Response?.StatusCode}");
                SendNotification(Notifications.LoadPlacesFailed);
                return;
            }

            var allPlaces = JsonConvert.DeserializeObject<List<Place>>(request.Response.DataAsText);
            Places = MapBounds.FilterContained(allPlaces, settings.NeLatitude, settings.NeLongitude, settings.SwLatitude, settings.SwLongitude);
            Debug.Log($"MapDataProxy loaded {Places.Count} places within map bounds");
            SendNotification(Notifications.PlacesLoaded, Places);
        }
    }
}
