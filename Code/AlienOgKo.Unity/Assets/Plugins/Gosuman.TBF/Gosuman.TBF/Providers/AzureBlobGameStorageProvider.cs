#if NETCOREAPP
using Azure.Storage.Blobs;
using Gosuman.EntitySystem.Database;
using Gosuman.EntitySystem.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Gosuman.TBF.Providers;

public class AzureBlobGameStorageProvider<T> : IGameStorageProvider where T : TurnBasedSystem, new()
{
    private readonly ILogger<AzureBlobGameStorageProvider<T>> logger;
    private readonly BlobContainerClient container;
    private readonly JsonSerializer serializer;

    public AzureBlobGameStorageProvider(IConfiguration config, ILogger<AzureBlobGameStorageProvider<T>> logger, JsonSerializerSettings serializerSettings)
    {
        this.logger = logger;
        serializer = JsonSerializer.Create(serializerSettings);
        var connectionConfig = config["AzureStorage.Connection"] ?? config["APPSETTING_AzureStorage_Connection"];
        var containerConfig = config["AzureStorage.Container"] ?? config["APPSETTING_AzureStorage_Container"];

        container = new BlobContainerClient(connectionConfig, containerConfig);
        var exists = container.Exists();
        if (!exists.Value)
        {
            throw new InvalidOperationException("The Azure Storage container does not exist.");
        }
    }

    public async Task DeleteGame(string gameId)
    {
        try
        {
            var blobClient = container.GetBlobClient($"Game-{gameId}.json");
            if (blobClient.Exists())
            {
                await blobClient.DeleteAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading blob from Azure.");
        }
    }

    public async Task<TurnBasedSystem?> LoadGame(string gameId)
    {
        try
        {
            var blobClient = container.GetBlobClient($"Game-{gameId}.json");
            TurnBasedSystem? game = null;
            if (blobClient.Exists())
            {
                var result = await blobClient.DownloadContentAsync();
                using var sr = new StreamReader(result.Value.Content.ToStream());
                using var jtr = new JsonTextReader(sr);
                try
                {
                    var database = serializer.Deserialize<EntityDatabase>(jtr) ?? throw new JsonSerializationException("Could not deserialize game.");
                    EntityReferenceFixer.Fix(database);
                    game = new T();
                    game.Initialize(database.Entities);
                }
                catch
                {
                    // If we cannot read the game, just delete it so it does not block playing.
                    await blobClient.DeleteAsync();
                }
                return game;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading blob from Azure.");
        }
        return null;
    }

    public async Task SaveGame(string gameId, TurnBasedSystem game)
    {
        try
        {
            if (game.Database.IsEmpty())
            {
                // The game state is empty. Delete it instead of saving.
                await DeleteGame(gameId);
                return;
            }
            using var ms = new MemoryStream(2048);
            using var tw = new StreamWriter(ms);
            lock (game)
            {
                serializer.Serialize(tw, game.Database);
            }
            tw.Flush();
            ms.Position = 0;
            var blobClient = container.GetBlobClient($"Game-{gameId}.json");
            await blobClient.UploadAsync(ms, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving blob to Azure.");
        }
    }
}
#endif