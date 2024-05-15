using ClearBible.Engine.Corpora;
using System;
using System.Collections.Generic;
using Xunit;

namespace ClearBible.Engine.Tests
{
    public class TokenMetadataPropertyTests
    {
        public class ComplexType
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private Token token_;
        public TokenMetadataPropertyTests()
        {
           token_ = new Token(new TokenId(1, 1, 1, 1, 1), "surface", "training")
            {
                Metadata =
                {
                    ["integer"] = 1,
                    ["string"] = "string",
                    ["double"] = 1.1,
                    ["bool"] = true,
                    ["complex"] = new ComplexType { Name = "name", Age = 1 }
                }
            };
        }

        [Fact]
        public void CanGetIntegerFromMetadata()
        {
            Assert.True(token_.HasMetadatum("integer"));
            var integer = token_.GetMetadatum<int>("integer");
            Assert.Equal(1, integer);
        }

        [Fact]
        public void CanGetStringFromMetadata()
        {
            Assert.True(token_.HasMetadatum("string"));
            var @string = token_.GetMetadatum<string>("string");
            Assert.Equal("string", @string);
        }

        [Fact]
        public void CanGetDoubleFromMetadata()
        {
            Assert.True(token_.HasMetadatum("double"));
            var @double = token_.GetMetadatum<double>("double");
            Assert.Equal(1.1, @double);
        }

        [Fact]
        public void CanGetBooleanFromMetadata()
        {
            Assert.True(token_.HasMetadatum("bool"));
            var @bool = token_.GetMetadatum<bool>("bool");
            Assert.True(@bool);
        }

        [Fact]
        public void CanGetComplexObjectFromMetadata()
        {
            Assert.True(token_.HasMetadatum("complex"));
            var complex = token_.GetMetadatum<ComplexType>("complex");
            Assert.Equal("name", complex.Name);
            Assert.Equal(1, complex.Age);
        }

        [Fact]
        public void ThrowsInvalidCastExceptionConvertingStringToBoolean()
        {
            var result = Assert.Throws<InvalidCastException>(() => token_.GetMetadatum<bool>("string"));
            Assert.Equal("Cannot cast 'string' to Boolean", result.Message);
        }

        [Fact]
        public void DoesNotThrowInvalidCastExceptionConvertingIntegerToDouble()
        {
            var result = (InvalidCastException)Record.Exception(() => token_.GetMetadatum<double>("integer"));
            Assert.Null(result);
        }

        [Fact]
        public void ThrowsKeyNotFoundException()
        {
            Assert.False(token_.HasMetadatum("unknown"));
            var result = Assert.Throws<KeyNotFoundException>(() => token_.GetMetadatum<string>("unknown"));
            Assert.Equal("Key 'unknown' not found in Metadata", result.Message);
        }
    }
}
