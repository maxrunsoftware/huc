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

namespace MaxRunSoftware.Utilities.Console.Commands;

public class WebServerUtilityGenerateRandomFile : WebServerUtilityBase
{
    public string Handle()
    {
        var generateRandom = new WebServerUtilityGenerateRandom();
        var randomString = (string)generateRandom.Handle(Context);
        Context.SendFile(randomString, "random.txt");
        return "Generated Random File";
    }

    public override string HandleHtml() => Handle();
}
