﻿using Aquarius.Helpers;

namespace Aquarius.TimeSeries.Client.EndPoints
{
    public class LegacyDataService
    {
        public const string EndPoint = Root.EndPoint + "/AquariusDataService.svc";

        public static string ResolveEndpoint(string host)
        {
            return UriHelper.ResolveEndpoint(host, EndPoint);
        }

        public static string ResolveEndpoint(string host, NonStandardRoot root)
        {
            return root == null
                ? ResolveEndpoint(host)
                : UriHelper.ResolveEndpoint(host, root.EndpointAtRoot(EndPoint));
        }
    }
}
