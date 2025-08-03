using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JsonToLLM.CSharpScripting;

public class RoslynExecutor
{
    private const string GeneratedClassName = "ScriptHost";
    private const string GeneratedNamespace = "__ScriptExecution";

    private readonly string[] _defaultNamespaces =
    [
        "System",
        "System.Text",
        "System.Reflection",
        "System.Globalization",
        "System.IO",
        "System.Net",
        "System.Net.Http",
        "System.Collections",
        "System.Collections.Generic",
        "System.Collections.Concurrent",
        "System.Text.RegularExpressions",
        "System.Threading.Tasks",
        "System.Linq"
    ];

    private NamespaceList Namespaces { get; } = [];
    private ReferenceList References { get; } = [];

    public RoslynExecutor()
    {
        AddLoadedReferences();
        AddNamespace(GetType().Namespace!);
    }

    public TResult? ExecuteMethod<TResult>(string methodCode, string methodName, params object[] parameters)
    {
        var resultObj = ExecuteMethod(methodCode, methodName, parameters);

        if (resultObj is TResult result)
            return result;

        return default;
    }

    public async Task<TResult?> ExecuteMethodAsync<TResult>(string methodCode, string methodName, params object[] parameters)
    {
        var resultObj = ExecuteMethod(methodCode, methodName, parameters);

        if (resultObj is Task<TResult?> resultTask)
            return await resultTask;

        return default;
    }

    public object? ExecuteMethod(string methodCode, string methodName, params object[] parameters)
    {
        var assembly = CompileAssembly(GenerateClass(methodCode));

        var instance = assembly.CreateInstance($"{GeneratedNamespace}.{GeneratedClassName}");
        if (instance is null)
            throw new InvalidOperationException("Unable to locate the specified type from the created assembly while instance creation.");

        var resultObj = InvokeMethod(instance, methodName, parameters);

        return resultObj;
    }
    
    private static object? InvokeMethod(object instance, string method, params object[] parameters)
    {
        return instance
            .GetType()
            .InvokeMember(method, BindingFlags.InvokeMethod, null, instance, parameters);
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

    private Assembly CompileAssembly(string source)
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

    private void AddLoadedReferences()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .ToArray();

        foreach (var assembly in assemblies)
            AddAssembly(assembly.Location);

        AddAssemblies("Microsoft.CSharp.dll", "System.Linq.Expressions.dll", "System.Text.RegularExpressions.dll");
       
        AddNamespaces(_defaultNamespaces);
    }

    private void AddNamespace(string nameSpace)
    {
        ArgumentException.ThrowIfNullOrEmpty(
            nameSpace,
            nameof(nameSpace)
        );

        Namespaces.Add(nameSpace);
    }

    private void AddNamespaces(params string[] namespaces)
    {
        foreach (var nameSpace in namespaces)
                AddNamespace(nameSpace);
    }

    /// <summary>
    /// Adds a MetadataReference for the specified assembly DLL.
    /// </summary>
    /// <param name="assemblyFileNameOrPath">
    /// Relative or absolute path to the assembly DLL (e.g. "Newtonsoft.Json.dll" or "C:\libs\Newtonsoft.Json.dll").
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when input is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the file cannot be found in either the provided path or the application's base directory.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a duplicate assembly is added or metadata cannot be created.
    /// </exception>
    private void AddAssembly(string assemblyFileNameOrPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(
            assemblyFileNameOrPath,
            nameof(assemblyFileNameOrPath)
        );

        // Resolve full path
        var filePath = Path.GetFullPath(assemblyFileNameOrPath);
        if (!File.Exists(filePath))
        {
            // Try default probing location (same dir as mscorlib)
            var basePath = Path.GetDirectoryName(typeof(object).Assembly.Location)
                           ?? Environment.CurrentDirectory;
            filePath = Path.Combine(basePath, assemblyFileNameOrPath);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"Unable to locate assembly '{assemblyFileNameOrPath}'. " +
                    $"Probed:\n  - {Path.GetFullPath(assemblyFileNameOrPath)}\n" +
                    $"  - {filePath}"
                );
            }
        }

        // Check for duplicate
        if (References.Any(r => string.Equals(r.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
            return;
        
        try
        {
            var reference = MetadataReference.CreateFromFile(filePath);
            References.Add(reference);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to add MetadataReference for '{filePath}': {ex.Message}",
                ex
            );
        }
    }

    private void AddAssemblies(params string[] assemblies)
    {
        foreach (var assembly in assemblies)
            AddAssembly(assembly);
    }
}

/// <summary>
/// HashSet of namespaces
/// </summary>
public class NamespaceList : HashSet<string>
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var ns in this)
            sb.AppendLine($"using {ns};");
        
        return sb.ToString();
    }
}

/// <summary>
/// HashSet of References
/// </summary>
public class ReferenceList : HashSet<PortableExecutableReference>;
