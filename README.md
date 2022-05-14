# HUC: Various Command Line Tools To Make Your Life Easier
## [Max Run Software](https://www.maxrunsoftware.com)

HUC is a simple to use open source command line tool for performing various tasks.
It is a self contained executable built on DotNet 5 and has builds available for Windows, Mac, and Linux

Get list of commands
```
huc
```

Get list of parameters for a command
```
huc Sql help
huc FtpPut help
huc Table help
huc <command> help
```

&nbsp;

## Operations

- [FTP / FTPS / SFTP](#ftp-ftps-sftp)
- [SSH](#ssh)
- [Email](#email)
- [Microsoft SQL / MySQL / Oracle](#sql)
    - [Querying](#sql)
    - [Importing Tab Delimited Data](#sql)
    - [Exporting Binary Data to Files](#sql)
    - [Downloading Files Based on a SQL Query](#sql)
- [ZIP](#zip)
- [Windows Task Scheduler Management](#windows-task-scheduler)
    - [List Tasks](#windows-task-scheduler)
    - [Add Task](#windows-task-scheduler)
    - [Remove Task](#windows-task-scheduler)
- [File Operations](#file-operations)
    - [Tab Delimited File Data Conversion](#file-operations)
    - [Text Replacement](#file-operations)
    - [Appending](#file-operations)
    - [Split](#file-operations)
    - [Generate File Checksums](#file-operations)
    - [Encryption and Decryption](#file-operations)
    - [Generate Random Data](#file-operations)
- [Directory Operations](#directory-operations)
    - [List Directory to Tab Delimited File](#directory-operations)
    - [Get Directory Size](#directory-operations)
    - [Flatten Directory Structure](#directory-operations)
    - [Remove Empty Directories](#directory-operations)
- [Web Server](#web-server)
- [Active Directory](#active-directory)
    - [List Objects](#active-directory)
    - [List Object Details](#active-directory)
    - [List Users / Groups / Computers](#active-directory)
    - [List Users Of Group](#active-directory)
    - [Add User / Group / OU](#active-directory)
    - [Add User To Group](#active-directory)
    - [Remove User / Group / OU](#active-directory)
    - [Remove User From Group](#active-directory)
    - [Move User / Group](#active-directory)
    - [Enable / Disable User](#active-directory)
    - [Change User Password](#active-directory)
- [Google Sheets](#google-sheets)
    - [Query](#google-sheets)
    - [Import Into a Sheet](#google-sheets)
    - [Add Row to Sheet](#google-sheets)
    - [Clear Sheet](#google-sheets)
    - [Format Cells](#google-sheets)
- [VMware](#vmware)
    - [List VMware Objects](#vmware)
    - [List VMware Objects to JSON](#vmware)
    - [Query Raw JSON Data](#vmware)
    - [Perform Operations On VMs](#vmware)
- [Generation of Public and Private Keys](#generate-public-and-private-keys)
- [Using a Properties File](#using-a-properties-file)
- [Helper Utility Functions](#helper-functions)
- [Logging](#logging)

&nbsp;
<details>
  <summary>
      
  ### Email
  </summary>
    
  Send an email
  ```
  huc email -h="smtp.somerelay.org" -from="someone@aol.com" -to="grandma@aol.com" -s="Grandpa Birthday" -b="Tell Grandpa/nHAPPY   BIRTHDAY!"
  ```
  <br/>
  
  Send an email with CC and BCC and attachments
  ```
  huc email -h="smtp.somerelay.org" -to="person1@aol.com;person2@aol.com" -cc="person3@aol.com" -bcc="person4@aol.com" -s="Some subject   text" -b="Some text for body" myAttachedFile1.csv myAttachedFile2.txt
  ```
  <br/>
  
  Send an email with text templating
  ```
  huc email -h="smtp.somerelay.org" -to="person1@aol.com" -t1="Sandy" -t2="some other text" -s="Email for {t1}" -b="Hi {t1},\nHere is   your {t2}"
  ```

</details>

<details>
  <summary>
      
  ### Delimited Files
  </summary>
    
  Convert tab delimited file to csv delimited file using defaults
  ```
  cp Orders.txt Orders.csv
  huc table Orders.csv
  ```
  <br/>
  
  Convert tab delimited file to csv delimited file using specific delimiters and excluding the header row
  ```
  cp Orders.txt Orders.csv
  huc table -hd=pipe -hq=single -he=true -dd=pipe -dq=single -de=false Orders.csv
  ```
  <br/>
  
  Convert tab delimited file to HTML table using defaults
  ```
  cp Orders.txt Orders.html
  huc tablehtml Orders.html
  ```
  <br/>
  
  Convert tab delimited file to HTML table embeddeding a custom CSS file and Javascript file
  ```
  cp Orders.txt Orders.html
  huc tablehtml -css=MyStyleSheet.css -js=MyJavascriptFile.js Orders.html
  ```
  <br/>
  
  Convert tab delimited file to XML
  ```
  cp Orders.txt Orders.xml
  huc tablexml Orders.xml
  ```
  <br/>
  
  Convert tab delimited file to JSON
  ```
  cp Orders.txt Orders.json
  huc tablejson Orders.json
  ```
  <br/>
  
  Convert tab delimited file to fixed width file
  ```
  huc tableFixedWidth Orders.txt 10 20 15 9 6 0 4 200
  ```

</details>

<details>
  <summary>
      
  ### FTP FTPS SFTP
  </summary>
    
  List files in default directory
  ```
  huc ftplist -h=192.168.1.5 -u=testuser -p=testpass
  huc ftplist -e=explicit -h=192.168.1.5 -u=testuser -p=testpass
  huc ftplist -e=implicit -h=192.168.1.5 -u=testuser -p=testpass
  huc ftplist -e=ssh -h=192.168.1.5 -u=testuser -p=testpass
  ```
  <br/>
  
  Recursively list files in /home/user directory
  ```
  huc ftplist -h=192.168.1.5 -u=testuser -p=testpass -r "/home/user"
  huc ftplist -e=explicit -h=192.168.1.5 -u=testuser -p=testpass -r "/home/user"
  huc ftplist -e=implicit -h=192.168.1.5 -u=testuser -p=testpass -r "/home/user"
  huc ftplist -e=ssh -h=192.168.1.5 -u=testuser -p=testpass -r "/home/user"
  ```
  <br/>
  
  Get a file from a FTP/FTPS/SFTP server
  ```
  huc ftpget -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
  huc ftpget -e=explicit -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
  huc ftpget -e=implicit -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
  huc ftpget -e=ssh -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
  ```
  <br/>
  
  Put a file on a FTP/FTPS/SFTP server
  ```
  huc ftpput -h=192.168.1.5 -u=testuser -p=testpass localfile.txt
  huc ftpput -e=explicit -h=192.168.1.5 -u=testuser -p=testpass localfile.txt
  huc ftpput -e=implicit -h=192.168.1.5 -u=testuser -p=testpass localfile.txt
  huc ftpput -e=ssh -h=192.168.1.5 -u=testuser -p=testpass localfile.txt
  ```

</details>

<details>
  <summary>
      
  ### SQL
  </summary>
    
  Query Microsoft SQL server and output tab delimited data file
  ```
  huc sql -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT TOP 100 * FROM Orders" Orders100.txt
  ```
  <br/>
  
  Query Microsoft SQL server and output multiple tab delimited data files from multiple result sets
  ```
  huc sql -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM Orders; SELECT * FROM   Employees" Orders.txt Employees.txt
  ```
  <br/>
  
  Query Microsoft SQL server with SQL script file and output tab delimited data file
  ```
  printf "SELECT TOP 100 *\nFROM Orders" > mssqlscript.sql
  huc sql -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -f="mssqlscript.sql" OrdersFromScript.txt
  ```
  <br/>
  
  Upload tab delimited file into a SQL server table
  ```
  huc sqlload -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -d=NorthWind -s=dbo -t=TempOrders   Orders.txt
  ```
  <br/>
  
  Upload tab delimited file into a SQL server table and include the file row number and a time stamp, dropping the table if it exists already
  ```
  huc sqlload -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -drop -rowNumberColumnName=RowNumber   -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -s=dbo -t=TempOrders Orders.txt
  ```

</details>

<details>
  <summary>
      
  ### Zip
  </summary>
    
  Zipping a file
  ```
  huc zip myOuputFile.zip someLocalFile.txt
  ```
  <br/>
  
  Zipping multiple files
  ```
  huc zip myOuputFile.zip *.txt *.csv
  ```

</details>

<details>
  <summary>
      
  ### Windows Task Scheduler
  </summary>
    
  List all tasks on scheduler
  ```
  huc WindowsTaskSchedulerList -h="localhost" -u="administrator" -p="password" ALL
  ```
  <br/>
  
  List a specific task MyTask on scheduler with details
  ```
  huc WindowsTaskSchedulerList -h="localhost" -u="administrator" -p="password" -d /myTaskFolder/MyTask
  ```
  <br/>
  
  Create a Windows Task Scheduler job to run every day at 4:15am
  ```
  huc WindowsTaskSchedulerAdd -h="localhost" -u="administrator" -p="password" -taskUsername="system" -tw="c:\temp" -t1="DAILY 04:15"   -tn="MyTask" "C:\temp\RunMe.bat"
  ```
  <br/>
  
  Create a Windows Task Scheduler job to run every hour at 35 minutes after the hour
  ```
  huc WindowsTaskSchedulerAdd -h="localhost" -u="administrator" -p="password" -taskUsername="system" -tw="c:\temp" -t1="HOURLY 35"   -tn="MyTask" "C:\temp\RunMe.bat"
  ```
  <br/>
  
  Create a Windows Task Scheduler job to run Monday and Wednesday at 7:12pm 
  ```
  huc WindowsTaskSchedulerAdd -h="localhost" -u="administrator" -p="password" -taskUsername="system" -tw="c:\temp" -t1="MONDAY 19:12"   -t2="WEDNESDAY 19:12" -tn="MyTask" "C:\temp\RunMe.bat"
  ```
  <br/>
  
  Delete a Windows Task Scheduler job
  ```
  huc WindowsTaskSchedulerRemove -h="localhost" -u="administrator" -p="password" MyTask
  ```

</details>

<details>
  <summary>
      
  ### File Operations
  </summary>
    
  Replace all instances of Person with Steve in the file mydoc.txt
  ```
  huc FileReplaceString "Person" "Steve" mydoc.txt
  ```
  <br/>
    
  Append files file1.txt and file2.txt to mainfile.txt
  ```
  huc FileAppend mainfile.txt file1.txt file2.txt
  ```
  <br/>
    
  Split a file on the new line character into 3 other files
  ```
  huc FileSplit Orders.txt Orders1.txt Orders2.txt Orders3.txt
  ```
  <br/>
    
  Generate MD5 checksum for file MyFile.zip
  ```
  huc FileChecksum MyFile.zip
  ```
  <br/>
  
  Generate SHA512 checksum for files *.txt
  ```
  huc FileChecksum -t=SHA512 *.txt
  ```
  <br/>
  
  Encrypt file with password
  ```
  huc FileEncrypt -p=password data.txt data.encrypted
  ```
  <br/>
  
  Decrypt file with password
  ```
  huc FileDecrypt -p=password data.encrypted dataDecrypted.txt
  ```
  <br/>
  
  Encrypt file with public key
  ```
  huc FileEncrypt -pk=MyPublicKey.txt data.txt data.encrypted
  ```
  <br/>
  
  Decrypt file with private key
  ```
  huc FileDecrypt -pk=MyPrivateKey.txt data.encrypted dataDecrypted.txt
  ```
  <br/>
    
  Generate file with random data
  ```
  huc GenerateRandomFile testdata.txt
  huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt
  ```

</details>

<details>
  <summary>
      
  ### Directory Operations
  </summary>
    
  List some file and directory information to a tab delimited file
  ```
  huc DirectoryList -creationTime -lastAccessTimeUtc -lastWriteTime -n -nf -p -s -recursiveDepth=10 -pattern=*.cs C:\temp\MyDirectory   mydatafile.txt 
  ```
  <br/>
  
  Get the size of a directory
  ```
  huc DirectorySize C:\temp\MyDirectory 
  ```
  <br/>
  
  Move all files in all subdirectories of target directory into the target directory, but don't overwrite if the file already exists
  ```
  huc DirectoryFlatten C:\temp\MyDirectory
  ```
  <br/>
  
  Move all files in all subdirectories of target directory into the target directory, and keep the newest file
  ```
  huc DirectoryFlatten -c=KeepNewest C:\temp\MyDirectory
  ```
  <br/>
  
  Deletes empty subdirectories recursively
  ```
  huc DirectoryRemoveEmpty C:\temp\MyDirectory
  ```

</details>

<details>
  <summary>
      
  ### Web Server
  </summary>
    
  Start webserver and host files out of the current directory
  ```
  huc WebServer .
  ```
  <br/>
  
  Start webserver on port 80 and host files out of c:\www directory
  ```
  huc WebServer -o=80 c:\www
  ```
  <br/>
  
  Start webserver on port 80 and host files out of c:\www directory and require a username and password
  ```
  huc WebServer -o=80 -u=user -p=testpass c:\www
  ```
  <br/>

</details>

<details>
  <summary>
      
  ### SSH
  </summary>
    
  Issue LS command
  ```
  huc SSH -h=192.168.1.5 -u=testuser -p=testpass "ls"
  ```
  <br/>
  
  Change directory and issue LS command with options
  ```
  huc SSH -h=192.168.1.5 -u=testuser -p=testpass "cd someDirectory; ls -la;"
  ```
  <br/>

</details>

<details>
  <summary>
      
  ### Active Directory
  </summary>
    
  List all objects and their attributes to a tab delimited file
  ```
  huc ActiveDirectoryList -h=192.168.1.5 -u=administrator -p=testpass adlist.txt
  ```
  <br/>
  
  List various object types
  ```
  huc ActiveDirectoryListObjects -h=192.168.1.5 -u=administrator -p=testpass ?teve*
  huc ActiveDirectoryListUsers -h=192.168.1.5 -u=administrator -p=testpass
  huc ActiveDirectoryListGroups -h=192.168.1.5 -u=administrator -p=testpass Group*
  huc ActiveDirectoryListComputers -h=192.168.1.5 -u=administrator -p=testpass
  ```
  <br/>
  
  List various object types and display specific LDAP fields
  ```
  huc ActiveDirectoryListObjects -h=192.168.1.5 -u=administrator -p=testpass -pi=*Name
  huc ActiveDirectoryListUsers -h=192.168.1.5 -u=administrator -p=testpass   -pi=DistinguishedName,OganizationalUnit,ObjectName,ObjectGuid ?teve*
  huc ActiveDirectoryListGroups -h=192.168.1.5 -u=administrator -p=testpass -pi=*Name,Object*
  huc ActiveDirectoryListComputers -h=192.168.1.5 -u=administrator -p=testpass -pi=* MyComputer?
  ```
  <br/>
  
  List additional details for an Active Directory object
  ```
  huc ActiveDirectoryListObjectDetails -h=192.168.1.5 -u=administrator -p=testpass Administrator
  huc ActiveDirectoryListObjectDetails -h=192.168.1.5 -u=administrator -p=testpass Users
  huc ActiveDirectoryListObjectDetails -h=192.168.1.5 -u=administrator -p=testpass ?teve*
  ```
  <br/>
  
  Change a user's password (note: requires LDAPS certificate to be installed on AD server or running HUC on the AD server itself)
  ```
  huc ActiveDirectoryChangePassword -h=192.168.1.5 -u=administrator -p=testpass testuser newpassword
  ```
  <br/>
  
  Add User
  ```
  huc ActiveDirectoryAddUser -h=192.168.1.5 -u=administrator -p=testpass testuser
  huc ActiveDirectoryAddUser -h=192.168.1.5 -u=administrator -p=testpass -firstname="steve" -lastname="foster" testuser
  ```
  <br/>
  
  Add Group
  ```
  huc ActiveDirectoryAddGroup -h=192.168.1.5 -u=administrator -p=testpass testgroup
  huc ActiveDirectoryAddGroup -h=192.168.1.5 -u=administrator -p=testpass -gt=GlobalSecurityGroup testgroup
  ```
  <br/>
  
  Delete User
  ```
  huc ActiveDirectoryRemoveUser -h=192.168.1.5 -u=administrator -p=testpass testuser
  ```
  <br/>
  
  Delete Group
  ```
  huc ActiveDirectoryRemoveGroup -h=192.168.1.5 -u=administrator -p=testpass testgroup
  ```
  <br/>
  
  Move User
  ```
  huc ActiveDirectoryMoveUser -h=192.168.1.5 -u=administrator -p=testpass testuser MyNewOU
  ```
  <br/>
  
  Move Group
  ```
  huc ActiveDirectoryMoveGroup -h=192.168.1.5 -u=administrator -p=testpass testgroup MyNewOU
  ```
  <br/>
  
  Add user to group
  ```
  huc ActiveDirectoryAddUserToGroup -h=192.168.1.5 -u=administrator -p=testpass testuser MyGroup1 SomeOtherGroup
  ```
  <br/>
  
  Remove user from group
  ```
  huc ActiveDirectoryRemoveUserFromGroup -h=192.168.1.5 -u=administrator -p=testpass testuser MyGroup1
  ```
  <br/>
  
  Enable user
  ```
  huc ActiveDirectoryEnableUser -h=192.168.1.5 -u=administrator -p=testpass testuser
  ```
  <br/>
  
  Disable user
  ```
  huc ActiveDirectoryDisableUser -h=192.168.1.5 -u=administrator -p=testpass testuser
  ```
  <br/>
  
  Disable users who have not logged on in the past 7 days
  ```
  huc ActiveDirectoryDisableUsers -h=192.168.1.5 -u=administrator -p=testpass -l=7
  ```

</details>

<details>
  <summary>
      
  ### Google Sheets
  </summary>
    
  For setting up the Google account see...\
  https://medium.com/@williamchislett/writing-to-google-sheets-api-using-net-and-a-services-account-91ee7e4a291 \
  \
  Clear all data from a Google Sheet tab named Sheet1 (sheet ID is in the URL)
  ```
  huc GoogleSheetsClear -k="MyGoogleAppKey.json" -a="MyApplicationName" -id="dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe" -s="Sheet1"
  ```
  <br/>
  
  Clear all data from the first Google Sheet tab
  ```
  huc GoogleSheetsClear -k="MyGoogleAppKey.json" -a="MyApplicationName" -id="dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe"
  ```
  <br/>
  
  Clear the first sheet tab and upload Orders.txt tab delimited file to it
  ```
  huc GoogleSheetsLoad -k="MyGoogleAppKey.json" -a="MyApplicationName" -id="dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe" Orders.txt
  ```
  <br/>
  
  Add a row to first sheet with the values "AA", blank, "CC"
  ```
  huc GoogleSheetsAddRow -k="MyGoogleAppKey.json" -a="MyApplicationName" -id="dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe" AA null CC
  ```
  <br/>
  
  Make the first row of data have red text, blue background, and bold
  ```
  huc GoogleSheetsFormatCells -k="MyGoogleAppKey.json" -a="MyApplicationName" -id="dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe" -width=100 -b   -fc=Red -bc=Blue 
  ```
  <br/>
  
  Query all data from first sheet and output it to a tab delimited file MyFile.txt
  ```
  huc GoogleSheetsQuery -k="MyGoogleAppKey.json" -a="MyApplicationName" -id="dkjfsd328sdfuhscbjcds8hfjndsfdsfdsfe" MyFile.txt
  ```

</details>

<details>
  <summary>
      
  ### Generate Public and Private Keys
  </summary>
    
  Generate RSA public and private key files
  ```
  huc GenerateKeyPair MyPublicKey.txt MyPrivateKey.txt
  ```
  <br/>
  
  Generate RSA public and private key files with RSA length 4096
  ```
  huc GenerateKeyPair -l=4096 MyPublicKey.txt MyPrivateKey.txt
  ```

</details>

<details>
  <summary>
      
  ### VMware
  </summary>
    
  Query various information in a VCenter 6.7+ infrastructure
  ```
  huc VMwareList -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass DataCenter VM StoragePolicy
  huc VMwareList -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass VM_Quick
  huc VMwareList -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass VM_WithoutTools
  huc VMwareList -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass VM_PoweredOff
  huc VMwareList -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass VM_IsoAttached
  ```
  <br/>
  
  Query raw JSON data from VCenter 6.7+ infrastructure
  ```
  huc VMwareQuery -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass  /rest/vcenter/host
  huc VMwareQuery -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass  /rest/vcenter/vm
  huc VMwareQuery -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass  /rest/vcenter/vm/vm-1692
  ```
  <br/>
  
  Query all infrastructure data to a JSON file
  ```
  huc VMwareQueryJSON -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyDataFile.json
  ```
  <br/>
  
  Perform various actions on a VM
  ```
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM None
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM Shutdown
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM Reboot
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM Standby
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM Reset
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM Start
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM Stop
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM Suspend
  huc VMwareVM -h=192.168.1.5 -u=testuser@vsphere.local -p=mypass MyVM DetachISOs
  ```

</details>

<details>
  <summary>
      
  ### Putting It All Together
  </summary>
    
  Query SQL server, convert the data, sftp it, zip it, then email the data
  ```
  huc sql -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM Orders" orders.csv
  huc table -hd=comma -hq=none -dd=comma -dq=none orders.csv
  huc ftpput -e=ssh -h=192.168.1.5 -u=testuser -p=testpass orders.csv
  huc zip orders.zip "*.csv"
  huc email -h="smtp.somerelay.org" -from="me@aol.com" -to="person@aol.com" -s="Orders data" -b="Attached is the order data" "*.zip"
  ```

</details>

<details>
  <summary>
      
  ### Using a Properties File
  </summary>
    
  When huc first runs, it attempts to generate a huc.properties file in the directory of the executable. This file contains all of the parameters for each command. You can populate this file with certain properties so you don't have to type them in every time. The huc program will first check if a parameter was supplied at the command line. If not, if will then check the properties file (commandline overrides properties file). If still not found it will attempt to use a default value for some parameters (not all, some are required to be provided).

  So assuming a properties file of...
  ```properties
  sql.connectionString=Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;
  table.headerDelimiter=comma
  table.headerQuoting=none
  table.dataDelimiter=comma
  table.dataQuoting=none
  ftpput.host=192.168.1.5
  ftpput.encryptionMode=SSH
  ftpput.username=testuser
  ftpput.password=testpass
  email.host=smtp.somerelay.org
  email.from=me@aol.com
  ```
  The commands now become...
  ```
  huc sql -s="SELECT * FROM Orders" orders.csv
  huc table orders.csv
  huc ftpput orders.csv
  huc zip orders.zip "*.csv"
  huc email -to="person@aol.com" -s="Orders data" -b="Attached is the order data" "*.zip"
  ```

</details>

<details>
  <summary>
      
  ### Helper Functions
  </summary>
  
  Show current properties set in the properties file
  ```
  huc ShowProperties
  ```
  <br/>
  
  Show all available properties
  ```
  huc ShowProperties -a
  ```
  <br/>
  
  Convert Binary file to Base16
  ```
  huc ConvertBinaryToBase16 myinputfile.txt myoutputfile.txt
  ```
  <br/>
  
  Convert Binary file to Base64
  ```
  huc ConvertBinaryToBase64 myinputfile.txt myoutputfile.txt
  ```
  <br/>
  
  Get a web file
  ```
  huc wget https://github.com/Steven-D-Foster/huc/releases/download/v1.3.0/huc-linux.zip
  ```
  <br/>
  
  Get a web page
  ```
  huc wget https://github.com github.txt
  ```
  <br/>
  
  Show internet time
  ```
  huc time
  ```
  <br/>
  
  Show drift of local clock compared to internet time
  ```
  huc time -d
  ```
  <br/>
  
  Show all of the colors available for commands that take a color parameter
  ```
  huc colors
  ```
  <br/>
  
  Show details for a specific color
  ```
  huc colors red
  ```
  <br/>
  
  Test JSAS service
  ```
  huc jsas https://192.168.0.10 MyPassword MyData
  huc jsas https://192.168.0.10 MyPassword MyData NewFile.txt
  ```
  <br/>
  
  Encrypt Password to use in huc.properties file
  ```
  huc EncodePassword mySecretPassword
  ```

</details>

<details>
  <summary>
      
  ### Logging
  </summary>
    
  HUC supports various logging. At the console level HUC supports ```INFO```, ```DEBUG```, and ```TRACE``` logging levels. By default the logging level is ```INFO```. To enable ```DEBUG``` level logging at the console, specify the ```-debug``` parameter at the command line. To enable ```TRACE``` level logging, specify the ```-trace``` parameter at the command line.

  HUC also supports logging to a file. To enable file logging, use the parameters ```Log.FileLevel``` and ```Log.FileName``` in the ```huc.properties``` file to specify the log level (```CRITICAL```, ```ERROR```, ```WARN```, ```INFO```, ```DEBUG```, ```TRACE```) and the filename of the file to write out to.

</details>
