PATH=%PATH%;%CD%/DbLinq-0.20.1

REM DbMetal --user=root --password=  --server=localhost --provider=MySql --dbml=DataModel.dbml --database=pd
dbmetal /server:127.0.0.1 /user:root /password:root /provider:MySql /database:tso /language:C# /code:../DataModelGenerated.cs /aliases:ModelAliases.xml /pluralize --debug
