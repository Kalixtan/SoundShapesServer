using SoundShapesServer.Database;
using SoundShapesServer.Types;
using SoundShapesServer.Types.Authentication;
using SoundShapesServer.Types.Users;

if (args.Length != 1) throw new ArgumentException("Expected data directory");
Directory.CreateDirectory(args[0]);
Environment.SetEnvironmentVariable("BUNKUM_DATA_FOLDER", args[0]);
var provider = new GameDatabaseProvider();
provider.Initialize();
using var database = provider.GetContext();
var user = database.CreateUser("openegg_probe");
var target = database.CreateUser("openegg_probe_target");
var token = database.CreateToken(user, TokenType.GameAccess,
    TokenAuthenticationType.None, 3600, PlatformType.Ps3, null);
Console.WriteLine($"FIXTURE {user.Id} {token.Id} {target.Id}");
