using JsonToLLM.Westwind;
using Newtonsoft.Json.Linq;

namespace JsonToLLM.Test;

public class WestwindTemplateEngineTests
{
    private readonly WestwindTemplateEngine _engine = new();

    [Fact]
    public async Task InlineExpression_RendersCorrectly()
    {
        var input = JObject.Parse("""{Name: "Alice"}""");
        var ctx = new ExecContext { Input = input };
        const string tpl = "Hello @(Context.Input.Name)!";

        var result = await _engine.RenderAsync(tpl, ctx);

        Assert.Equal("Hello Alice!", result);
    }

    [Fact]
    public async Task BlockExpression_ReturnsEmptyIfNoReturn()
    {
        var input = JObject.Parse("{X: 5}");
        var ctx = new ExecContext { Input = input };
        const string tpl = "Test @{ var y = Context.Input.X * 2; } Done";

        var result = await _engine.RenderAsync(tpl, ctx);

        Assert.Equal("Test  Done", result);
    }

    [Fact]
    public async Task BlockExpression_WithReturn_RendersResult()
    {
        var input = JObject.Parse("{X: 3}");
        var ctx = new ExecContext { Input = input };
        const string tpl = "Value @{ return (Context.Input.X + 4).ToString(); } Tail";

        var result = await _engine.RenderAsync(tpl, ctx);

        Assert.Equal("Value 7 Tail", result);
    }

    [Fact]
    public async Task MultipleExpressions_MixedInlineAndBlock()
    {
        var input = JObject.Parse("{A: 1, B: 2, C:3}");
        var ctx = new ExecContext { Input = input };
        const string tpl = "Start @(Context.Input.A) @{ return (Context.Input.B*2).ToString(); } End @(Context.Input.C)";

        var result = await _engine.RenderAsync(tpl, ctx);

        Assert.Equal("Start 1 4 End 3", result);
    }

    [Fact]
    public async Task MultipleExpressions_GenerateFromArray()
    {
        var input = JObject.Parse("""
                                  {items: [
                                    {no: 1, price: 10},
                                    {no: 2, price: 20},
                                    {no: 3, price: 30},
                                    {no: 4, price: 40},
                                    {no: 5, price: 50}
                                  ]}
                                  """);
        var ctx = new ExecContext { Input = input };
        
        const string tpl = """
                           items: 
                           @{ 
                            var sp = new StringBuilder();
                            foreach(var item in Context.Input.items){
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

        var result = await _engine.RenderAsync(tpl, ctx);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UsesJsonInput_JObject_ParseWorks()
    {
        var json = JObject.Parse("""{ "name":"BMW", "speed":100 }""");
        var ctx = new ExecContext { Input = json };
        const string tpl = "Brand: @(Context.Input[\"name\"]) Speed: @{ return Context.Input[\"speed\"].ToString(); }";

        var result = await _engine.RenderAsync(tpl, ctx);

        Assert.Equal("Brand: BMW Speed: 100", result);
    }

    [Fact]
    public async Task InlineEmptyCode_ThrowsArgumentException()
    {
        var ctx = new ExecContext { Input = new { } };
        await Assert.ThrowsAsync<ArgumentException>(() => _engine.RenderAsync("Hello @() End", ctx));
    }
}