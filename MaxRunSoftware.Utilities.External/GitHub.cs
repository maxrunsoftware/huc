/*
Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)

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

using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace MaxRunSoftware.Utilities.External;

public class GitHub
{
    private readonly string username;
    private readonly string repositoryName;
    public GitHub(string username, string repositoryName)
    {
        this.username = username.CheckNotNullTrimmed(nameof(username));
        this.repositoryName = repositoryName.CheckNotNullTrimmed(nameof(repositoryName));
    }

    public IReadOnlyList<Release> Releases
    {
        get
        {
            // https://api.github.com/repos/maxrunsoftware/huc/releases
            var releasesTask = Task.Run(async () => await GetReleasesAsync());
            var releases = releasesTask.Result;
            return releases;
        }
    }

    private async Task<IReadOnlyList<Release>> GetReleasesAsync()
    {
        var client = new GitHubClient(new ProductHeaderValue(username));

        var result = await client.Repository.Release.GetAll(username, repositoryName);
        return result;
    }
}