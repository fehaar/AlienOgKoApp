#if NETCOREAPP
using Gosuman.EntitySystem.Database;
using Gosuman.EntitySystem.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Gosuman.TBF.Providers
{
    public class FileGameStorageProvider<T> : IGameStorageProvider where T : TurnBasedSystem, new()
    {
        private readonly string? path;
        private readonly JsonSerializer serializer;
        private readonly ILogger<FileGameStorageProvider<T>> logger;

        public FileGameStorageProvider(IConfiguration configuration, JsonSerializerSettings serializerSettings, ILogger<FileGameStorageProvider<T>> logger)
        {
            path = configuration["FileStorage.Path"];
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            serializer = JsonSerializer.Create(serializerSettings);
            serializer.Formatting = Formatting.Indented;
            this.logger = logger;
        }

        private string GetGameFilePath(string gameId) => Path.Combine(path ?? "", $"Game-{gameId}.json");

        public Task<TurnBasedSystem?> LoadGame(string gameId)
        {
            var filePath = GetGameFilePath(gameId);
            TurnBasedSystem? game = null;
            if (File.Exists(filePath))
            {
                using var fs = File.OpenRead(filePath);
                using var sr = new StreamReader(fs);
                using var jtr = new JsonTextReader(sr);
                try
                {
                    var database = serializer.Deserialize<EntityDatabase>(jtr) ?? throw new JsonSerializationException("Could not deserialize game.");
                    EntityReferenceFixer.Fix(database);
                    game = new T();
                    game.Initialize(database.Entities);
                }
                catch (Exception e)
                {
                    // If we get an exception while deserializing, move the game to an error file for examination and return null.
                    fs.Close();
                    logger.LogError($"Could not deserialize game. {e.Message}");
                    File.Move(filePath, Path.ChangeExtension(filePath, "error"), true);
                }
            }
            return Task.FromResult(game);
        }

        public Task SaveGame(string gameId, TurnBasedSystem game)
        {
            var filePath = GetGameFilePath(gameId);
            lock (game)
            {
                using var fs = File.Open(filePath, FileMode.Create, FileAccess.Write);
                using var sr = new StreamWriter(fs);
                serializer.Serialize(sr, game.Database.Entities);
            }
            return Task.CompletedTask;
        }

        public Task DeleteGame(string gameId)
        {
            var filePath = GetGameFilePath(gameId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
#endif