# Migration Note #

Please see FSO.Server.Core for the full information on how to run the server.

The server software is currently being migrated to run on .NET Core. Due to framework version restrictions (and target OS support), most projects are still in .NET framework, including the majority of the server. In future, shared projects will use .NET Standard and will be easily shared between the .NET Framework client project and .NET Core server, but that day is not today.