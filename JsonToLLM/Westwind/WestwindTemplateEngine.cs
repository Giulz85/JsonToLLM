using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Westwind.Scripting;

namespace JsonToLLM.Westwind;

public interface ITemplateEngine
{
    Task<string> RenderAsync(string templateText, ExecContext context);
}

public class WestwindTemplateEngine : ITemplateEngine
{
    private const string GeneratedClassName = "ScriptHost";
    private const string GeneratedNamespace = "__ScriptExecution";

    public NamespaceList Namespaces { get; } = [];
    public ReferenceList References { get; } = [];

    private readonly string[] _defaultNamespaces =
    [
        "System",
        "System.Text",
        "System.Reflection",
        "System.IO",
        "System.Net",
        "System.Net.Http",
        "System.Collections",
        "System.Collections.Generic",
        "System.Collections.Concurrent",
        "System.Text.RegularExpressions",
        "System.Threading.Tasks",
        "System.Linq",
        "Westwind.Scripting"
    ];

    public WestwindTemplateEngine()
    {
        AddLoadedReferences();
        AddNamespace(GetType().Namespace!);
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

    private Task<object?> ExecuteBlockExpressionAsync(string code, ExecContext context)
    {
        return ExecuteMethodAsync<object?>("public async Task<object?> ExecuteCode(ExecContext Context)" +
                                          Environment.NewLine +
                                          "{\n" +
                                          code +
                                          Environment.NewLine +

                                          // force a return value
                                          (!code.Contains("return ")
                                              ? "return default;" + Environment.NewLine
                                              : string.Empty) +
                                          Environment.NewLine +
                                          "}",
            context);
    }

    private Task<object?> ExecuteInLineExpressionAsync(string code, ExecContext context)
    {
        return string.IsNullOrEmpty(code) ?
            Task.FromResult<object?>(null) :
            ExecuteBlockExpressionAsync("return " + code + ";", context);
    }

    public async Task<T> ExecuteMethodAsync<T>(string methodCode, ExecContext context)
    {
        var assembly = CompileAssembly(GenerateClass(methodCode));


        // Add binder and dynamic runtime references explicitly
        //var binder = typeof(CSharpArgumentInfo).Assembly;
        //assemblies.Add(MetadataReference.CreateFromFile(binder.Location));

        //var dynamicRuntime = typeof(System.Dynamic.DynamicObject).Assembly;
        //assemblies.Add(MetadataReference.CreateFromFile(dynamicRuntime.Location));
        this.ObjectInstance = this.Assembly.CreateInstance($"{this.GeneratedNamespace}.{this.GeneratedClassName}");
        try
        {
            var type = assembly.GetType("ScriptHost");
            var method = type!.GetMethod("InvokeAsync", BindingFlags.Public | BindingFlags.Static);
            var task = (Task<T>)method!.Invoke(null, [context])!;
            var value = await task;
            return value;
        }
        finally
        {
            alc.Unload();
        }
    }

    public void AddLoadedReferences()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .ToArray();

        foreach (var assembly in assemblies)
            AddAssembly(assembly.Location);

        AddAssembly("Microsoft.CSharp.dll");
        AddAssemblies("System.Linq.Expressions.dll", "System.Text.RegularExpressions.dll");
        AddNamespaces(_defaultNamespaces);
    }

    public void AddNamespaces(params string[] namespaces)
    {
        foreach (var nameSpace in namespaces)
            if (!string.IsNullOrEmpty(nameSpace))
                AddNamespace(nameSpace);
    }

    public void AddNamespace(string nameSpace)
    {
        if (string.IsNullOrWhiteSpace(nameSpace))
            return;

        Namespaces.Add(nameSpace);
    }

    public bool AddAssembly(string assemblyDll)
    {
        if (string.IsNullOrEmpty(assemblyDll))
            return false;
        var file = Path.GetFullPath(assemblyDll);
        if (!File.Exists(file))
        {
            file = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty, assemblyDll);
            if (!File.Exists(file))
                return false;
        }
        if (References.Any<PortableExecutableReference>(r => r.FilePath == file))
            return true;
        try
        {
            References.Add(MetadataReference.CreateFromFile(file));
        }
        catch
        {
            return false;
        }
        return true;
    }

    public void AddAssemblies(params string[] assemblies)
    {
        foreach (var assembly in assemblies)
            AddAssembly(assembly);
    }

    private string GenerateClass(string classBody)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(Namespaces.ToString());
        stringBuilder.Append($"namespace {GeneratedNamespace} {{{Environment.NewLine}{Environment.NewLine}public class {GeneratedClassName}{Environment.NewLine}{{ {Environment.NewLine}{Environment.NewLine}");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(classBody);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"}} {Environment.NewLine}}}");

        return stringBuilder.ToString();
    }

    public Assembly CompileAssembly(string source)
    {
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(source.Trim());
        var csharpCompilation = CSharpCompilation
            .Create(GeneratedClassName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release))
            .AddReferences(References)
            .AddSyntaxTrees(syntaxTree);

        using var ms = new MemoryStream();

        var emitResult = csharpCompilation.Emit(ms);

        if (!emitResult.Success)
        {
            var stringBuilder = new StringBuilder();
            foreach (var diagnostic in emitResult.Diagnostics)
                stringBuilder.AppendLine(diagnostic.ToString());

            throw new InvalidOperationException($"Compilation failed:{Environment.NewLine}{stringBuilder}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var alc = new AssemblyLoadContext(null, isCollectible: true);

        return alc.LoadFromStream(ms);
    }

    public object InvokeMethod(object instance, string method, params object[] parameters)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return instance
            .GetType()
            .InvokeMember(method, BindingFlags.InvokeMethod, (System.Reflection.Binder)null, instance, parameters);
    }
}
