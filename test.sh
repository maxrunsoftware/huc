#!/bin/bash
ip=$1

if [[ -n "$ip" ]]; then
    echo "Using IP: $ip"
else
    echo "No IP argument supplied"
    exit 1
fi

set -x #echo on

rm -rf ./test
mkdir ./test

cp ./publish/osx-x64/huc ./test
cd test

./huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt

# MSSQL
./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT TOP 100 * FROM Orders" Orders100.txt
./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM Orders; SELECT * FROM Employees" Orders.txt Employees.txt
./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="PRINT 'Hello'; PRINT 'World';" 
printf "SELECT TOP 100 *\nFROM Orders" > mssqlscript.sql
./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -f="mssqlscript.sql" OrdersFromScript.txt
./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="if exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'TempOrders' AND TABLE_SCHEMA = 'dbo') DROP TABLE NorthWind.dbo.TempOrders;" 

# MSSQL Load
./huc sqlload -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -drop -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -s=dbo -t=TempOrders Orders.txt
./huc sqlload -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -s=dbo -t=TempOrders Orders.txt
./huc sqlload -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -d=NorthWind -s=dbo -t=TempOrders Orders.txt
./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM TempOrders" TempOrders.txt

# MySQL
./huc sql -st=MySQL -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM products LIMIT 100;" Products100.txt
./huc sql -st=MySQL -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM products; SELECT * FROM invoices;" Products.txt Invoices.txt
#./huc sql -st=MySQL -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="PRINT 'Hello'; PRINT 'World';" 

# MySQL load
./huc sqlload -st=MySQL -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -drop -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -t=TempProducts Products.txt
./huc sqlload -st=MySQL -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -t=TempProducts Products.txt
./huc sqlload -st=MySQL -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -d=NorthWind -t=TempProducts Products.txt
./huc sql -st=MySQL -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM TempProducts;" TempProducts.txt

# Table
cp Orders.txt Orders.csv
./huc table Orders.csv

cp Orders100.txt Orders100.csv
./huc table -hd=pipe -hq=none -he=true -dd=pipe -dq=none -he=false Orders100.csv

./huc ftpput -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpdelete -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpdelete -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpput -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpdelete -h=$ip -u=testuser -p=testpass "testdata?.txt"

./huc ftpput -e=Implicit -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -e=Implicit -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpdelete -e=Implicit -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpdelete -e=Implicit -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpput -e=Implicit -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpdelete -e=Implicit -h=$ip -u=testuser -p=testpass "testdata?.txt"

./huc ftpput -e=Explicit -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -e=Explicit -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpdelete -e=Explicit -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpdelete -e=Explicit -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpput -e=Explicit -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpdelete -e=Explicit -h=$ip -u=testuser -p=testpass "testdata?.txt"

./huc ftpput -e=SSH -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -e=SSH -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpdelete -e=SSH -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpdelete -e=SSH -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpput -e=SSH -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpdelete -e=SSH -h=$ip -u=testuser -p=testpass "testdata?.txt"

./huc zip testdata.zip "testdata?.txt"

