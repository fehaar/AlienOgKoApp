using Gosuman.TBF.Shared.Serialization;
using Newtonsoft.Json;

namespace Gosuman.TBF.Shared.Test.Serialization;

public class SerializingGameActions : xSpec
{
    [Fact]
    public void DelegatesWillNotGetSerialized()
    {
        var gameAction = default(IClientGameAction);
        Given("a client game action that has a delegate", () =>
        {
            gameAction = new TestActionWithDelegate();
        });
        var json = string.Empty;
        Where("we serialize it to JSON", () =>
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new GameActionConverter<IClientGameAction>());
            json = JsonConvert.SerializeObject(gameAction, settings);
        });
        Expect("that the delegate will not be in the resulting data", () =>
        {
            json.Should().NotBeNullOrEmpty();
            json.Should().NotContain("Delegate");
        });
    }
}