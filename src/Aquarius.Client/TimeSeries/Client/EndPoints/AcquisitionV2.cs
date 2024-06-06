﻿using Aquarius.Helpers;

namespace Aquarius.TimeSeries.Client.EndPoints
{
    public class AcquisitionV2
    {
        public const string EndPoint = Root.EndPoint + "/Acquisition/v2";

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
