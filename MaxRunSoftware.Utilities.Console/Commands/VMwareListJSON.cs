﻿// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using MaxRunSoftware.Utilities.External;
using Newtonsoft.Json;

namespace MaxRunSoftware.Utilities.Console.Commands;

public class VMwareListJSON : VMwareBase
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        base.CreateHelp(help);
        help.AddSummary("Lists all details available for a VMware VCenter environment to a JSON file");
        help.AddValue("<JSON output file>");
        help.AddExample(HelpExamplePrefix + " MyVMwareStuff.json");
    }

    protected override void ExecuteInternal(VMwareClient vmware)
    {
        var outputFile = GetArgValueTrimmed(0);
        outputFile.CheckValueNotNull(nameof(outputFile), log);

        DeleteExistingFile(outputFile);

        var data = JsonConvert.SerializeObject(vmware, Formatting.Indented);

        Util.FileWrite(outputFile, data, Constant.ENCODING_UTF8);
    }
}
