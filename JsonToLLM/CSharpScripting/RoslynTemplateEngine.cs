using System.Text;
using Newtonsoft.Json.Linq;

namespace JsonToLLM.CSharpScripting;

public interface ITemplateEngine
{
    Task<string> RenderAsync(string templateText, JObject model);
}

public class RoslynTemplateEngine(RoslynExecutor roslynExecutor) : ITemplateEngine
{
    public async Task<string> RenderAsync(string template, JObject model)
    {
        var current = 0;
        var sb = new StringBuilder();

        var matches = RoslynTemplateParser.Parse(template);

        foreach (var m in matches)
        {
            sb.Append(template.AsSpan(current, m.StartIndex - current));

            if (m.IsBlock)
                sb.Append(await ExecuteBlockExpressionAsync(m.Code, model));
            else
                sb.Append(await ExecuteInLineExpressionAsync(m.Code, model));

            current = m.EndIndex;
        }

        sb.Append(template.AsSpan(current));

        return sb.ToString();

    }

    private async Task<object?> ExecuteBlockExpressionAsync(string code, JObject model)
    {
        const string methodName = "ExecuteCode";
        var isAsyncCode = code.Contains("await ");
        var methodReturnType = isAsyncCode ? "async Task<object?>" : "object?";

        var methodCode = $"public {methodReturnType} {methodName}(dynamic Model)" +
                         Environment.NewLine +
                         "{" +
                         Environment.NewLine +
                         code +
                         Environment.NewLine +

                         // force a return value
                         (!code.Contains("return ")
                             ? "return default;" + Environment.NewLine
                             : string.Empty) +
                         Environment.NewLine +
                         "}";

        return isAsyncCode ? 
            await roslynExecutor.ExecuteMethodAsync<object?>(methodCode, methodName, model) :
            roslynExecutor.ExecuteMethod<object?>(methodCode, methodName, model);
    }

    private Task<object?> ExecuteInLineExpressionAsync(string code, JObject model)
    {
        return string.IsNullOrEmpty(code) ?
            Task.FromResult<object?>(null) :
            ExecuteBlockExpressionAsync("return " + code + ";", model);
    }

}
