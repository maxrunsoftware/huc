# HUC: Various utilities to make your life easier
HUC is a simple to use command line tool for performing various tasks including...
- FTP
- FTPS
- SFTP
- Delimited data conversion
- SQL querying
- ZIP

## Examples:
Generate file with random data
```sh
./huc GenerateRandomFile testdata.txt
./huc GenerateRandomFile -l=1000000 testdata2.txt
```

Query MSSQL server and output tab delimited data file
```sh
./huc sql -c="Server=$192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT TOP 100 * FROM Orders" Orders.txt
./huc sql -c="Server=$192.168.1.5;Database=NorthWind;User Id=testuser;Password=testpass;" -f="mssqlscript.sql" Orders2.txt
```



