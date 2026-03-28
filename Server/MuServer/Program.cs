using MuServer.Core;
using MuServer.Database;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.Title = "MU Online Server";

PrintBanner();

var config = ServerConfig.Load();
var db = new DatabaseManager(config.DatabasePath);
db.Initialize();

var server = new GameServer(config, db);
await server.StartAsync();

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(@"
  __  __ _   _    ___  _   _ _     ___ _   _ ___ 
 |  \/  | | | |  / _ \| \ | | |   |_ _| \ | | __|
 | |\/| | |_| | | | | |  \| | |    | ||  \| | _| 
 |_|  |_|\___/  |_| |_|_|\__|_|___|___|_|\__|___|
                                |_____|           
         MU Online Server - .NET 10
    ");
    Console.ResetColor();
}
