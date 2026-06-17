using UnityEngine;

namespace Gosuman.TBF
{
    [CreateAssetMenu(fileName = "ServerSettings", menuName = "Gosuman/Server Settings")]
    public class ServerSettings : ScriptableObject
    {
        public const string ResourcePath = "ServerSettings";

        [SerializeField] private string localServerUrl = "https://localhost:32769";
        [SerializeField] private string remoteServerUrl = "";

        public string LocalServerUrl => localServerUrl;
        public string RemoteServerUrl => remoteServerUrl;
    }
}
