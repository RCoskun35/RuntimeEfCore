using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Linq.Dynamic.Core;

namespace RuntimeEfCore
{
    public static class DynamicContextExtensions
    {
        public static IQueryable Query(this DbContext context, string entityName) =>
            context.Query(entityName, context.Model.FindEntityType(entityName).ClrType);

        static readonly MethodInfo SetMethod =
            typeof(DbContext).GetMethod(nameof(DbContext.Set), 1, new[] { typeof(string) }) ??
            throw new Exception($"Type not found: DbContext.Set");
        
        public static IQueryable Query(this DbContext context, string entityName, Type entityType) =>
            (IQueryable)SetMethod.MakeGenericMethod(entityType)?.Invoke(context, new[] { entityName }) ??
            throw new Exception($"Type not found: {entityType.FullName}");
        public static void StartHttpServer()
        {
            HttpListener listener = new HttpListener();
            string url = "http://localhost:8090/";

            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine($"HTTP sunucusu {url} adresinde çalışıyor...");

            _ = HandleRequests(listener); // İstekleri asenkron olarak işle
        }
        static async Task HandleRequests(HttpListener listener)
        {
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                try
                {
                    var request = context.Request;
                    var response = context.Response;

                    Console.WriteLine($"İstek alındı: {request.HttpMethod} {request.RawUrl}");

                    if (request.HttpMethod == "GET")
                    {
                        var entityName = request.RawUrl.Split('?')[0].Replace("/", "");
                        var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);

                        var entities = GetEntity(entityName).AsQueryable();

                        // ODATA destekli sorgu işlemleri
                        entities = (IQueryable<object>)ApplyODataQuery(entities, query);

                        string responseString = JsonConvert.SerializeObject(entities);
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                        response.ContentType = "application/json; charset=utf-8";
                        response.ContentEncoding = System.Text.Encoding.UTF8;
                        response.ContentLength64 = buffer.Length;

                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        response.OutputStream.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata: {ex.Message}");

                    var response = context.Response;
                    response.StatusCode = 400; // HTTP 400 Bad Request
                    response.ContentType = "application/json; charset=utf-8";

                    string errorResponse = JsonConvert.SerializeObject(new { error = ex.Message });
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(errorResponse);

                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
            }
        }
        // ...

        public static IQueryable ApplyODataQuery(IQueryable entities, System.Collections.Specialized.NameValueCollection query)
        {
            // $filter
            if (query["$filter"] != null)
            {
                entities = entities.Where(query["$filter"]);
                var list = new List<object>();
                
            }

            // $orderby
            if (query["$orderby"] != null)
            {
                entities = entities.OrderBy(query["$orderby"]);
            }

            // $skip
            if (query["$skip"] != null && int.TryParse(query["$skip"], out int skip))
            {
                entities = entities.Skip(skip);
            }

            // $top
            if (query["$top"] != null && int.TryParse(query["$top"], out int top))
            {
                entities = entities.Take(top);
            }

            return entities;
        }

        public static IEnumerable<object> GetEntity(string entityName)
        {
            entityName = "TypedDataContext.Models." + entityName;
            var entityTypes = dynamicContext.Model.GetEntityTypes();
            if (entityTypes.All(e => e.Name != entityName))
            {
                throw new Exception($"Entity type: {entityName} not found");
            }
            var items = (IQueryable<object>)dynamicContext.Query(entityName);
            return items.ToList();
        }

        public static DbContext dynamicContext;

        public static bool enableLazyLoading;
        public static AssemblyLoadContext assemblyLoadContext;
        public static void DynamicContext()
        {
            MemoryStream peStream;
            var connectionString = "Data Source=ISMAIL\\SQLEXPRESS;Initial Catalog=MyCityTransportMainDb;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

            var scaffolder = CreateMssqlScaffolder();

            var dbOpts = new DatabaseModelFactoryOptions();
            var modelOpts = new ModelReverseEngineerOptions();
            var codeGenOpts = new ModelCodeGenerationOptions()
            {
                RootNamespace = "TypedDataContext",
                ContextName = "DataContext",
                ContextNamespace = "TypedDataContext.Context",
                ModelNamespace = "TypedDataContext.Models",
                SuppressConnectionStringWarning = true
            };

            var scaffoldedModelSources = scaffolder.ScaffoldModel(connectionString, dbOpts, modelOpts, codeGenOpts);
            var sourceFiles = new List<string> { scaffoldedModelSources.ContextFile.Code };
            sourceFiles.AddRange(scaffoldedModelSources.AdditionalFiles.Select(f => f.Code));
            peStream = new MemoryStream();
            enableLazyLoading = false;
            var result = GenerateCode(sourceFiles, enableLazyLoading).Emit(peStream);

            if (!result.Success)
            {
                var failures = result.Diagnostics
                    .Where(diagnostic => diagnostic.IsWarningAsError ||
                                         diagnostic.Severity == DiagnosticSeverity.Error);

                var error = failures.FirstOrDefault();
                throw new Exception($"{error?.Id}: {error?.GetMessage()}");
            }

            assemblyLoadContext = new AssemblyLoadContext("DbContext", isCollectible: !enableLazyLoading);
            peStream.Seek(0, SeekOrigin.Begin);
            var assembly = assemblyLoadContext.LoadFromStream(peStream);

            var type = assembly.GetType("TypedDataContext.Context.DataContext");
            _ = type ?? throw new Exception("DataContext type not found");

            var constr = type.GetConstructor(Type.EmptyTypes);
            _ = constr ?? throw new Exception("DataContext ctor not found");

            dynamicContext = (DbContext)constr.Invoke(null);
            var entityTypes = dynamicContext.Model.GetEntityTypes();
            Console.WriteLine($"Context contains {entityTypes.Count()} types");

            foreach (var entityType in entityTypes)
            {
                var items = (IQueryable<object>)dynamicContext.Query(entityType.Name);

                Console.WriteLine($"Entity type: {entityType.Name} contains {items.Count()} items");
            }
            StartHttpServer();
        }

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "We need it")]
        public static IReverseEngineerScaffolder CreateMssqlScaffolder() =>
            new ServiceCollection()
               .AddEntityFrameworkSqlServer()
               .AddLogging()
               .AddEntityFrameworkDesignTimeServices()
               .AddSingleton<LoggingDefinitions, SqlServerLoggingDefinitions>()
               .AddSingleton<IRelationalTypeMappingSource, SqlServerTypeMappingSource>()
               .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>()
               .AddSingleton<IDatabaseModelFactory, SqlServerDatabaseModelFactory>()
               .AddSingleton<IProviderConfigurationCodeGenerator, SqlServerCodeGenerator>()
               .AddSingleton<IScaffoldingModelFactory, RelationalScaffoldingModelFactory>()
               .AddSingleton<IPluralizer, Bricelam.EntityFrameworkCore.Design.Pluralizer>()
               .AddSingleton<ProviderCodeGeneratorDependencies>()
               .AddSingleton<AnnotationCodeGeneratorDependencies>()
               .BuildServiceProvider()
               .GetRequiredService<IReverseEngineerScaffolder>();


        public static List<MetadataReference> CompilationReferences(bool enableLazyLoading)
        {
            var refs = new List<MetadataReference>();
            var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            refs.AddRange(referencedAssemblies.Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(BackingFieldAttribute).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));

            if (enableLazyLoading)
            {
                refs.Add(MetadataReference.CreateFromFile(typeof(ProxiesExtensions).Assembly.Location));
            }

            return refs;
        }

        public static CSharpCompilation GenerateCode(List<string> sourceFiles, bool enableLazyLoading)
        {
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10);

            var parsedSyntaxTrees = sourceFiles.Select(f => SyntaxFactory.ParseSyntaxTree(f, options));

            return CSharpCompilation.Create($"DataContext.dll",
                parsedSyntaxTrees,
                references: CompilationReferences(enableLazyLoading),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }
    }
}
