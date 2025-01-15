using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Bricelam.EntityFrameworkCore.Design;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace RuntimeEfCoreWeb
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
       

        public static IQueryable<object> GetEntity(string entityName)
        {
            entityName = "TypedDataContext.Models." + entityName;
            var entityTypes = dynamicContext.Model.GetEntityTypes();
            if (entityTypes.All(e => e.Name != entityName))
            {
                throw new Exception($"Entity type: {entityName} not found");
            }
            var items = (IQueryable<object>)dynamicContext.Query(entityName);
            return items; // IQueryable döndürülüyor
        }

        public static DbContext dynamicContext;

        public static bool enableLazyLoading;
        public static AssemblyLoadContext assemblyLoadContext;
        public static void DynamicContext(string connectionString)
        {
            MemoryStream peStream;
            

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

            //foreach (var entityType in entityTypes)
            //{
            //    var items = (IQueryable<object>)dynamicContext.Query(entityType.Name);

            //    Console.WriteLine($"Entity type: {entityType.Name} contains {items.Count()} items");
            //}
        }

        public static IEdmModel GetDynamicEdmModel(DbContext context)
        {
            var builder = new ODataConventionModelBuilder();

            foreach (var entityType in context.Model.GetEntityTypes())
            {
                // Varlık türünü alın
                var clrType = entityType.ClrType;

                // OData'da varlık seti oluştur
                var entitySet = builder.AddEntitySet(entityType.Name.Replace("TypedDataContext.Models.", ""), builder.AddEntityType(clrType));
                foreach (var property in entityType.GetProperties())
                {
                    // Tüm özellikleri EDM modeline ekle
                    entitySet.EntityType.AddProperty(property.PropertyInfo);
                }
                // Birincil anahtar kontrolü
                var primaryKey = entityType.FindPrimaryKey();
                if (primaryKey == null)
                {
                    throw new InvalidOperationException($"The entity '{entityType.Name}' does not have a primary key defined.");
                }

                // Birincil anahtarı OData modeline ekle
                foreach (var property in primaryKey.Properties)
                {
                    entitySet.EntityType.HasKey(property.PropertyInfo);
                }
            }

            return builder.GetEdmModel();
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
               .AddSingleton<IPluralizer, Pluralizer>()
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
