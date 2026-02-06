// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Micasa.Cli.GitHub;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models.GitHub;
using Micasa.Cli.Serialization;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Micasa.UnitTests.GitHub
{
    public class GitHubArchiveSeekerTests
    {
        private static readonly Assembly Assembly = typeof(GitHubArchiveSeekerTests).Assembly;
        private readonly JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
        };

        private readonly IPlatformDecoder platformDecoder = Substitute.For<IPlatformDecoder>();

        private readonly GitHubArchiveSeeker seeker;

        public GitHubArchiveSeekerTests()
        {
            seeker = new GitHubArchiveSeeker(platformDecoder);
        }


        [Theory]
        [InlineData("bat.json", "amd64", "https://github.com/sharkdp/bat/releases/download/v0.26.1/bat-v0.26.1-x86_64-unknown-linux-musl.tar.gz")]
        [InlineData("bat.json", "arm64", "https://github.com/sharkdp/bat/releases/download/v0.26.1/bat-v0.26.1-aarch64-unknown-linux-musl.tar.gz")]
        [InlineData("fd.json", "amd64", "https://github.com/sharkdp/fd/releases/download/v10.3.0/fd-v10.3.0-x86_64-unknown-linux-musl.tar.gz")]
        [InlineData("fd.json", "arm64", "https://github.com/sharkdp/fd/releases/download/v10.3.0/fd-v10.3.0-aarch64-unknown-linux-musl.tar.gz")]
        [InlineData("fzf.json", "amd64", "https://github.com/junegunn/fzf/releases/download/v0.67.0/fzf-0.67.0-linux_amd64.tar.gz")]
        [InlineData("fzf.json", "arm64", "https://github.com/junegunn/fzf/releases/download/v0.67.0/fzf-0.67.0-linux_arm64.tar.gz")]
        [InlineData("eza.json", "amd64", "https://github.com/eza-community/eza/releases/download/v0.23.4/eza_x86_64-unknown-linux-musl.tar.gz")]
        [InlineData("eza.json", "arm64", "https://github.com/eza-community/eza/releases/download/v0.23.4/eza_aarch64-unknown-linux-gnu.tar.gz")]
        [InlineData("lazygit.json", "amd64", "https://github.com/jesseduffield/lazygit/releases/download/v0.58.1/lazygit_0.58.1_linux_x86_64.tar.gz")]
        [InlineData("lazygit.json", "arm64", "https://github.com/jesseduffield/lazygit/releases/download/v0.58.1/lazygit_0.58.1_linux_arm64.tar.gz")]
        [InlineData("neovim.json", "amd64", "https://github.com/neovim/neovim/releases/download/v0.11.6/nvim-linux-x86_64.appimage")]
        [InlineData("neovim.json", "arm64", "https://github.com/neovim/neovim/releases/download/v0.11.6/nvim-linux-arm64.appimage")]
        [InlineData("ripgrep.json", "amd64", "https://github.com/BurntSushi/ripgrep/releases/download/15.1.0/ripgrep-15.1.0-x86_64-unknown-linux-musl.tar.gz")]
        [InlineData("ripgrep.json", "arm64", "https://github.com/BurntSushi/ripgrep/releases/download/15.1.0/ripgrep-15.1.0-aarch64-unknown-linux-gnu.tar.gz")]
        private void CanFindArchive(string filename, string arch, string expectedUrl)
        {
            // Arrange
            var path = $"Micasa.UnitTests.GitHub.TestFiles.{filename}";
            GitHubReleaseInfo? releaseInfo;
            using (var stream = Assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Could not load resource: {path}");
                }

                releaseInfo = JsonSerializer.Deserialize<GitHubReleaseInfo>(stream, serializerOptions);

                if (releaseInfo == null)
                {
                    throw new Exception($"Could not deserialize resource: {path}");
                }
            }

            platformDecoder.SystemArchitecture.Returns(arch);

            // Act
            var actualUrl = seeker.FindBestAsset(releaseInfo);

            // Assert
            actualUrl.BrowserDownloadUrl.ShouldBe(expectedUrl);
        }
    }
}
