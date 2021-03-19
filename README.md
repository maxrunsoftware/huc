# HUC: Various utilities to make your life easier
HUC is a simple to use command line tool for performing various tasks including...
- FTP
- FTPS
- SFTP
- Delimited data conversion
- SQL querying
- ZIP

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

Generate file with random data
```sh
huc GenerateRandomFile testdata.txt
huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt
```

Send an email
```sh
huc email -h="smtp.somerelay.org" -from="someone@aol.com" -to="grandma@aol.com" -s="Grandpa Birthday" -b="Tell Grandpa/nHAPPY BIRTHDAY!"
huc email -h="smtp.somerelay.org" -to="person1@aol.com;person2@aol.com" -cc="person3@aol.com" -bcc="person4@aol.com" -s="Some subject text" -b="Some text for body" myAttachedFile1.csv myAttachedFile2.txt
```

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

Convert tab delimited file to csv delimited file using defaults
```sh
cp Orders.txt Orders.csv
huc table Orders.csv
```

Convert tab delimited file to csv delimited file using specific delimiters and excluding the header row
```sh
cp Orders.txt Orders.csv
huc table -hd=pipe -hq=single -he=true -dd=pipe -dq=single -he=false Orders.csv
```

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
huc ftpget -e=explicit -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
huc ftpget -e=implicit -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
huc ftpget -e=ssh -h=192.168.1.5 -u=testuser -p=testpass remotefile.txt
```






