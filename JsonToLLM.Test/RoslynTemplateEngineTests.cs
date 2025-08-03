using System.Globalization;
using JsonToLLM.CSharpScripting;
using Newtonsoft.Json.Linq;

namespace JsonToLLM.Test;

public class RoslynTemplateEngineTests
{
    private readonly RoslynTemplateEngine _engine = new(new RoslynExecutor());

    [Fact]
    public async Task RenderAsync_InlineExpression_RendersCorrectly()
    {
        var model = JObject.Parse("""{Name: "Alice"}""");
        const string tpl = "Hello @(Model.Name)!";

        var result = await _engine.RenderAsync(tpl, model);

        Assert.Equal("Hello Alice!", result);
    }

    [Fact]
    public async Task RenderAsync_BlockExpression_ReturnsEmptyIfNoReturn()
    {
        var model = JObject.Parse("{X: 5}");
        const string tpl = "Test @{ var y = Model.X * 2; } Done";

        var result = await _engine.RenderAsync(tpl, model);

        Assert.Equal("Test  Done", result);
    }

    [Fact]
    public async Task RenderAsync_BlockExpressionWithReturn_RendersResult()
    {
        var model = JObject.Parse("{X: 3}");
        const string tpl = "Value @{ return (Model.X + 4).ToString(); } Tail";

        var result = await _engine.RenderAsync(tpl, model);

        Assert.Equal("Value 7 Tail", result);
    }

    [Fact]
    public async Task RenderAsync_MixedInlineAndBlock()
    {
        var model = JObject.Parse("{A: 1, B: 2, C:3}");
        const string tpl = "Start @(Model.A) @{ return (Model.B*2).ToString(); } End @(Model.C)";

        var result = await _engine.RenderAsync(tpl, model);

        Assert.Equal("Start 1 4 End 3", result);
    }

    [Fact]
    public async Task RenderAsync_BlockExpressionTransformsAnArray()
    {
        var model = JObject.Parse("""
                                  {items: [
                                    {no: 1, price: 10},
                                    {no: 2, price: 20},
                                    {no: 3, price: 30},
                                    {no: 4, price: 40},
                                    {no: 5, price: 50}
                                  ]}
                                  """);
        const string tpl = """
                           items: 
                           @{ 
                            var sp = new StringBuilder();
                            foreach(var item in Model.items){
                                sp.AppendLine($"- {item.no}. {item.price}$");
                            }
                            return sp.ToString().Trim();
                           }
                           """;

        const string expected = """
                                items: 
                                - 1. 10$
                                - 2. 20$
                                - 3. 30$
                                - 4. 40$
                                - 5. 50$
                                """;

        var result = await _engine.RenderAsync(tpl, model);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UsesJsonInput_JObject_ParseWorks()
    {
        var model = JObject.Parse("""{ "name":"BMW", "speed":100 }""");
        const string tpl = "Brand: @(Model[\"name\"]) Speed: @{ return Model[\"speed\"].ToString(); }";

        var result = await _engine.RenderAsync(tpl, model);

        Assert.Equal("Brand: BMW Speed: 100", result);
    }
}