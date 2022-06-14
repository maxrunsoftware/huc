// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
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

using System;
using MaxRunSoftware.Utilities.External;

namespace MaxRunSoftware.Utilities.Console.Commands;

public abstract class GoogleSheetsBase : Command
{
    protected override void CreateHelp(CommandHelpBuilder help)
    {
        help.AddDetail("See the following to setup the account to be able to interact with Google Sheets...");
        // ReSharper disable StringLiteralTypo
        help.AddDetail("https://medium.com/@williamchislett/writing-to-google-sheets-api-using-net-and-a-services-account-91ee7e4a291");
        // ReSharper restore StringLiteralTypo
        help.AddParameter(nameof(securityKeyFile), "k", "The JSON formatted security key file");
        help.AddParameter(nameof(applicationName), "a", "The Google Developer application name");
        help.AddParameter(nameof(spreadsheetId), "id", "The ID of the spreadsheet to upload to which is in the URL of the document");
    }

    private string securityKeyFile;
    private string securityKeyFileData;
    private string applicationName;
    private string spreadsheetId;

    protected override void ExecuteInternal()
    {
        securityKeyFile = GetArgParameterOrConfigRequired(nameof(securityKeyFile), "k");
        securityKeyFileData = ReadFile(securityKeyFile);
        log.Debug(nameof(securityKeyFileData) + ": " + securityKeyFileData);

        applicationName = GetArgParameterOrConfigRequired(nameof(applicationName), "a");
        spreadsheetId = GetArgParameterOrConfigRequired(nameof(spreadsheetId), "id");
    }

    protected GoogleSheets CreateConnection()
    {
        if (spreadsheetId == null) throw new Exception("base.Execute() never called for class " + GetType().FullNameFormatted());

        log.Debug("Opening Google Sheets connection");
        return new GoogleSheets(securityKeyFileData, applicationName, spreadsheetId);
    }
}
