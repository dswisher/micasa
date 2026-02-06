// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Micasa.Cli.Models
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated by JSON deserializer")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Set by JSON deserializer")]
    public class InstallerDirective
    {
        /// <summary>
        /// The name of the tool used to install the package, which determines the driver to use.
        /// </summary>
        public required string Tool { get; set; }

        /// <summary>
        /// For package managers that use package IDs (like apt, yum, brew), this is the ID of the package to install.
        /// </summary>
        public string? PackageId { get; set; }

        /// <summary>
        /// If the executable name differs from the package ID, it is specified here.
        /// </summary>
        public string? Executable { get; set; }

        /// <summary>
        /// For drivers that download an installer (shell script, .deb file, etc), this is the URL to download from.
        /// </summary>
        public string? InstallerUrl { get; set; }

        /// <summary>
        /// The URL of the github repo for the project, used to fetch latest releases.
        /// </summary>
        public string? RepoUrl { get; set; }

        /// <summary>
        /// For .appimage files, indicates whether FUSE is required to run the AppImage.
        /// </summary>
        /// <remarks>
        /// This will typically trigger unpacking the AppImage (neovim, for example).
        /// </remarks>
        public bool? RequiresFuse { get; set; }
    }
}
