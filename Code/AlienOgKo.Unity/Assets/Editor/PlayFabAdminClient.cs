using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlienOgKo.Editor
{
    internal static class PlayFabAdminClient
    {
        private static readonly HttpClient http = new HttpClient();

        public class PlayFabException : Exception
        {
            public string PlayFabErrorCode { get; }
            public PlayFabException(string code, string message) : base(message)
            {
                PlayFabErrorCode = code;
            }
        }

        public static async Task<string> LoginAsync(string titleId, string secretKey, string customId)
        {
            var body = JsonConvert.SerializeObject(new { CustomId = customId, CreateAccount = false });
            try
            {
                var data = await CallAsync(titleId, secretKey, "/Server/LoginWithCustomID", body);
                return (string)data["PlayFabId"];
            }
            catch (PlayFabException ex) when (ex.PlayFabErrorCode == "AccountNotFound")
            {
                return null;
            }
        }

        public static async Task<string> GetReadOnlyDataAsync(string titleId, string secretKey, string playFabId, string key)
        {
            var body = JsonConvert.SerializeObject(new { PlayFabId = playFabId, Keys = new[] { key } });
            var data = await CallAsync(titleId, secretKey, "/Server/GetUserReadOnlyData", body);
            return (string)data["Data"]?[key]?["Value"];
        }

        public static async Task RemoveReadOnlyKeyAsync(string titleId, string secretKey, string playFabId, string key)
        {
            var body = JsonConvert.SerializeObject(new { PlayFabId = playFabId, KeysToRemove = new[] { key } });
            await CallAsync(titleId, secretKey, "/Server/UpdateUserReadOnlyData", body);
        }

        public static async Task EvictPlayerCacheAsync(string serverBaseUrl, string secretKey, string ulid)
        {
            using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("X-PlayFab-SecretKey", secretKey);
            var resp = await client.DeleteAsync($"{serverBaseUrl.TrimEnd('/')}/admin/player/{ulid}");
            if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
                throw new Exception($"Server eviction returned {(int)resp.StatusCode}");
        }

        private static async Task<JObject> CallAsync(string titleId, string secretKey, string path, string body)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"https://{titleId}.playfabapi.com{path}");
            req.Headers.Add("X-SecretKey", secretKey);
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var resp = await http.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            var parsed = JObject.Parse(text);
            if (!resp.IsSuccessStatusCode || (int?)parsed["code"] != 200)
            {
                var code = (string)parsed["error"] ?? "Unknown";
                var msg = (string)parsed["errorMessage"] ?? (string)parsed["error"] ?? text;
                throw new PlayFabException(code, msg);
            }
            return (JObject)parsed["data"];
        }
    }
}
