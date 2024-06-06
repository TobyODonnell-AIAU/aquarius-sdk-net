namespace Aquarius.TimeSeries.Client.EndPoints
{
    public class NonStandardRoot
    {
        public NonStandardRoot(string endPoint)
        {
            EndPoint = endPoint;
        }

        public string EndPoint { get; }

        public string EndpointAtRoot(string standardEndpoint)
        {
            return standardEndpoint.Replace(Root.EndPoint, EndPoint);
        }
    }
}
