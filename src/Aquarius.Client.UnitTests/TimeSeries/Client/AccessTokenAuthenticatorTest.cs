using Aquarius.TimeSeries.Client;
using Aquarius.TimeSeries.Client.EndPoints;
using Aquarius.TimeSeries.Client.ServiceModels.Publish;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using ServiceStack;

#if AUTOFIXTURE4
using AutoFixture;
#else
using Ploeh.AutoFixture;
#endif

namespace Aquarius.Client.UnitTests.TimeSeries.Client
{
    [TestFixture]
    public class AccessTokenAuthenticatorTest : AuthenticatorTestBase<AccessTokenAuthenticator>
    {
        protected override IServiceClient CreateMockServiceClient()
        {
            return Substitute.For<IServiceClient>();
        }

        private AccessTokenAuthenticator CreateAuthenticator(bool atNonStandardRoot)
        {
            var authenticator =
                (atNonStandardRoot
                    ? AccessTokenAuthenticator.Create("dummyhost", new NonStandardRoot("/dummyroot"))
                    : AccessTokenAuthenticator.Create("dummyhost")) as AccessTokenAuthenticator;

            if (authenticator != null)
            {
                authenticator.Client = _mockServiceClient;
            }

            return authenticator;
        }

        [Test]
        public void Login_SetsClientBearerToken([Values] bool atNonStandardRoot)
        {
            var authenticator = CreateAuthenticator(atNonStandardRoot);

            if (authenticator != null && authenticator is AccessTokenAuthenticator tokenAuthenticator)
            {
                var mockAccessToken = _fixture.Create<string>();
                authenticator.Login(mockAccessToken);
                
                tokenAuthenticator.Client.BearerToken.Should().BeEquivalentTo(mockAccessToken);
            }
        }

        [Test]
        public void Logout_CallsDeleteSession([Values] bool atNonStandardRoot)
        {
            var authenticator = CreateAuthenticator(atNonStandardRoot);
            
            authenticator.Logout();

            _mockServiceClient.Received(1).Delete(Arg.Any<DeleteSession>());
        }
    }
}