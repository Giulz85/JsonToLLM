using System.Text;
using Westwind.Scripting;

namespace JsonToLLM.Westwind;

public interface ITemplateEngine
{
    Task<string> RenderAsync(string templateText, ExecContext context);
}

public class WestwindTemplateEngine : ITemplateEngine
{
    private readonly CSharpScriptExecution _scriptExec;

    public WestwindTemplateEngine()
    {
        _scriptExec = new CSharpScriptExecution
        {
            SaveGeneratedCode = false,
            ThrowExceptions = true
        };

        _scriptExec.AddLoadedReferences();
    }

    public async Task<string> RenderAsync(string template, ExecContext context)
    {
        var current = 0;
        var sb = new StringBuilder();

        var matches = ExpressionParser.Parse(template);

        foreach (var m in matches)
        {
            sb.Append(template.AsSpan(current, m.StartIndex - current));
           
            if (m.IsBlock)
                sb.Append(await ExecuteBlockExpressionAsync(m.Code, context));
            else
                sb.Append(await ExecuteInLineExpressionAsync(m.Code, context));
            
            current = m.EndIndex;
        }

        sb.Append(template.AsSpan(current));

        return sb.ToString();

    }

    private Task<string> ExecuteBlockExpressionAsync(string code, ExecContext context)
    {
        return _scriptExec.ExecuteMethodAsync<string>("public async Task<string> ExecuteCode(JsonToLLM.Westwind.ExecContext Context)" +
                                                      Environment.NewLine +
                                                      "{\n" +
                                                      code +
                                                      Environment.NewLine +

                                                      // force a return value
                                                      (!code.Contains("return ")
                                                          ? "return string.Empty;" + Environment.NewLine
                                                          : string.Empty) +
                                                      Environment.NewLine +
                                                      "}",
            "ExecuteCode", context);
    }

    private Task<string> ExecuteInLineExpressionAsync(string code, ExecContext context)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Can't evaluate empty code. Please pass code.");

        return ExecuteBlockExpressionAsync("return " + code + ";", context);
    }
}
