using System.ComponentModel;
using System.Linq;
using Xunit;

namespace BH.DIS.Core.Events
{
    public class EventTypeTests
    {
        class EmptyEvent : Event { }

        [Fact]
        public void Id_Always_ReturnsNameOfTypeParameter()
        {
            // Arrange.
            var eventType = new EventType<EmptyEvent>();

            // Act.
            var id = eventType.Id;

            // Assert.
            Assert.Equal(typeof(EmptyEvent).Name, id);
        }

        [Fact]
        public void Name_Always_ReturnsNameOfTypeParameter()
        {
            // Arrange.
            var eventType = new EventType<EmptyEvent>();

            // Act.
            var name = eventType.Name;

            // Assert.
            Assert.Equal(typeof(EmptyEvent).Name, name);
        }

        const string MyDescription = "blabvlablab";
        [Description(MyDescription)]
        class EventWithDescription : Event { }

        [Fact]
        public void Description_WhenDescriptionAttributeApplied_ReturnsValue()
        {
            // Arrange.
            var eventType = new EventType<EventWithDescription>();

            // Act.
            var description = eventType.Description;

            // Assert.
            Assert.Equal(MyDescription, description);
        }

        [Fact]
        public void Description_WhenDescriptionAttributeNotApplied_ReturnsNull()
        {
            // Arrange.
            var eventType = new EventType<EmptyEvent>();

            // Act.
            var description = eventType.Description;

            // Assert.
            Assert.Null(description);
        }

        [Fact]
        public void Properties_WhenNoDefinedProperties_IsEmpty()
        {
            // Arrange.
            var eventType = new EventType<EmptyEvent>();

            // Act.
            var properties = eventType.Properties;

            // Assert.
            Assert.Empty(properties);
        }

        class EventWithProperties : Event
        {
            public int MyInt { get; set; }
            public string MyString { get; set; }
        }

        [Fact]
        public void Properties_WhenPropertiesExistOnEventType_ContainsAllOfThem()
        {
            // Arrange.
            var eventType = new EventType<EventWithProperties>();

            // Act.
            var properties = eventType.Properties;

            // Assert.
            Assert.Equal(3, properties.Count());
            Assert.Contains(properties, p => p.Name == nameof(EventWithProperties.MyInt));
            Assert.Contains(properties, p => p.Name == nameof(EventWithProperties.MyString));
        }

        class MyEvent : Event
        {
            public string MyString { get; set; }
        }

        [Fact]
        public void Properties_WhenStringPropertyDefined_ContainsIt()
        {
            var eventType = new EventType<MyEvent>();

            Assert.Single(eventType.Properties, p => p.Name == nameof(MyEvent.MyString));
        }


    }
}
