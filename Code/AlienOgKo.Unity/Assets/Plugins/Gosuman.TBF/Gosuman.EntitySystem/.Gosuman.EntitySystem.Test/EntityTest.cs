using FluentAssertions;
using Xunit;

namespace Gosuman.EntitySystem.Test
{
    public class EntityTest : xSpec
    {
        [Fact]
        public void UsingToString()
        {
            var entity = default(Entity); 
            Given("an entity", () =>
            {
                entity = new TestEntity();
            });
            Expect("that to string will not include the namespace", () =>
            {
                entity?.ToString().Should().Be("TestEntity");
            });
        }
    }
}
