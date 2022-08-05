#!/bin/bash
ip=$1

if [[ -n "$ip" ]]; then
    echo "Using IP: $ip"
else
    echo "No IP argument supplied"
    exit 1
fi

set -x # echo on
set -e # exit on error

mssql="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;"
mysql="Server=$ip;Database=NorthWind;User Id=testuser;Password=testpass;"


rm -rf ./test
mkdir ./test

cp ./publish/osx-x64/huc ./test
cd test

#clear


echo --- SQL MSSQL ---
./huc sql -c="$mssql" -s="SELECT TOP 100 * FROM Orders" Orders100.txt
./huc sql -c="$mssql" -s="SELECT * FROM Orders; SELECT * FROM Employees" Orders.txt Employees.txt
./huc sql -c="$mssql" -s="PRINT 'Hello'; PRINT 'World';" 
printf "SELECT TOP 100 *\nFROM Orders" > mssqlscript.sql
./huc sql -c="$mssql" -f="mssqlscript.sql" OrdersFromScript.txt
./huc sql -c="$mssql" -s="if exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'TempOrders' AND TABLE_SCHEMA = 'dbo') DROP TABLE NorthWind.dbo.TempOrders;" 

echo --- SQLLOAD MSSQL ---
./huc sqlload -c="$mssql" -drop -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -s=dbo -t=TempOrders -dct -cv Orders.txt
./huc sqlload -c="$mssql" -drop -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -s=dbo -t=TempOrders Orders.txt
./huc sqlload -c="$mssql" -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -s=dbo -t=TempOrders Orders.txt
./huc sqlload -c="$mssql" -d=NorthWind -s=dbo -t=TempOrders Orders.txt
./huc sql -c="$mssql" -s="SELECT * FROM TempOrders" TempOrders.txt

printf "BITS,0,1,t,F,true,FaLSe,y,N,yEs,no \n" > mssqldct.txt
printf "TINYINTS,0,1,255,0,0,0,0,0,0,0 \n" >> mssqldct.txt
printf "SMALLINTS,-32768,-1,0,1,32767,0,0,0,0,0 \n" >> mssqldct.txt
printf "INTS,-2147483648,-1,0,1,2147483647,0,0,0,0,0 \n" >> mssqldct.txt
./huc FileReplaceString "," "\\t" mssqldct.txt
./huc TableTranspose mssqldct.txt
./huc sqlload -c="$mssql" -drop -d=NorthWind -dct -cv mssqldct.txt



echo --- SQL MySQL ---
./huc sql -st=MySQL -c="$mysql" -s="SELECT * FROM products LIMIT 100;" Products100.txt
./huc sql -st=MySQL -c="$mysql" -s="SELECT * FROM products; SELECT * FROM invoices;" Products.txt Invoices.txt
#./huc sql -st=MySQL -c="$mysql" -s="PRINT 'Hello'; PRINT 'World';" 

echo --- SQLLOAD MySQL ---
./huc sqlload -st=MySQL -c="$mysql" -drop -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -t=TempProducts Products.txt
./huc sqlload -st=MySQL -c="$mysql" -rowNumberColumnName=RowNumber -currentUtcDateTimeColumnName=UploadTime -d=NorthWind -t=TempProducts Products.txt
./huc sqlload -st=MySQL -c="$mysql" -d=NorthWind -t=TempProducts Products.txt
./huc sql -st=MySQL -c="$mysql" -s="SELECT * FROM TempProducts;" TempProducts.txt


echo --- TABLE ---
cp Orders.txt Orders.csv
./huc table Orders.csv
cp Orders100.txt Orders100.csv
./huc table -hd=pipe -hq=none -he=true -dd=pipe -dq=none -he=false Orders100.csv

echo --- TABLEHTML ---
cp Orders.txt Orders.html
./huc tablehtml Orders.html

echo --- TABLEXML ---
cp Orders.txt Orders.xml
./huc tablexml Orders.xml

echo --- TABLEJSON ---
cp Orders.txt Orders.json
./huc tablejson Orders.json



echo --- DIRECTORYFLATTEN ---
mkdir subdir1
mkdir subdir2
cp Orders.txt ./subdir1
mv Orders.xml ./subdir2
mv Orders.json ./subdir2
./huc directoryFlatten -c=KeepNewest .

echo --- DIRECTORYSIZE ---
./huc DirectorySize .



echo --- FTP ---
./huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt
./huc ftpput -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpdelete -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpdelete -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpput -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpdelete -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpput -h=$ip -u=testuser -p=testpass testdata1.txt testdata2.txt testdata3.txt
rm -f testdata*.txt
./huc ftpget -h=$ip -u=testuser -p=testpass te*.txt
./huc ftpget -h=$ip -u=testuser -p=testpass testdata?.txt

echo --- FTP IMPLICIT ---
./huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt
./huc ftpput -e=Implicit -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -e=Implicit -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpdelete -e=Implicit -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpdelete -e=Implicit -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpput -e=Implicit -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpdelete -e=Implicit -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpput -h=$ip -u=testuser -p=testpass testdata1.txt testdata2.txt testdata3.txt
rm -f testdata*.txt
./huc ftpget -e=Implicit -h=$ip -u=testuser -p=testpass te*.txt
./huc ftpget -e=Implicit -h=$ip -u=testuser -p=testpass testdata?.txt

echo --- FTP EXPLICIT ---
./huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt
./huc ftpput -e=Explicit -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -e=Explicit -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpdelete -e=Explicit -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpdelete -e=Explicit -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpput -e=Explicit -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpdelete -e=Explicit -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpput -h=$ip -u=testuser -p=testpass testdata1.txt testdata2.txt testdata3.txt
rm -f testdata*.txt
./huc ftpget -e=Explicit -h=$ip -u=testuser -p=testpass te*.txt
./huc ftpget -e=Explicit -h=$ip -u=testuser -p=testpass testdata?.txt

echo --- FTP SSH ---
./huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt
./huc ftpput -e=SSH -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -e=SSH -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpdelete -e=SSH -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpdelete -e=SSH -h=$ip -u=testuser -p=testpass testdata2.txt testdata3.txt
./huc ftpput -e=SSH -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpdelete -e=SSH -h=$ip -u=testuser -p=testpass "testdata?.txt"
./huc ftpput -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpget -h=$ip -u=testuser -p=testpass testdata1.txt
./huc ftpput -h=$ip -u=testuser -p=testpass testdata1.txt testdata2.txt testdata3.txt
rm -f testdata*.txt
./huc ftpget -e=SSH -h=$ip -u=testuser -p=testpass te*.txt
./huc ftpget -e=SSH -h=$ip -u=testuser -p=testpass testdata?.txt



echo --- ZIP ---
./huc GenerateRandomFile -l=1000000 testdata1.txt testdata2.txt testdata3.txt
./huc zip testdata.zip "testdata?.txt"


echo --- DirectoryList ---
./huc GenerateRandomFile ./testsub1/testSub11/myfile11.txt
./huc GenerateRandomFile ./testsub1/myfile1.txt
./huc GenerateRandomFile ./testsub1/testSub11/myfile11.txt
./huc GenerateRandomFile ./testsub1/testSub11/myfile12.txt
./huc GenerateRandomFile ./testsub1/testSub12/myfile21.txt
./huc GenerateRandomFile ./testsub2/myfile75657.txt
./huc DirectoryListTextLines . directoryList.txt -dts=9 -fpat=*.txt
./huc sqlload -drop -c="$mssql" -d=NorthWind -t=DirectoryListTextLines directoryList.txt


