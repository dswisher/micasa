// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.GitHub;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Micasa.UnitTests.GitHub
{
    public class GitHubReleaseInfoFetcherTests
    {
        private static readonly Assembly Assembly = typeof(GitHubReleaseInfoFetcherTests).Assembly;

        private readonly IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        private readonly GitHubReleaseInfoFetcher fetcher;

        private readonly CancellationToken token = CancellationToken.None;


        public GitHubReleaseInfoFetcherTests()
        {
            fetcher = new GitHubReleaseInfoFetcher(httpClientFactory);
        }


        [Fact]
        public async Task CanFetch()
        {
            // Arrange
            const string path = "Micasa.UnitTests.GitHub.TestFiles.bat.json";
            string responseJson;
            await using (var stream = Assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Could not load resource: {path}");
                }

                using (var reader = new StreamReader(stream))
                {
                    responseJson = await reader.ReadToEndAsync(token);
                }
            }

            var mockHttpMessageHandler = new MockHttpMessageHandler(responseJson);
            var httpClient = new HttpClient(mockHttpMessageHandler);
            httpClientFactory.CreateClient().Returns(httpClient);

            // Act
            var info = await fetcher.FetchLatestReleaseInfoAsync("https://github.com/sharkdp/bat", token);

            // Assert
            info.ShouldNotBeNull();
            info.Name.ShouldBe("v0.26.1");
            info.Assets.ShouldContain(x => x.Name == "bat-musl_0.26.1_arm64.deb");
            info.Assets.Count.ShouldBe(21);
        }


        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string response;

            public MockHttpMessageHandler(string response)
            {
                this.response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(response, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
