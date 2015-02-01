PATH=%PATH%;%CD%/DbLinq-0.20.1

REM DbMetal --user=root --password=  --server=localhost --provider=MySql --dbml=DataModel.dbml --database=pd
dbmetal /server:127.0.0.1 /user:root /password:root /provider:MySql /database:eastjerome /language:C# /code:../CityDataModelGenerated.cs /aliases:ModelAliases.xml /pluralize --debug
