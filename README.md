# HUC: Various command line tools to make your life easier
HUC is a simple to use open source command line tool for performing various tasks including...
- FTP
- FTPS
- SFTP
- Email
- Delimited data conversion
- MSSQL/MySQL/Oracle querying
- MSSQL/MySQL importing data into a table
- ZIP
- Windows Task Scheduler Management
- File String Replacement
- WebServer

HUC is a self contained executable built on DotNet 5 and has builds available for Windows, Mac, and Linux

## Examples:
Get list of commands
```sh
huc
```

Get list of parameters for a command
```sh
huc Sql
huc FtpPut
huc Table
huc <command>
```
&nbsp;
### Email
Send an email
```sh
huc email -h="smtp.somerelay.org" -from="someone@aol.com" -to="grandma@aol.com" -s="Grandpa Birthday" -b="Tell Grandpa/nHAPPY BIRTHDAY!"
huc email -h="smtp.somerelay.org" -to="person1@aol.com;person2@aol.com" -cc="person3@aol.com" -bcc="person4@aol.com" -s="Some subject text" -b="Some text for body" myAttachedFile1.csv myAttachedFile2.txt
```
&nbsp;
### SQL
Query Microsoft SQL server and output tab delimited data file
```sh
huc sql -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT TOP 100 * FROM Orders" Orders100.txt
```

Query Microsoft SQL server and output multiple tab delimited data files from multiple result sets
```sh
huc sql -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM Orders; SELECT * FROM Employees" Orders.txt Employees.txt
```

Query Microsoft SQL server with SQL script file and output tab delimited data file
```sh
printf "SELECT TOP 100 *\nFROM Orders" > mssqlscript.sql
huc sql -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -f="mssqlscript.sql" OrdersFromScript.txt
```

Upload tab delimited file into a SQL server table
```sh
./huc sqlload -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -d=NorthWind -s=dbo -t=TempOrders Orders.txt
```

Upload tab delimited file into a SQL server table and include the file row number and a time stamp, dropping the table if it exists already
```sh
./huc sqlload -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -drop -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -s=dbo -t=TempOrders Orders.txt
```
&nbsp;
### Delimited Files
Convert tab delimited file to csv delimited file using defaults
```sh
cp Orders.txt Orders.csv
huc table Orders.csv
```

Convert tab delimited file to csv delimited file using specific delimiters and excluding the header row
```sh
cp Orders.txt Orders.csv
huc table -hd=pipe -hq=single -he=true -dd=pipe -dq=single -de=false Orders.csv
```
&nbsp;
### FTP/FTPS/SFTP
Get a file from a FTP/FTPS/SFTP server
```sh
huc ftpget -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
huc ftpget -e=explicit -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
huc ftpget -e=implicit -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
huc ftpget -e=ssh -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
```

Put a file on a FTP/FTPS/SFTP server
```sh
huc ftpput -h=192.168.1.5 -u=testuser -p=testpass localfile.txt
huc ftpput -e=explicit -h=192.168.1.5 -u=testuser -p=testpass localfile.txt
huc ftpput -e=implicit -h=192.168.1.5 -u=testuser -p=testpass localfile.txt
huc ftpput -e=ssh -h=192.168.1.5 -u=testuser -p=testpass localfile.txt
```
&nbsp;
### Zip
Zipping a file
```sh
huc zip myOuputFile.zip someLocalFile.txt
```

Zipping multiple files
```sh
huc zip myOuputFile.zip *.txt *.csv
```
&nbsp;
### Windows Task Scheduler
List all tasks on scheduler
```sh
huc WindowsTaskSchedulerList -h="localhost" -u="administrator" -p="password" ALL
```

List a specific task MyTask on scheduler with details
```sh
huc WindowsTaskSchedulerList -h="localhost" -u="administrator" -p="password" -d /myTaskFolder/MyTask
```

Create a Windows Task Scheduler job to run every day at 4:15am
```sh
huc WindowsTaskSchedulerCreate -h="localhost" -u="administrator" -p="password" -taskUsername="system" -tw="c:\temp" -t1="DAILY 04:15" -tn="MyTask" "C:\temp\RunMe.bat"
```

Create a Windows Task Scheduler job to run every hour at 35 minutes after the hour
```sh
huc WindowsTaskSchedulerCreate -h="localhost" -u="administrator" -p="password" -taskUsername="system" -tw="c:\temp" -t1="HOURLY 35" -tn="MyTask" "C:\temp\RunMe.bat"
```

Create a Windows Task Scheduler job to run Monday and Wednesday at 7:12pm 
```sh
huc WindowsTaskSchedulerCreate -h="localhost" -u="administrator" -p="password" -taskUsername="system" -tw="c:\temp" -t1="MONDAY 19:12" -t2="WEDNESDAY 19:12" -tn="MyTask" "C:\temp\RunMe.bat"
```

Delete a Windows Task Scheduler job
```sh
huc WindowsTaskSchedulerDelete -h="localhost" -u="administrator" -p="password" MyTask
```
&nbsp;
### File Replacement
Replace all instances of Person with Steve in the file mydoc.txt
```sh
huc FileReplaceSting "Person" "Steve" mydoc.txt
```
&nbsp;
### Web Server
Start webserver and host files out of the current directory
```sh
huc WebServer .
```

Start webserver on port 80 and host files out of c:\www directory
```sh
huc WebServer -o=80 c:\www
```
&nbsp;
&nbsp;
## Putting it all together
Query SQL server, convert the data, sftp it, zip it, then email the data
```sh
huc sql -c="Server=192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM Orders" orders.csv
huc table -hd=comma -hq=none -dd=comma -dq=none orders.csv
huc ftpput -e=ssh -h=192.168.1.5 -u=testuser -p=testpass orders.csv
huc zip orders.zip "*.csv"
huc email -h="smtp.somerelay.org" -from="me@aol.com" -to="person@aol.com" -s="Orders data" -b="Attached is the order data" "*.zip"
```
&nbsp;
## Using a properties file
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
```sh
huc sql -s="SELECT * FROM Orders" orders.csv
huc table orders.csv
huc ftpput orders.csv
huc zip orders.zip "*.csv"
huc email -to="person@aol.com" -s="Orders data" -b="Attached is the order data" "*.zip"
```
&nbsp;
## Helper functions
Generate file with random data
```sh
huc GenerateRandomFile testdata.txt
huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt
```

Show current properties set in the properties file
```sh
huc ShowProperties CURRENT
```

Show all available properties
```sh
huc ShowProperties CURRENT -a
```


