using BH.DIS.Validation.Attributes;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace BH.DIS.Core.Events
{
    public class EventTests
    {
        class EmptyEvent : Event { }

        [Fact]
        public void GetSessionId_WhenNotOverridden_ReturnsUniqueValue()
        {
            // Arrange.
            var @event = new EmptyEvent();

            // Act.
            string sessionId1 = @event.GetSessionId();
            string sessionId2 = @event.GetSessionId();

            // Assert.
            Assert.NotEqual(sessionId1, sessionId2);
        }

        class EventWithRequiredProperty : Event
        {
            [Required]
            public string RequiredProperty { get; set; }
        }

        [Fact]
        public void Validate_WhenRequiredPropertyIsNull_ThrowsValidationException()
        {
            var invalidEvent = new EventWithRequiredProperty()
            {
                RequiredProperty = null,
            };

            Assert.Throws<ValidationException>(invalidEvent.Validate);
        }

        [Fact]
        public void Validate_WhenRequiredPropertyIsEmpty_ThrowsValidationException()
        {
            var invalidEvent = new EventWithRequiredProperty()
            {
                RequiredProperty = "",
            };

            Assert.Throws<ValidationException>(invalidEvent.Validate);
        }

        [Fact]
        public void Validate_WhenRequiredPropertyHasValue_DoesNotThrow()
        {
            var invalidEvent = new EventWithRequiredProperty()
            {
                RequiredProperty = "some value",
            };

            invalidEvent.Validate();
        }


        class EventWithRequiredAllowEmptyProperty : Event
        {
            [RequiredAllowEmptyString]
            public string RequiredString { get; set; }
        }

        [Fact]
        public void Validate_WhenRequiredAllowEmptyString_ThrowsError()
        {
            var invalidEvent = new EventWithRequiredAllowEmptyProperty
            {
                RequiredString = null
            };

            Assert.Throws<ValidationException>(invalidEvent.Validate);
        }

        [Fact]
        public void Validate_WhenRequiredAllowEmptyString_DoesNotThrow()
        {
            var invalidEvent = new EventWithRequiredAllowEmptyProperty
            {
                RequiredString = ""
            };

            invalidEvent.Validate();
        }

    }
}
