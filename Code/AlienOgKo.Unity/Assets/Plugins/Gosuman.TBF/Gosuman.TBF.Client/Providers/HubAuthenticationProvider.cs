using Best.HTTP;
using Best.SignalR;
using System;

namespace Gosuman.TBF.Providers
{
    public class HubAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string authorizationHeader;
        private readonly string queryParamName;
        private readonly string token;

        public HubAuthenticationProvider(string authorizationHeader, string queryParamName, string token)
        {
            this.authorizationHeader = authorizationHeader;
            this.queryParamName = queryParamName;
            this.token = token;
        }

        public bool IsPreAuthRequired => false;

#pragma warning disable 0067
        public event OnAuthenticationSuccededDelegate OnAuthenticationSucceded;
        public event OnAuthenticationFailedDelegate OnAuthenticationFailed;
#pragma warning restore 0067

        public void StartAuthentication()
        {
        }

        public void PrepareRequest(HTTPRequest request)
        {
            // Add Authorization header to http requests, add access_token param to the uri otherwise
            if (Best.HTTP.Hosts.Connections.HTTPProtocolFactory.GetProtocolFromUri(request.CurrentUri) == Best.HTTP.Hosts.Connections.SupportedProtocols.HTTP)
                request.SetHeader("Authorization", authorizationHeader);
            else
#if !BESTHTTP_DISABLE_WEBSOCKET
                if (Best.HTTP.Hosts.Connections.HTTPProtocolFactory.GetProtocolFromUri(request.Uri) != Best.HTTP.Hosts.Connections.SupportedProtocols.WebSocket)
                request.Uri = PrepareUriImpl(request.Uri);
#else
                ;
#endif
        }

        public Uri PrepareUri(Uri uri)
        {
            if (uri.Query.StartsWith("??"))
            {
                UriBuilder builder = new UriBuilder(uri);
                builder.Query = builder.Query.Substring(2);

                return builder.Uri;
            }

#if !BESTHTTP_DISABLE_WEBSOCKET
            if (Best.HTTP.Hosts.Connections.HTTPProtocolFactory.GetProtocolFromUri(uri) == Best.HTTP.Hosts.Connections.SupportedProtocols.WebSocket)
                uri = PrepareUriImpl(uri);
#endif

            return uri;

        }

        private Uri PrepareUriImpl(Uri uri)
        {
            string query = string.IsNullOrEmpty(uri.Query) ? "" : uri.Query + "&";
            UriBuilder uriBuilder = new UriBuilder(uri.Scheme, uri.Host, uri.Port, uri.AbsolutePath, query + queryParamName + "=" + token);
            return uriBuilder.Uri;
        }

        public void Cancel()
        {
        }
    }
}
