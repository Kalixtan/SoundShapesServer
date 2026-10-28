using System.Net;
using Bunkum.Core;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Core.Storage;
using Bunkum.Listener.Protocol;
using Bunkum.Protocols.Http;
using HttpMultipartParser;
using SoundShapesServer.Configuration;
using SoundShapesServer.Database;
using SoundShapesServer.Extensions;
using SoundShapesServer.Responses.Game.Levels;
using SoundShapesServer.Services;
using SoundShapesServer.Types.Authentication;
using SoundShapesServer.Types.Levels;
using SoundShapesServer.Types.Users;

namespace SoundShapesServer.Endpoints.Game.Levels;

public class Endpoints : EndpointGroup
{
    // The database currently retains only the original upload, not upload history.
    [GameEndpoint("~level:{levelId}/~upload:{arguments}")]
    [Authentication(false)]
    public Response GetUpload(RequestContext context, GameDatabaseContext database, GameUser? user, string levelId, string arguments)
    {
        const string suffix = ".get";
        if (!arguments.EndsWith(suffix, StringComparison.Ordinal) ||
            !long.TryParse(arguments[..^suffix.Length], out long uploadId))
            return new Response(HttpStatusCode.NotFound);

        GameLevel? level = database.GetLevelWithId(levelId);
        if (level == null || !level.HasUserAccess(user) ||
            level.CreationDate.ToUnixTimeMilliseconds() != uploadId)
            return new Response(HttpStatusCode.NotFound);

        // Publish and upload records share an identifier; the exact retail
        // response fields still need confirmation.
        return new Response(new LevelPublishResponse(level), ContentType.Json);
    }

    [GameEndpoint("~level:{args}")]
    public Response GetEndpoints(RequestContext context, IDataStore dataStore, GameDatabaseContext database, GameUser user, string args)
    {
        string[] arguments = args.Split('.', 2);
        if (arguments.Length != 2 || arguments.Any(string.IsNullOrWhiteSpace))
            return new Response(HttpStatusCode.NotFound);

        string levelId = arguments[0];
        string action = arguments[1];
        
        GameLevel? level = database.GetLevelWithId(levelId);
        
        if (level == null) return new Response(HttpStatusCode.NotFound);

        return action switch
        {
            "delete" => LevelManagementEndpoints.RemoveLevel(dataStore, database, user, level),
            "latest" when level.HasUserAccess(user) => new Response(new LevelResponse(level, user), ContentType.Json),
            _ => new Response(HttpStatusCode.NotFound)
        };
    }

    [GameEndpoint("~level:{args}", HttpMethods.Post)]
    public Response PostEndpoints(RequestContext context, IDataStore dataStore, ProfanityService profanity, Stream body, GameDatabaseContext database, GameServerConfig config, GameUser user, GameToken token, string args)
    {
        string[] arguments = args.Split('.', 2);
        if (arguments.Length != 2 || arguments.Any(string.IsNullOrWhiteSpace))
            return new Response(HttpStatusCode.NotFound);

        string levelId = arguments[0];
        string action = arguments[1];

        MultipartFormDataParser parser;
        try
        {
            parser = MultipartFormDataParser.Parse(body);
        }
        catch (Exception)
        {
            return new Response(HttpStatusCode.BadRequest);
        }

        if (action == "create") return LevelManagementEndpoints.CreateLevel(context, config, dataStore, profanity, parser, database, user, token);
        
        GameLevel? level = database.GetLevelWithId(levelId);
        if (level == null) return new Response(HttpStatusCode.NotFound);

        return action == "update" ? LevelManagementEndpoints.UpdateLevel(context, dataStore, parser, database, user, level.Id) : new Response(HttpStatusCode.NotFound);
    }
}
