# Getting started #
## Prerequisites ##
1. MySQL database server
2. .NET or Mono runtime

## Configure database ##
Create a database within your MySQL server. You can use an existing database (tables are namespaced) but making a new one is recommended.

    CREATE DATABASE `fso`

Create a user on the database server with full access to the new database. Make sure you change the password for a production system.

    CREATE USER 'fsoserver'@'localhost' IDENTIFIED BY 'password';
    GRANT ALL PRIVILEGES ON fso.* TO 'fsoserver'@'localhost';

Setup config.json to connect to the new database. It should look something like this (with your updated password).

    {
      "database": {
    	"connectionString": "server=127.0.0.1;uid=fsoserver;pwd=password;database=fso;"
       }
    }
    
Finally, install the fso tables and database scripts. There is a tool built into the server that automates this process. Never do this manually unless you know what you are doing. Run the following command:

    server.exe db-init

The tool will show you the differences between your database and the expected structure. Press Y to install all the tables and scripts.

You can use this tool to update your database as new features are added. It will only apply the changes. In these cases you should backup your database first.

## Configure api ##
