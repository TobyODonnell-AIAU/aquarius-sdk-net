﻿using System;
using System.Collections.Generic;
using System.Linq;
using Aquarius.Helpers;
using Aquarius.TimeSeries.Client;
using Aquarius.TimeSeries.Client.EndPoints;
using Aquarius.TimeSeries.Client.Helpers;
using Aquarius.TimeSeries.Client.ServiceModels.Provisioning;
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
    public class AquariusClientTests
    {
        private IFixture _fixture;
        private AquariusClient _client;
        private IServiceClient _mockPublish;
        private IServiceClient _mockAcquisition;
        private IServiceClient _mockProvisioning;
        private IAuthenticator _mockAuthenticator;
        private string _mockHost;
        private string _mockNonStandardRoot;

        [SetUp]
        public void BeforeEachTest()
        {
            _fixture = new Fixture();
        }

        private void SetUpClientWithMockEndpoints(AuthenticationType authType, bool useNonStandardRoot)
        {
            _client = new AquariusClient(authType)
            {
                ServerVersion = CreateDeveloperBuild()
            };

            _mockPublish = CreateMockServiceClient();
            _mockAcquisition = CreateMockServiceClient();
            _mockProvisioning = CreateMockServiceClient();
            _mockAuthenticator = CreateMockAuthenticator();

            _client.Connection = CreateMockConnection(authType);

            _mockHost = $"https://{Guid.NewGuid().ToString()}.com";
            _mockNonStandardRoot = "/" + Guid.NewGuid();
            var mockRoot = useNonStandardRoot
                ? new NonStandardRoot(_mockNonStandardRoot)
                : default(NonStandardRoot);

            _client.AddServiceClient(AquariusClient.ClientType.PublishJson, _mockPublish, PublishV2.ResolveEndpoint(_mockHost, mockRoot));
            _client.AddServiceClient(AquariusClient.ClientType.AcquisitionJson, _mockAcquisition, AcquisitionV2.ResolveEndpoint(_mockHost, mockRoot));
            _client.AddServiceClient(AquariusClient.ClientType.ProvisioningJson, _mockProvisioning, Provisioning.ResolveEndpoint(_mockHost, mockRoot));
        }

        private AquariusServerVersion CreateDeveloperBuild()
        {
            return AquariusServerVersion.Create("0");
        }

        private IServiceClient CreateMockServiceClient()
        {
            var mockServiceClient = Substitute.For<IServiceClient>();

            return mockServiceClient;
        }

        private IAuthenticator CreateMockAuthenticator()
        {
            var mockAuthenticator = Substitute.For<IAuthenticator>();

            return mockAuthenticator;
        }

        private IConnection CreateMockConnection(AuthenticationType authType)
        {
            var mockHostname = _fixture.Create<string>();

            return authType == AuthenticationType.Credential
                ? new Connection(mockHostname, _fixture.Create<string>(), _fixture.Create<string>(),
                    _mockAuthenticator, connection => { }) as IConnection
                : new AccessTokenConnection(mockHostname, _fixture.Create<string>(),
                    _mockAuthenticator, connection => { }) as IConnection;
        }

        [Ignore("This integration test should only be run from within the IDE, connecting to a live AQTS app server")]
        [Test]
        public void IntegrationTest_SequentialConnectionsToTheSameServer_Succeed()
        {
            var u1 = GetUnitsFromALiveServer();
            var u2 = GetUnitsFromALiveServer();

            u1.ShouldBeEquivalentTo(u2);
        }

        private List<PopulatedUnitGroup> GetUnitsFromALiveServer()
        {
            using (var client = AquariusClient.CreateConnectedClient("doug-vm2019", "admin", "admin"))
            {
                return client.Provisioning.Get(new GetUnits()).Results;
            }
        }

        [Test]
        public void Publish_HasBaseUri([Values] AuthenticationType authType, [Values] bool atNonStandardRoot)
        {
            SetUpClientWithMockEndpoints(authType, atNonStandardRoot);
            AssertClientHasBaseUri(_client.Publish, atNonStandardRoot);
        }

        [Test]
        public void Acquisition_HasBaseUri([Values] AuthenticationType authType, [Values] bool atNonStandardRoot)
        {
            SetUpClientWithMockEndpoints(authType, atNonStandardRoot);
            AssertClientHasBaseUri(_client.Acquisition, atNonStandardRoot);
        }

        [Test]
        public void Provisioning_HasBaseUri([Values] AuthenticationType authType, [Values] bool atNonStandardRoot)
        {
            SetUpClientWithMockEndpoints(authType, atNonStandardRoot);
            AssertClientHasBaseUri(_client.Provisioning, atNonStandardRoot);
        }

        private void AssertClientHasBaseUri(IServiceClient serviceClient, bool atNonStandardRoot)
        {
            var baseUri = _client.GetBaseUri(serviceClient);

            baseUri.Should().NotBeNullOrWhiteSpace();
            baseUri.Should().StartWith(_mockHost);
            baseUri.Should().Contain(atNonStandardRoot ? _mockNonStandardRoot : Root.EndPoint);
        }

        [Test]
        public void UnregisteredServiceClient_HasNoBaseUri()
        {
            var otherClient = new JsonServiceClient();

            var baseUri = _client.GetBaseUri(otherClient);

            baseUri.Should().BeNullOrEmpty();
        }

        [Test]
        public void Client_AccessTokenAuthentication_DoesNotSetReAuthentication()
        {
            var token = _fixture.Create<string>();
            if (!(AquariusClient.CreateConnectedClient(_fixture.Create<string>(), token) is AquariusClient client))
                throw new ArgumentException($"{nameof(AquariusClient)} cannot be null");

            foreach (var kv in client.ServiceClients)
            {
                var serviceClient = (SdkServiceClient) kv.Value;
                serviceClient.OnAuthenticationRequired.Should().BeNull();
            }

            AssertAllClientsHaveBearerToken(client, token);
        }

        private static void AssertAllClientsHaveBearerToken(AquariusClient aquariusClient, string token)
        {
            aquariusClient.Connection.Token().Should().Be(token);
            foreach (var client in aquariusClient.ServiceClients.Values.Concat(aquariusClient.CustomClients.Values))
            {
                client.BearerToken.Should().Be(token);
            }
        }

        [Test]
        public void UpdateAccessToken_ForAccessTokenAuthentication_UpdatesBearerTokenForAllClients()
        {
            var token = _fixture.Create<string>();
            if (!(AquariusClient.CreateConnectedClient(_fixture.Create<string>(), token) is AquariusClient client))
                throw new ArgumentException($"{nameof(AquariusClient)} cannot be null");
            AssertAllClientsHaveBearerToken(client, token);

            var newToken = _fixture.Create<string>();
            client.UpdateAccessToken(newToken);

            AssertAllClientsHaveBearerToken(client, newToken);
        }

        [Test]
        public void UpdateAccessToken_ForCredentialsAuthentication_Throws([Values] bool atNonStandardRoot)
        {
            SetUpClientWithMockEndpoints(AuthenticationType.Credential, atNonStandardRoot);

            Assert.That(() => _client.UpdateAccessToken(_fixture.Create<string>()), Throws.Exception.TypeOf<NotImplementedException>());
        }
    }
}
