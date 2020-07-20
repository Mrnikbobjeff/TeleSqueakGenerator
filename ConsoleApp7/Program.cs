using ConsoleApp7.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TdObject = TdLib.TdApi.Object;

namespace ConsoleApp7
{
    public readonly struct SmalltalkProperty : IEquatable<SmalltalkProperty>
    {
        public SmalltalkProperty(string name, string typeName)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        public string Name { get; }
        public string TypeName { get; }

        public override bool Equals(object obj)
        {
            return obj is SmalltalkProperty property &&
                   this.Equals(property);
        }

        public bool Equals([AllowNull] SmalltalkProperty other)
        {
            return (Name, TypeName) == (other.Name, other.TypeName);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, TypeName);
        }
    }
    public readonly struct SmalltalkClass : IEquatable<SmalltalkClass>
    {
        public SmalltalkClass(string baseTypeName, string className, SmalltalkProperty[] properties, string comment)
        {
            BaseTypeName = baseTypeName ?? throw new ArgumentNullException(nameof(baseTypeName));
            ClassName = className ?? throw new ArgumentNullException(nameof(className));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Comment = comment ?? throw new ArgumentNullException(nameof(comment));
        }

        public string BaseTypeName { get; }
        public string ClassName { get; }
        public SmalltalkProperty[] Properties { get; }
        public string Comment { get; }

        public bool Equals([AllowNull] SmalltalkClass other)
        {
            return (BaseTypeName, ClassName, Properties, Comment) == (other.BaseTypeName, other.ClassName, other.Properties, other.Comment);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BaseTypeName, ClassName, Properties, Comment);
        }
    }

    public static class ReflectionEx
    {
        public static bool AnyBaseTypeIs(this Type @this, Type baseType)
        {
            Type temp = @this ;
            while ((temp = temp.BaseType) != null)
            {
                if (temp == baseType)
                    return true;
            }
            return false;
        }
    }
    public class TdLibExporter : ISmalltalkTypeExporter
    {
        readonly Assembly assembly;

        public TdLibExporter(Assembly assembly)
        {
            this.assembly = assembly;
        }

        SmalltalkProperty ToSmalltalkProperty(PropertyInfo t)
        {
            var name = t.Name;
            var typeName = t.PropertyType.Name;
            return new SmalltalkProperty(name, typeName);
        }

        public IEnumerable<SmalltalkClass> GetExportedObjects()
        {
            return assembly
                .GetExportedTypes()
                .Where(type => type.BaseType.AnyBaseTypeIs(typeof(TdObject)))
                .Concat(new[] { typeof(TdObject) })
                .Select(v => Map(v));
        }

        SmalltalkClass Map(Type type)
        {
            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => ToSmalltalkProperty(x));
            var name = type.Name;
            if (type.IsAbstract)
                name = "Abstract" + name;
            if (type.IsGenericTypeDefinition)
                name = name + $"_{string.Join("_", type.GetGenericArguments().Select(t => t))}";
            string baseName = string.Empty;
            if (!type.BaseType.IsGenericType)
                baseName = type.BaseType.Name;
            else
            {
                var tempName = type.BaseType.Name;
                baseName = tempName.Substring(0, tempName.Length - 2);
                baseName += string.Join("_", type.BaseType.GetGenericArguments().Select(t => t.Name));
            }
            var comment = type.FullName;
            var c = new SmalltalkClass(baseName, name, properties.ToArray(), comment);
            return c;
        }
    }

    public class SmalltalkClassWriter
    {
        readonly string category;
        readonly Func<string, string> categoryResolver;
        readonly string outputDirectory;

        public SmalltalkClassWriter(string category, Func<string, string> methodCategoryResolver, string outputDirectory)
        {
            this.category = category;
            this.categoryResolver = methodCategoryResolver;
            this.outputDirectory = outputDirectory;
        }

        void CreateMethodPropertiesJson(string classDirectory, SmalltalkProperty[] properties)
        {
            var poco = new MethodPropertiesJson("N.S.", DateTimeOffset.UtcNow, properties);
            var methodPropertiesPath = Path.Combine(classDirectory, "methodProperties.json");
            File.WriteAllText(methodPropertiesPath, JsonSerializer.Serialize(poco));
        }

        void CreatePropertiesJson(string classDirectory, SmalltalkClass smalltalkClass)
        {
            var poco = new ClassPropertiesJson()
            {
                Category = category,
                Name = smalltalkClass.ClassName,
                Super = smalltalkClass.BaseTypeName,
                InstanceVariables = smalltalkClass.Properties.Select(t => t.Name).ToArray()//$"| {string.Join(" ", smalltalkClass.Properties.Select(t => t.Name))} |";
            };
            var methodPropertiesPath = Path.Combine(classDirectory, "properties.json");
            File.WriteAllText(methodPropertiesPath, JsonSerializer.Serialize(poco));
        }

        void CreatePropertyAccessors(string instanceDirectory, SmalltalkProperty[] properties)
        {
            foreach(var prop in properties)
            {
                var accessorFile = Path.Combine(instanceDirectory, prop.Name + ".st");
                var setterFile = Path.Combine(instanceDirectory, prop.Name + "..st");
                var sb = new StringBuilder(100);
                sb.AppendLine(categoryResolver("Translated"));
                sb.AppendLine($"{prop.Name}:a{prop.TypeName}");
                sb.Append("\t");
                sb.Append($"{prop.Name} := a{prop.TypeName}.");
                File.WriteAllText(setterFile, sb.ToString());
                sb.Clear();
                sb.AppendLine(categoryResolver("Translated"));
                sb.AppendLine($"{prop.Name}");
                sb.Append("\t");
                sb.Append($"^{prop.Name}.");
                File.WriteAllText(accessorFile, sb.ToString());
            }
        }

        public void ToFiles(SmalltalkClass[] smalltalkClasses)
        {
            foreach(var smalltalkClass in smalltalkClasses)
            {
                var classDirectory = Path.Combine(outputDirectory, smalltalkClass.ClassName + ".class");
                Directory.CreateDirectory(classDirectory);
                var instanceDirectory = Path.Combine(classDirectory, "instance");
                Directory.CreateDirectory(instanceDirectory);
                CreateMethodPropertiesJson(classDirectory, smalltalkClass.Properties);
                CreatePropertiesJson(classDirectory, smalltalkClass);
                CreatePropertyAccessors(instanceDirectory, smalltalkClass.Properties);
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var types = new TdLibExporter(typeof(TdObject).Assembly).GetExportedObjects();
            var exporter = new SmalltalkClassWriter("TelegramClient-Generated", (s) => s, "D:\\My Data\\Desktop\\SWT-2020.app\\Contents\\test");
            exporter.ToFiles(types.ToArray());
        }
    }
}
