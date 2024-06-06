using System;
using System.Reflection;
using Aquarius.Helpers;
using Aquarius.TimeSeries.Client.EndPoints;
using Aquarius.TimeSeries.Client.Helpers;
using ServiceStack;
using ServiceStack.Logging;

namespace Aquarius.TimeSeries.Client
{
    public class Authenticator : IAuthenticator
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static IAuthenticator Create(string hostname)
        {
            return new Authenticator(hostname);
        }

        public static IAuthenticator Create(string hostname, NonStandardRoot nonStandardRoot)
        {
            return new Authenticator(hostname, nonStandardRoot);
        }

        internal IServiceClient Client { get; set; }
        private AquariusServerVersion ServerVersion { get; }

        private Authenticator(string hostname)
        {
            Client = new SdkServiceClient(PublishV2.ResolveEndpoint(hostname));

            ServerVersion = AquariusSystemDetector.Instance.GetAquariusServerVersion(hostname);
        }

        private Authenticator(string hostname, NonStandardRoot nonStandardRoot)
        {
            Client = new SdkServiceClient(PublishV2.ResolveEndpoint(hostname, nonStandardRoot));

            ServerVersion = AquariusSystemDetector.Instance.GetAquariusServerVersion(hostname, nonStandardRoot);
        }

        public string Login(string username, string password)
        {
            return ClientHelper.Login(Client, username, password);
        }

        public void Login(string accessToken)
        {
            throw new NotImplementedException($"Credentials authenticator requires a username and password to login.");
        }

        public void Logout()
        {
            try
            {
                if (!FirstNgVersion.IsLessThan(ServerVersion)) return;

                ClientHelper.Logout(Client);
            }
            catch (Exception exception)
            {
                Log.Warn("Ignoring exception during disconnect", exception);
            }
        }

        private static readonly AquariusServerVersion FirstNgVersion = AquariusServerVersion.Create("14");
    }
}
