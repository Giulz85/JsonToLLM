
namespace JsonToLLM.CSharpScripting;

public class RoslynTemplateParse
{
    public class ExpressionMatch
    {
        public bool IsBlock { get; set; }  // true for @{...}, false for @(...)
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }  // exclusive
        public required string Code { get; set; }
    }

    public static List<ExpressionMatch> Parse(string template)
    {
        var result = new List<ExpressionMatch>();
        var len = template.Length;

        for (var i = 0; i < len - 1; i++)
        {
            if (template[i] == '@' && (template[i + 1] == '(' || template[i + 1] == '{'))
            {
                var isBlock = template[i + 1] == '{';
                var open = isBlock ? '{' : '(';
                var close = isBlock ? '}' : ')';
                var j = i + 2;
                var depth = 1;
                var inString = false;
                var stringDelimiter = '\0';

                while (j < len && depth > 0)
                {
                    var c = template[j];

                    if (inString)
                    {
                        if (c == '\\' && j + 1 < len)
                        {
                            j += 2;
                            continue;
                        }
                        if (c == stringDelimiter)
                        {
                            inString = false;
                        }
                    }
                    else
                    {
                        if (c == '"' || c == '\'')
                        {
                            inString = true;
                            stringDelimiter = c;
                        }
                        else if (c == open)
                        {
                            depth++;
                        }
                        else if (c == close)
                        {
                            depth--;
                        }
                    }

                    j++;
                }

                if (depth == 0)
                {
                    var code = template.Substring(i + 2, j - (i + 2) - 1);
                    result.Add(new ExpressionMatch
                    {
                        IsBlock = isBlock,
                        StartIndex = i,
                        EndIndex = j,
                        Code = code
                    });
                    i = j - 1;
                }
                else
                {
                    throw new InvalidOperationException($"Unterminated expression starting at position {i}");
                }
            }
        }

        return result;
    }
}
