﻿//-----------------------------------------------------------------------
// <copyright file="BuildWrapperInstaller.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarQube.Common;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SonarQube.TeamBuild.PreProcessor
{
    public class BuildWrapperInstaller : IBuildWrapperInstaller
    {
        public const string CppPluginKey = "cpp";
        public const string BuildWrapperStaticResourceName = "build-wrapper-win-x86.zip";

        public const string BuildWrapperOutputPropertyKey = "sonar.cfamily.build-wrapper-output";

        private readonly ILogger logger;

        public BuildWrapperInstaller(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.logger = logger;
        }

        #region IBuildWrapperInstaller methods

        public AnalysisProperties InstallBuildWrapper(ISonarQubeServer server, string binDirectory, string outputDirectory)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }
            if (string.IsNullOrWhiteSpace(binDirectory))
            {
                throw new ArgumentNullException("binDirectory");
            }

            AnalysisProperties properties = new AnalysisProperties();

            if (IsCppPluginInstalled(server) &&
                FetchResourceFromServer(server, binDirectory))
            {
                string bwOutputDir = Path.Combine(outputDirectory, "bw");
                properties.Add(new Property() { Id = BuildWrapperOutputPropertyKey, Value = bwOutputDir });
            }
            else
            {
                this.logger.LogInfo(Resources.BW_CppPluginNotInstalled);
            }

            return properties;
        }

        #endregion

        #region Private methods

        private static bool IsCppPluginInstalled(ISonarQubeServer server)
        {
            return server.GetInstalledPlugins().Contains(CppPluginKey);
        }

        private bool FetchResourceFromServer(ISonarQubeServer server, string targetDir)
        {
            this.logger.LogDebug(Resources.BW_DownloadingBuildWrapper);

            Directory.CreateDirectory(targetDir);

            bool success = server.TryDownloadEmbeddedFile(CppPluginKey, BuildWrapperStaticResourceName, targetDir);

            if (success)
            {
                string targetFilePath = Path.Combine(targetDir, BuildWrapperStaticResourceName);

                if (IsZipFile(targetFilePath))
                {
                    this.logger.LogDebug(Resources.MSG_ExtractingFiles, targetDir);
                    ZipFile.ExtractToDirectory(targetFilePath, targetDir);
                }
            }
            else
            {
                // We assume that the absence of the embedded zip means that an old
                // version of the Cpp plugin is installed
                this.logger.LogWarning(Resources.BW_CppPluginUpgradeRequired);
            }
            return success;
        }

        private static bool IsZipFile(string fileName)
        {
            return string.Equals(".zip", Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}