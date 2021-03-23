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

./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT TOP 100 * FROM Orders" Orders100.txt
./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -s="SELECT * FROM Orders; SELECT * FROM Employees" Orders.txt Employees.txt
printf "SELECT TOP 100 *\nFROM Orders" > mssqlscript.sql
./huc sql -c="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;" -f="mssqlscript.sql" OrdersFromScript.txt

cp Orders.txt Orders.csv
./huc table Orders.csv

cp Orders100.txt Orders100.csv
./huc table -hd=pipe -hq=none -he=true -dd=pipe -dq=none -he=false Orders100.csv


exit 1

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

