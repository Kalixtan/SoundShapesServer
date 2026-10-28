using SoundShapesServer.Database;
using SoundShapesServer.Helpers;
using SoundShapesServer.Types;
using SoundShapesServer.Types.Authentication;
using SoundShapesServer.Types.Levels;
using SoundShapesServer.Types.Users;

if (args.Length != 1) throw new ArgumentException("Expected data directory");
Directory.CreateDirectory(args[0]);
Environment.SetEnvironmentVariable("BUNKUM_DATA_FOLDER", args[0]);
var provider = new GameDatabaseProvider();
provider.Initialize();
using var database = provider.GetContext();
var user = database.CreateUser("openegg_probe");
var target = database.CreateUser("openegg_probe_target");
var now = DateTimeOffset.UtcNow;
var level = new GameLevel {
    Id = IdHelper.GenerateLevelId(), Author = target, Name = "Probe level",
    CreationDate = now, ModificationDate = now, Visibility = LevelVisibility.Public,
    UploadPlatform = PlatformType.Unknown
};
database.AddLevel(level, true);
var token = database.CreateToken(user, TokenType.GameAccess,
    TokenAuthenticationType.None, 3600, PlatformType.Ps3, null);
var ownerToken = database.CreateToken(target, TokenType.GameAccess,
    TokenAuthenticationType.None, 3600, PlatformType.Ps3, null);
Console.WriteLine($"FIXTURE {user.Id} {token.Id} {target.Id} {level.Id} {ownerToken.Id}");
