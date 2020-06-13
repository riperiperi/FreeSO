# Getting started #
## Prerequisites ##
1. MySQL database server (preferrably MariaDB)
2. .NET Core 2.2 SDK or Runtime

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

    dotnet exec FSO.Server.Core.dll db-init

The tool will show you the differences between your database and the expected structure. Press Y to install all the tables and scripts.

You can use this tool to update your database as new features are added. It will only apply the changes. In these cases you should backup your database first.

The server itself can be run with the following command

    dotnet exec FSO.Server.Core.dll run

Note that there are a few other useful tools in there, such as import-nhood and restore-lots. You can find more information on these on the wiki.

## Configure api ##
The API is a http web server which provides the following services:

1. Authentication - Consistent with Maxis protocol
2. Shard (city) selection - Consistent with Maxis protocol
3. User registration
4. System administration

This API can be used with fso and with the original tso game (with host file entries and after trusting a self signed CA).

### SSL configuration for the server (currently disabled) ###
Ingame communications are currently done entirely in plaintext, so the following information is outdated. The game server will run correctly with However, if you would like to revive the SSL mode, you should use the below information to help set it all up.

Note that this does *NOT* mean that you should run the API server on plain HTTP - the current recommended method is to use a nginx or IIS reverse proxy with letsencrypt, which is the easiest way to 

#### SSL with self signed certificates (Windows) ####
This approach is the easiest for development and has a side benefit of allowing the original TSO game client to connect to this API (with host file entries). NOTE: This wont work for other people unless they install your certificates into their trust stores.

First you will need to make sure you have the makecert command. It is available in the Visual Studio command prompt.

Next you need to create a self signed certificate authority cert. Below is an example command you can run:
    
    makecert -r -pe -n "CN=My Root Authority" -ss CA -sr CurrentUser -a sha1 -sky signature -cy authority -sv CA.pvk CA.cer

This should produce two files, CA.cer and CA.pvk.

Next, you need to issue the certificate itself. For compatibility with the original TSO game use a CN value of auth.east.ea.com.

    makecert -pe -n "CN=auth.east.ea.com" -a sha1 -sky Exchange ^
     -eku 1.3.6.1.5.5.7.3.1 -ic CA.cer -iv CA.pvk ^
     -sp "Microsoft RSA SChannel Cryptographic Provider" ^
     -sy 12 -sv auth.east.ea.com.pvk auth.east.ea.com.cer

Convert the certificate into a pfx file.

    pvk2pfx -pvk auth.east.ea.com.pvk -spc auth.east.ea.com.cer -pfx auth.east.ea.com.pfx

Install the CA.cer into your trusted root certificate authorities. 

1. Double click on CA.cer
2. Click Install certificate
3. Click Next
4. Choose "Place all certificates in the following store". Browse and select "Trusted Root Certificate Authorities".

Install auth.east.ea.com.pfx onto your PC

1. Run mmc.exe
2. Go to File => Add / Remove snap in. Add the Certificates snap in choosing Computer Account.
3. Navigate to Personal / Certificates.
4. Right click on the Certificates folder in the tree and run All Tasks => Import.
5. Follow the wizard to import your pfx file (it may not have a password).

Configure windows to use this certificate on port 443 for fso server.

1. Get the thumbprint of the certificate you just generated. Instructions can be found [here](https://msdn.microsoft.com/en-us/library/ms734695(v=vs.110).aspx). When you copy the value paste ti into notepad, press home and press delete twice and reenter the first character. The certificate GUI tool adds non-printable characters which break the next step.
2. Run the command below, replacing the certhash with your hash. The app id refers to the Assembly id found in AssemblyInfo.cs in FSO.server. You do not need to change this.

Example command:

    netsh http add sslcert ipport=0.0.0.0:443 certhash=c28d66a5d109e9a44afe47cd3d529d9a87ee7a75 appid={8f125201-fdf0-4a13-886f-19662707d34d}

You should now be able to update your config.json so the API has the following bindings:

    "bindings": ["https://*:443/"],

Finally, add a host file entry for auth.east.ea.com pointing to 127.0.0.1.

#### SSL with real certificates (Windows) ####
Install your certificate into the computer's personal certificates.

1. Run mmc.exe
2. Go to File => Add / Remove snap in. Add the Certificates snap in choosing Computer Account.
3. Navigate to Personal / Certificates.
4. Right click on the Certificates folder in the tree and run All Tasks => Import.
5. Follow the wizard to import your pfx file (it may not have a password).

Configure windows to use this certificate on port 443 for fso server.

1. Get the thumbprint of the certificate you just generated. Instructions can be found [here](https://msdn.microsoft.com/en-us/library/ms734695(v=vs.110).aspx). When you copy the value paste it into notepad, press home and press delete twice and reenter the first character. The certificate GUI tool adds non-printable characters which break the next step.
2. Run the command below, replacing the certhash with your hash. The app id refers to the Assembly id found in AssemblyInfo.cs in FSO.server. You do not need to change this.

Example command:

    netsh http add sslcert ipport=0.0.0.0:443 certhash=c28d66a5d109e9a44afe47cd3d529d9a87ee7a75 appid={8f125201-fdf0-4a13-886f-19662707d34d}

You should now be able to update your config.json so the API has the following bindings:

    "bindings": ["https://*:443/"],


If you want to use this setup with the original TSO game you will need to update the config files within TSO to repoint them.

Update TSOClient\sys\gameentry.ini so that Server=yourserver.com

Also update TSOClient\sys\cityselector.ini so that ServerName = yourserver.com

#### SSL with letsencrypt.org ####
TODO

### HTTP only ###
Running in a HTTP only mode as your main endpoint is strongly discouraged. However there are circumstances where this is valid and thus it is a supported option.

Specifically, you can put another tool such as NGINX, IIS or an Amazon Load Balancer in front of the API to handle SSL. In these cases the tool in front would provide SSL and make HTTP requests to the API. For the first two, there are many methods to set up free letsencrypt certificates with periodic renewals.

For this configuration, set the API bindings to something like this:

    "bindings": ["http://*:8080/"],