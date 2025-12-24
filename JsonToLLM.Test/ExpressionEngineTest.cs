using Newtonsoft.Json.Linq;
using JsonToLLM.Model;
using Moq;

namespace JsonToLLM.Test
{
    public class ExpressionEngineTest
    {
        private readonly Mock<IExpressionResolver> _mockExpressionResolver = new();
        private readonly ExpressionEngine _engine;

        public ExpressionEngineTest()
        {
            _mockExpressionResolver
                .Setup(r => r.Resolve("echo", It.IsAny<TemplateContext>(), It.IsAny<string?[]?>()))
                .Returns<string, TemplateContext, string?[]?>((_, context, args) => new EchoExpression(context, args![0]!.ToString()));
           
            _mockExpressionResolver
                .Setup(r => r.Resolve("concat", It.IsAny<TemplateContext>(), It.IsAny<string?[]?>()))
                .Returns<string, TemplateContext, string?[]?>((_, context, args) => new ConcatArgsExpression(context, args));

            _engine = new ExpressionEngine(_mockExpressionResolver.Object);
        }

        [Theory]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData("", typeof(ArgumentException))]
        [InlineData("   ", typeof(ArgumentException))]
        public void Evaluate_InvalidExpression_ThrowsArgumentException(string expr, Type exType)
        {
            // Arrange
            var ctx = TemplateContext.Create(new JObject(), new JObject());

            // Act & Assert
            Assert.Throws(exType, () => _engine.Evaluate(expr!, ctx));
        }

        [Fact]
        public void Evaluate_NullContext_ThrowsArgumentNullException()
        {
            // Arrange
           
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _engine.Evaluate("@fn()", null!));
        }

        [Fact]
        public void Evaluate_NullLocalContext_ThrowsArgumentNullException()
        { 
            // Arrange
            var ctx = TemplateContext.Create(new JObject(), null!);

            Assert.Throws<ArgumentNullException>(() => _engine.Evaluate("@fn()", ctx));
        }

        [Fact]
        public void Evaluate_NullGlobalContext_ThrowsArgumentNullException()
        {
            // Arrange
            var ctx = TemplateContext.Create(null!, new JObject());

            Assert.Throws<ArgumentNullException>(() => _engine.Evaluate("@fn()", ctx));
        }

        [Fact]
        public void Evaluate_Echo_ReturnsArgument()
        {
            // Arrange
            var ctx = TemplateContext.Create(new JObject(), new JObject());
            
            // Act
            var result = _engine.Evaluate("@echo(\"hello\")", ctx);

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public void Evaluate_NonFunction_ReturnsInputUnchanged()
        {
            // Arrange
            var ctx = TemplateContext.Create(new JObject(), new JObject());
            const string input = "plain text without function";

            // Act
            var result = _engine.Evaluate(input, ctx);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Evaluate_SingleFunction_WithVariousArguments_PassesArgsToResolver()
        {
            // Arrange
            var ctx = TemplateContext.Create(new JObject(), new JObject());

            // Act
            // Provide string, number and null arguments
            var result = _engine.Evaluate("@concat(\"hello\", 123, null)", ctx);

            // Assert
            Assert.Equal("hello|123|null", result);
        }

        [Fact]
        public void Evaluate_NestedFunction_InnerResolvedBeforeOuter()
        {
            // Arrange
            var ctx = TemplateContext.Create(new JObject(), new JObject());

            // Act
            // Outer has inner as first arg; inner should be evaluated first and its result passed to outer
            var result = _engine.Evaluate("@concat(@concat( \"Inner\", \"then\" ), \"Outer\")", ctx);

            // Assert
            Assert.Equal("Inner|then|Outer", result);
        }
    }
}


namespace JsonToLLM.Model
{
    internal class EchoExpression : ExpressionBase
    {
        public string Message { get; }

        public EchoExpression(TemplateContext context, string message) : base(context)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
            Message = message;
        }

        public override string GetValue() => Message;
    }

    internal class ConcatArgsExpression(TemplateContext context, string?[]? args) : ExpressionBase(context)
    {
        public override string GetValue()
        {
            if (args == null || args.Length == 0)
                return string.Empty;

            var parts = args.Select(a => a is null ? "null" : a.ToString());
            return string.Join("|", parts);
        }
    }
}
