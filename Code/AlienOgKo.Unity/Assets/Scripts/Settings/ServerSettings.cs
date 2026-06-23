using UnityEngine;

namespace Gosuman.TBF
{
    /// <summary>The identity backend a build authenticates against. A build ships with exactly one.</summary>
    public enum AuthBackendType
    {
        PlayFab,
        Nakama
    }

    [CreateAssetMenu(fileName = "ServerSettings", menuName = "Gosuman/Server Settings")]
    public class ServerSettings : ScriptableObject
    {
        public const string ResourcePath = "ServerSettings";

        [Header("Identity backend")]
        [SerializeField] private AuthBackendType authBackend = AuthBackendType.Nakama;

        [Header("Nakama (used when Auth Backend = Nakama)")]
        [SerializeField] private string nakamaScheme = "http";
        [SerializeField] private string nakamaHost = "localhost";
        [SerializeField] private int nakamaPort = 7350;
        [SerializeField] private string nakamaServerKey = "defaultkey";

        public AuthBackendType AuthBackend => authBackend;
        public string NakamaScheme => nakamaScheme;
        public string NakamaHost => nakamaHost;
        public int NakamaPort => nakamaPort;
        public string NakamaServerKey => nakamaServerKey;
    }
}
