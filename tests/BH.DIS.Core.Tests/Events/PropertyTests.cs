using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace BH.DIS.Core.Events
{
    public class PropertyTests
    {
        class MyClass
        {
            public const string MyDescription = "Blabalba";

            public string MyProperty { get; set; }

            [Required]
            public string MyRequiredProperty { get; set; }

            [Description(MyDescription)]
            public string MyDescribedProperty { get; set; }
        }

        [Fact]
        public void Name_Always_ReturnsNameOfProperty()
        {
            // Arrange
            var propertyInfo = typeof(MyClass).GetProperty(nameof(MyClass.MyProperty));
            var property = new Property(propertyInfo);

            // Act
            string name = property.Name;

            // Assert
            Assert.Equal(nameof(MyClass.MyProperty), name);
        }

        [Fact]
        public void TypeName_Always_ReturnsNameOfProperty()
        {
            // Arrange
            var propertyInfo = typeof(MyClass).GetProperty(nameof(MyClass.MyProperty));
            var property = new Property(propertyInfo);

            // Act
            string typeName = property.TypeName;

            // Assert
            Assert.Equal(propertyInfo.PropertyType.Name, typeName);
        }

        [Fact]
        public void TypeFullName_Always_ReturnsFullNameOfProperty()
        {
            // Arrange
            var propertyInfo = typeof(MyClass).GetProperty(nameof(MyClass.MyProperty));
            var property = new Property(propertyInfo);

            // Act
            string typeFullName = property.TypeFullName;

            // Assert
            Assert.Equal(propertyInfo.PropertyType.FullName, typeFullName);
        }

        [Fact]
        public void Description_WhenDescriptionAttributeApplied_ReturnsValue()
        {
            // Arrange
            var propertyInfo = typeof(MyClass).GetProperty(nameof(MyClass.MyDescribedProperty));
            var property = new Property(propertyInfo);

            // Act
            string description = property.Description;

            // Assert
            Assert.Equal(MyClass.MyDescription, description);
        }

        [Fact]
        public void Description_WhenDescriptionAttributeNotApplied_ReturnsNull()
        {
            // Arrange
            var propertyInfo = typeof(MyClass).GetProperty(nameof(MyClass.MyProperty));
            var property = new Property(propertyInfo);

            // Act
            string description = property.Description;

            // Assert
            Assert.Null(description);
        }

        [Fact]
        public void IsRequired_WhenRequiredAttributeApplied_ReturnsTrue()
        {
            // Arrange
            var propertyInfo = typeof(MyClass).GetProperty(nameof(MyClass.MyRequiredProperty));
            var property = new Property(propertyInfo);

            // Act
            bool isRequired = property.IsRequired;

            // Assert
            Assert.True(isRequired);
        }

        [Fact]
        public void IsRequired_WhenRequiredAttributeNotApplied_ReturnsFalse()
        {
            // Arrange
            var propertyInfo = typeof(MyClass).GetProperty(nameof(MyClass.MyProperty));
            var property = new Property(propertyInfo);

            // Act
            bool isRequired = property.IsRequired;

            // Assert
            Assert.False(isRequired);
        }
    }
}
