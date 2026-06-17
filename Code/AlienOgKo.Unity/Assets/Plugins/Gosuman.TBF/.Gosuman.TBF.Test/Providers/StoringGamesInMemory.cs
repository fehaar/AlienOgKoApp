using Gosuman.TBF.Providers;


namespace Gosuman.TBF.Test.Providers
{
    public class StoringGamesInMemory : xSpec
    {
        [Fact]
        public void InitialGames()
        {
            var provider = default(IGameStorageProvider);            
            Given("a memory game store", () => {
                provider = new MemoryGameStorageProvider();
            });
            Expect("that we do not have a game from the start", () => {
                if (provider != null)
                {
                    var task = provider.LoadGame("TestGame");
                    task.Wait();
                    var game = task.Result;
                    game.Should().BeNull();
                }
            });
            Where("we save a game", () => {
                if (provider != null)
                {
                    provider.SaveGame("TestGame", new TestGame()).Wait();
                }
            });
            Expect("that we can now load the game", () => {
                if (provider != null)
                {
                    var task = provider.LoadGame("TestGame");
                    task.Wait();
                    var game = task.Result;
                    game.Should().NotBeNull();
                }
            });
        }

        [Fact]
        public void SaveAndLoad()
        {
            var provider = default(IGameStorageProvider);
            Given("a memory game store", () => {
                provider = new MemoryGameStorageProvider();
            });
            Where("we save a game", () => {
                if (provider != null)
                {
                    provider.SaveGame("TestGame", new TestGame()).Wait();
                }
            });
            Expect("that we can now load the game", () => {
                if (provider != null)
                {
                    var task = provider.LoadGame("TestGame");
                    task.Wait();
                    var game = task.Result;
                    game.Should().NotBeNull();
                }
            });
        }

        [Fact]
        public void DeleteAGame()
        {
            var provider = default(IGameStorageProvider);
            Given("a memory game store with a game in it", () => {
                provider = new MemoryGameStorageProvider();
                provider.SaveGame("TestGame", new TestGame()).Wait();
            });
            Where("we delete the game", () => {
                if (provider != null)
                {
                    provider.DeleteGame("TestGame").Wait();
                }
            });
            Expect("that the game is now gone", () => {
                if (provider != null)
                {
                    var task = provider.LoadGame("TestGame");
                    task.Wait();
                    var game = task.Result;
                    game.Should().BeNull();
                }
            });
        }
    }
}
