// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Models.GitHub;
using Micasa.Cli.Serialization;

namespace Micasa.Cli.GitHub
{
    public class GitHubReleaseInfoFetcher : IGitHubReleaseInfoFetcher
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
        };


        public GitHubReleaseInfoFetcher(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }


        public async Task<GitHubReleaseInfo> FetchLatestReleaseInfoAsync(string repoUrl, CancellationToken stoppingToken)
        {
            // Pick apart the repo to get the API url
            //      https://github.com/{owner}/{repo}  ->  https://api.github.com/repos/{owner}/{repo}/releases/latest
            var apiUrl = BuildApiUrl(repoUrl);

            // Fetch the content from GitHub API
            using (var httpClient = httpClientFactory.CreateClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Micasa");

                // TODO - if a github token is provided in the config, use it

                using (var response = await httpClient.GetAsync(apiUrl, stoppingToken))
                {
                    response.EnsureSuccessStatusCode();

                    // Deserialize the JSON response content
                    await using (var stream = await response.Content.ReadAsStreamAsync(stoppingToken))
                    {
                        var releaseInfo = await JsonSerializer.DeserializeAsync<GitHubReleaseInfo>(stream, serializerOptions, stoppingToken);

                        if (releaseInfo == null)
                        {
                            throw new InvalidOperationException($"Failed to deserialize GitHub release info from {apiUrl}");
                        }

                        return releaseInfo;
                    }
                }
            }
        }


        private static string BuildApiUrl(string repoUrl)
        {
            // Parse: https://github.com/{owner}/{repo}
            var uri = new Uri(repoUrl);
            var pathParts = uri.AbsolutePath.Trim('/').Split('/');

            if (pathParts.Length < 2)
            {
                throw new ArgumentException($"Invalid GitHub repository URL: {repoUrl}", nameof(repoUrl));
            }

            var owner = pathParts[0];
            var repo = pathParts[1];

            return $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        }
    }
}
