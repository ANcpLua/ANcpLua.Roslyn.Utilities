using ANcpLua.Roslyn.Utilities;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Roslyn.Utilities.Testing.Tests;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("GetValue", "get-value", "get_value")]
    [InlineData("XMLParser", "xml-parser", "xml_parser")]
    [InlineData("GetHTTPClient", "get-http-client", "get_http_client")]
    [InlineData("SimpleTest", "simple-test", "simple_test")]
    [InlineData("", "", "")]
    public void DelimitedCase_UsesSharedAcronymBoundaries(string input, string kebab, string snake)
    {
        input.ToKebabCase().Should().Be(kebab);
        input.ToSnakeCase().Should().Be(snake);
    }

    [Fact]
    public void NameCase_ValidatesNullAndEmptyInputs()
    {
        ((Action)(() => ((string)null!).ToPropertyName())).Should().Throw<ArgumentNullException>();
        ((Action)(() => "".ToPropertyName())).Should().Throw<ArgumentException>();
        ((Action)(() => ((string)null!).ToParameterName())).Should().Throw<ArgumentNullException>();
        ((Action)(() => "".ToParameterName())).Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("firstName", "FirstName")]
    [InlineData("x", "X")]
    public void ToPropertyName_UppercasesFirstCharacter(string input, string expected)
    {
        input.ToPropertyName().Should().Be(expected);
    }

    [Theory]
    [InlineData("FirstName", "firstName")]
    [InlineData("Class", "@class")]
    [InlineData("System", "system")]
    public void ToParameterName_LowercasesFirstCharacterAndEscapesKeywords(string input, string expected)
    {
        input.ToParameterName().Should().Be(expected);
    }

    [Theory]
    [InlineData("global::System.Int32?", "System.Int32")]
    [InlineData("global::System.Nullable<System.Int32>", "System.Nullable<System.Int32>")]
    public void NormalizeTypeName_RemovesGlobalPrefixAndNullableMarker(string input, string expected)
    {
        input.NormalizeTypeName().Should().Be(expected);
    }

    [Theory]
    [InlineData("int?", "int")]
    [InlineData("System.Nullable<int>", "int")]
    [InlineData("global::System.String?", "global::System.String")]
    public void UnwrapNullable_RemovesNullableWrappers(string input, string expected)
    {
        input.UnwrapNullable().Should().Be(expected);
    }

    [Theory]
    [InlineData("System.Int32", "int")]
    [InlineData("Int32", "int")]
    [InlineData("string", null)]
    public void GetCSharpKeyword_UsesBclAliasesOnly(string input, string? expected)
    {
        input.GetCSharpKeyword().Should().Be(expected);
    }

    [Theory]
    [InlineData("int", "System.Int32", true)]
    [InlineData("global::System.String", "String", true)]
    [InlineData("System.Boolean", "double", false)]
    public void TypeNamesEqual_NormalizesAliasesAndGlobalPrefixes(string left, string right, bool expected)
    {
        left.TypeNamesEqual(right).Should().Be(expected);
    }

    [Theory]
    [InlineData("string", true)]
    [InlineData("String", true)]
    [InlineData("System.String", true)]
    [InlineData("global::System.String", true)]
    [InlineData("System.DateTime", false)]
    public void IsStringType_UsesNormalizedTypeComparison(string input, bool expected)
    {
        input.IsStringType().Should().Be(expected);
    }

    [Theory]
    [InlineData("string", true)]
    [InlineData("System.Int64", true)]
    [InlineData("bool", true)]
    [InlineData("System.DateTime", false)]
    public void IsPrimitiveJsonType_UsesNormalizedPrimitiveSet(string input, bool expected)
    {
        input.IsPrimitiveJsonType().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("hello", "hello")]
    [InlineData("hello world", "\"hello world\"")]
    [InlineData("\"x\"", "\"x\"")]
    public void DoubleQuoteIfNeeded_PreservesExistingSemantics(string? input, string expected)
    {
        input.DoubleQuoteIfNeeded().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("hello", "hello")]
    [InlineData("hello world", "'hello world'")]
    [InlineData("'x'", "'x'")]
    public void SingleQuoteIfNeeded_PreservesExistingSemantics(string? input, string expected)
    {
        input.SingleQuoteIfNeeded().Should().Be(expected);
    }

    [Fact]
    public void QuoteHelpers_EscapeMatchingQuoteCharacter()
    {
        "a\"b".DoubleQuote().Should().Be("\"a\\\"b\"");
        "a'b".SingleQuote().Should().Be("'a\\'b'");
        "\"x\"".IsDoubleQuoted().Should().BeTrue();
        "x".IsDoubleQuoted().Should().BeFalse();
        "'x'".IsSingleQuoted().Should().BeTrue();
        "\"x\"".IsSingleQuoted().Should().BeFalse();
    }
}
