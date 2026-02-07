// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Micasa.UnitTests.Helpers
{
    public class FileDistributorTests
    {
        private const string DirPath = "/tmp/files";
        private const string HomeDir = "/home/testuser";
        private const string BinaryContent = "binary-file";

        private readonly IEnvironmentHelper environment = Substitute.For<IEnvironmentHelper>();
        private readonly ILogger<FileDistributor> logger = Substitute.For<ILogger<FileDistributor>>();
        private readonly MockFileData fileData = new("miscellaneous-file-content");
        private readonly MockFileData binaryData = new(BinaryContent);
        private readonly CancellationToken token = CancellationToken.None;

        public FileDistributorTests()
        {
            environment.GetHomeDirectory().Returns(HomeDir);
        }


        [Fact]
        public async Task CanHandleFdLinuxArm64()
        {
            // Arrange
            var fdDir = $"{DirPath}/fd-v10.3.0-aarch64-unknown-linux-musl";

            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { $"{DirPath}/fd-v10.3.0-aarch64-unknown-linux-musl.tar.gz", fileData },
                { $"{fdDir}/CHANGELOG.md", fileData },
                { $"{fdDir}/LICENSE-APACHE", fileData },
                { $"{fdDir}/LICENSE-MIT", fileData },
                { $"{fdDir}/README.md", fileData },
                { $"{fdDir}/autocomplete/_fd", fileData },
                { $"{fdDir}/autocomplete/fd.bash", fileData },
                { $"{fdDir}/autocomplete/fd.fish", fileData },
                { $"{fdDir}/autocomplete/fd.ps1", fileData },
                { $"{fdDir}/fd", binaryData },
                { $"{fdDir}/fd.1", fileData },
            });

            var distributor = new FileDistributor(fileSystem, environment, logger);

            // Act
            var ok = distributor.DistributeFiles(DirPath);

            // Assert
            ok.ShouldBeTrue();

            fileSystem.File.Exists($"{HomeDir}/.local/bin/fd").ShouldBeTrue();
            fileSystem.File.Exists($"{HomeDir}/.local/share/man/man1/fd.1").ShouldBeTrue();

            VerifyFileContents(fileSystem, $"{HomeDir}/.local/bin/fd", BinaryContent);
        }


        [Fact]
        public async Task CanHandleBatLinuxArm64()
        {
            // Arrange
            var batDir = $"{DirPath}/bat-v0.26.1-aarch64-unknown-linux-musl";

            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { $"{DirPath}/bat-v0.26.1-aarch64-unknown-linux-musl.tar.gz", fileData },
                { $"{batDir}/CHANGELOG.md", fileData },
                { $"{batDir}/LICENSE-APACHE", fileData },
                { $"{batDir}/LICENSE-MIT", fileData },
                { $"{batDir}/README.md", fileData },
                { $"{batDir}/autocomplete/_bat.ps1", fileData },
                { $"{batDir}/autocomplete/bat.bash", fileData },
                { $"{batDir}/autocomplete/bat.fish", fileData },
                { $"{batDir}/autocomplete/bat.zsh", fileData },
                { $"{batDir}/bat", fileData },
                { $"{batDir}/bat.1", fileData },
            });

            var distributor = new FileDistributor(fileSystem, environment, logger);

            // Act
            var ok = distributor.DistributeFiles(DirPath);

            // Assert
            ok.ShouldBeTrue();

            fileSystem.File.Exists($"{HomeDir}/.local/bin/bat").ShouldBeTrue();
            fileSystem.File.Exists($"{HomeDir}/.local/share/man/man1/bat.1").ShouldBeTrue();
        }


        [Fact]
        public async Task CanHandleLazygitLinuxArm64()
        {
            // Arrange
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { $"{DirPath}/lazygit_0.58.1_linux_armv6.tar.gz", fileData },
                { $"{DirPath}/LICENSE", fileData },
                { $"{DirPath}/README.md", fileData },
                { $"{DirPath}/lazygit", fileData },
            });

            var distributor = new FileDistributor(fileSystem, environment, logger);

            // Act
            var ok = distributor.DistributeFiles(DirPath);

            // Assert
            ok.ShouldBeTrue();

            fileSystem.File.Exists($"{HomeDir}/.local/bin/lazygit").ShouldBeTrue();
        }


        [Fact]
        public async Task CanHandleFzfLinuxArm64()
        {
            // Arrange
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { $"{DirPath}/fzf-0.67.0-linux_arm64.tar.gz", fileData },
                { $"{DirPath}/fzf", fileData },
            });

            var distributor = new FileDistributor(fileSystem, environment, logger);

            // Act
            var ok = distributor.DistributeFiles(DirPath);

            // Assert
            ok.ShouldBeTrue();

            fileSystem.File.Exists($"{HomeDir}/.local/bin/fzf").ShouldBeTrue();
        }


        [Fact]
        public async Task CanHandleEzaLinuxArm64()
        {
            // Arrange
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { $"{DirPath}/eza_aarch64-unknown-linux-gnu.tar.gz", fileData },
                { $"{DirPath}/eza", fileData },
            });

            var distributor = new FileDistributor(fileSystem, environment, logger);

            // Act
            var ok = distributor.DistributeFiles(DirPath);

            // Assert
            ok.ShouldBeTrue();

            fileSystem.File.Exists($"{HomeDir}/.local/bin/eza").ShouldBeTrue();
        }


        [Fact]
        public async Task CanHandleNeovimLinuxArm64()
        {
            // Arrange
            var nvimDir = $"{DirPath}/nvim-linux-arm64";

            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { $"{DirPath}/nvim-linux-arm64.tar.gz", fileData },
                { $"{nvimDir}/bin/nvim", binaryData },
                { $"{nvimDir}/lib/nvim/parser/lua.so", fileData },
                { $"{nvimDir}/share/nvim/applications/nvim.desktop", fileData },
                { $"{nvimDir}/share/nvim/icons/hicolor/128x128/apps/nvim.png", fileData },
                { $"{nvimDir}/share/nvim/man/man1/nvim.1", fileData },
                { $"{nvimDir}/share/nvim/runtime/autoload/README.txt", fileData },
                { $"{nvimDir}/share/nvim/runtime/doc/tips.txt", fileData },
                { $"{nvimDir}/share/nvim/runtime/tutor/en/vim-01-beginner.tutor", fileData },
            });

            var distributor = new FileDistributor(fileSystem, environment, logger);

            // Act
            var ok = distributor.DistributeFiles(DirPath);

            // Assert
            ok.ShouldBeTrue();

            VerifySymlink(fileSystem, "~/.local/bin/nvim", "~/.local/opt/nvim/bin/nvim");

            fileSystem.File.Exists($"{HomeDir}/.local/opt/nvim/lib/nvim/parser/lua.so").ShouldBeTrue();
            fileSystem.File.Exists($"{HomeDir}/.local/opt/nvim/share/nvim/man/man1/nvim.1").ShouldBeTrue();
        }


        private static void VerifyFileContents(MockFileSystem fileSystem, string path, string expectedContent)
        {
            var actualContent = fileSystem.File.ReadAllText(path);

            actualContent.ShouldBe(expectedContent);
        }


        private static void VerifySymlink(MockFileSystem fileSystem, string linkPath, string expectedTarget)
        {
            // Expand ~ to home directory for MockFileSystem
            var expandedLinkPath = linkPath.Replace("~", HomeDir);
            var expandedExpectedTarget = expectedTarget.Replace("~", HomeDir);

            var target = fileSystem.File.ResolveLinkTarget(expandedLinkPath, returnFinalTarget: false);

            target.ShouldNotBeNull();
            target.FullName.ShouldBe(expandedExpectedTarget);
        }
    }
}
