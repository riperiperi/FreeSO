# Database Setup

FreeSO operates using a MySQL database to keep track of pretty much everything that doesn't directly happen on a lot. It manages all shards (cities) that are exposed, the lots within them, all users, all sims and their associated resources.

We recommend that you use MariaDB as your database server, as it has higher performance under our load than MySQL, but is compatible with the database structure and queries that the game makes.

## MariaDB setup

The version of MariaDB used for the official server was MariaDB 10.5. Newer versions seem to be incompatible with the current version of MySQL.Data, which cannot be upgraded due to .NET Framework version requirements.

On Windows Server, you can find an MSI installer at https://mariadb.org/download/ that installs a MariaDB service that runs automatically whenever a user is logged in.

On linux, you should be able to find a package called `mariadb-server` (on apt) that will install everything that you need. You can then manage MariaDB's lifecycle with systemctl: `sudo systemctl start mariadb.service`. You can start configuring MariaDB by running `sudo mysql_secure_installation`.

You'll want to create a database for the game. Here's an example:

```sql
CREATE DATABASE `fso`
```

During the setup, you don't need to specify a password for the root account in your MySQL server - on Linux this can actually cause issues. Say yes to using UTF8 as a default. You _do_ want to add an account that the game server will use to access the database, so run a query on the database to add a user for the server to use:

```sql
GRANT ALL ON fso.* TO 'fsoserver'@'localhost' IDENTIFIED BY 'password' WITH GRANT OPTION;
```

Even though only localhost can access this account, you should change `password` to actually be something people can't guess. Run `FLUSH PRIVILEGES;` after this.

Congrats, you now have a working database. You can have a look over it with your fsoserver account, ideally using management tools such as MySQL Workbench.

If you need to connect to the server remotely, you can add additional account access from locations other than `localhost`, and also add incoming firewall exceptions for MariaDB on TCP 3306. Try to keep the exception specific to the IPs of the machines you want to access the database, as exposing the port to the internet without being specific is a security risk.

## Configuring the server to see your database

In the config.json file, near the top you'll see the field `database.connectionString`. Make sure that it properly includes all the information you just set up - the database IP, mysql username, password and the name of your database:

```json
{
  "database": {
	"connectionString": "server=127.0.0.1;uid=fsoserver;pwd=password;database=fso;"
   }
}
```

## Running DB init scripts

Once the server has been configured, you can run the `db-init` scripts to initialize the database. Use the following command:

`dotnet exec FSO.Server.Core.dll db-init`

This tool runs a bunch of scripts in the `DatabaseScripts` folder to set up the initial state of core tables in the database, then runs a sequence of "change" scripts found in the `DatabaseScripts/changes` folder to apply changes that have been made to the database structure since the scripts were first added to FreeSO. This includes everything from new fields to entirely new tables, so it's important that they all run.

When database scripts are run, they are registered in the `fso_db_changes` table, so that we know they have been applied and won't run again when you repeat this command. 

**This means that the command can (and should) be rerun whenever there are any changes to the database made in the codebase**, so that the database remains compatible with the game.

Database scripts are registered on the `manifest.json` file with unique ids, dependencies and associated script files. If you aim to modify the database structure, you should add your own scripts to this manifest file so that anyone using your codebase can update their database to work with your changes. Similarly, if you're using someone else's codebase, you'll need to be wary of database changes and rerun this command when they occur.
