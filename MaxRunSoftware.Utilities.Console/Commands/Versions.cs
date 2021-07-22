/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MaxRunSoftware.Utilities.External;
using Octokit;

namespace MaxRunSoftware.Utilities.Console.Commands
{
    public class Versions : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Display versions of HUC available for download and optionally downloads them");
            help.AddParameter(nameof(download), "d", "Download latest version");
            //help.AddValue("<source URL> <output file>");
            help.AddExample("");
            help.AddExample("-d");
        }

        private bool download;

        protected override void ExecuteInternal()
        {
            download = GetArgParameterOrConfigBool(nameof(download), "d", false);

            var github = new GitHub("Steven-D-Foster", "huc");
            var releases = github.Releases;
            foreach (var release in releases)
            {
                log.Info(FormatRelease(release));
            }

            if (download && releases.Count > 0)
            {
                DownloadLatestRelease(releases[0]);
            }



        }

        private void DownloadLatestRelease(Release release)
        {
            var hucFileName = "huc-";
            if (Constant.OS_MAC) hucFileName += "osx";
            else if (Constant.OS_UNIX) hucFileName += "linux";
            else if (Constant.OS_WINDOWS) hucFileName += "win";
            hucFileName += ".zip";

            string url = null;
            foreach (var asset in release.Assets)
            {
                if (asset.Name == null) continue;
                if (!asset.Name.EqualsCaseInsensitive(hucFileName)) continue;
                url = asset.BrowserDownloadUrl;
            }
            if (url == null)
            {
                throw new Exception("Could not find asset " + hucFileName + " for release " + release.TagName);
            }

            var sourceURL = url;
            sourceURL.CheckValueNotNull(nameof(sourceURL), log);


            var outputFile = sourceURL.Split('/').TrimOrNull().WhereNotNull().LastOrDefault();
            outputFile = Util.FilenameSanitize(outputFile, "_");
            outputFile = Path.Combine(Environment.CurrentDirectory, outputFile);
            outputFile = Path.GetFullPath(outputFile);
            log.DebugParameter(nameof(outputFile), outputFile);
            DeleteExistingFile(outputFile);

            using (var cli = new WebClient())
            {
                log.Info("Downloading: " + sourceURL);
                cli.DownloadFile(sourceURL, outputFile);
                log.Info("Download complete: " + outputFile);
            }

        }

        private string FormatRelease(Release release)
        {
            var sb = new StringBuilder();
            if (("v" + Version.Value).EqualsCaseInsensitive(release.TagName))
            {
                sb.Append("* ");
            }
            else
            {
                sb.Append("  ");
            }
            sb.Append(release.TagName.PadRight(10));
            var publishedAt = release.PublishedAt;
            if (publishedAt == null)
            {
                sb.Append("".PadRight("yyyy-MM-dd HH:mm:ss".Length));
            }
            else
            {
                var localDateTime = publishedAt.Value.LocalDateTime;
                var dtString = localDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                sb.Append(dtString);
            }
            sb.Append("  ");
            var body = release.Body.TrimOrNull();
            if (body != null)
            {
                if (body.StartsWith(release.TagName)) body = body.Substring(release.TagName.Length).TrimOrNull();
                if (body != null)
                {
                    if (body.StartsWith("-")) body = body.Substring("-".Length).TrimOrNull();
                    if (body != null)
                    {
                        sb.Append(body);
                    }
                }
            }
            return sb.ToString();
        }

    }
}
