using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace GenerateMergeOptionsMethods
{
    class Program
    {
        static void Main(string[] args)
        {
            GenerateMergeMethod(typeof(MergedOptions), typeof(MicrosoftAuthenticationOptions));
            return;

            GenerateMergeMethod(typeof(MergedOptions), typeof(MicrosoftIdentityOptions));
            GenerateMergeMethod(typeof(MergedOptions), typeof(ConfidentialClientApplicationOptions));
            GenerateMergeMethod(typeof(MergedOptions), typeof(JwtBearerOptions));
            GenerateMergeMethod(typeof(ConfidentialClientApplicationOptions), typeof(MergedOptions), true);

            GenerateCommonProperties(typeof(MergedOptions), typeof(JwtBearerOptions));

            GenerateMergeMethod(typeof(JwtBearerMergedOptions), typeof(JwtBearerOptions));
            GenerateMergeMethod(typeof(JwtBearerMergedOptions), typeof(ConfidentialClientApplicationOptions));
            GenerateMergeMethod(typeof(ConfidentialClientApplicationOptions), typeof(JwtBearerMergedOptions));
        }

        private static void GenerateCommonProperties(Type type1, Type type2)
        {
            var type1Properties = type1
                 .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                 .OrderBy(p => p.Name);
            var type2Properties = type2
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(p => p.Name);

            foreach (PropertyInfo p in type1Properties)
            {
                if (type2Properties.Any(property => property.Name == p.Name))
                {
                    if (p.CanWrite)
                    {
                        Console.WriteLine($"{p.PropertyType.Name} {p.Name}" + " {get;set;}");
                    }
                }
            }
        }

        private static void GenerateMergeMethod(Type toType, Type fromType, bool showMissingDestination = false)
        {
            string fromTypeVariableName = char.ToLower(fromType.Name.First(), CultureInfo.InvariantCulture).ToString() + fromType.Name.Substring(1);
            string toTypeVariableName = char.ToLower(toType.Name.First(), CultureInfo.InvariantCulture).ToString() + toType.Name.Substring(1);

            Console.WriteLine($"public static void Update{toType.Name}From{fromType.Name}({fromType.Name} {fromTypeVariableName}, {toType.Name} {toTypeVariableName})");
            Console.WriteLine("{");

            GenerateAssignments(fromType, fromTypeVariableName, toType, toTypeVariableName, showMissingDestination);
            Console.WriteLine("}");
        }

        private static void GenerateAssignments(Type fromType, string fromTypeVariableName, Type toType, string toTypeVariableName, bool showMissingDestination)
        {
            var toTypeProperties = toType
                 .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                 .OrderBy(p => p.Name);
            var fromTypeProperties = fromType
                 .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                 .OrderBy(p => p.Name);
            foreach (PropertyInfo p in toTypeProperties)
            {
                if (fromTypeProperties.Any(property => property.Name == p.Name))
                {
                    if (p.CanWrite)
                    {
                        if (p.PropertyType == typeof(string))
                        {
                            Console.WriteLine($"    if (string.IsNullOrEmpty({toTypeVariableName}.{p.Name}) && !string.IsNullOrEmpty({fromTypeVariableName}.{p.Name}))");
                            Console.WriteLine("    {");
                            Console.WriteLine($"      {toTypeVariableName}.{p.Name} = {fromTypeVariableName}.{p.Name};");
                            Console.WriteLine("    }");
                            Console.WriteLine("");
                        }
                        else if (p.PropertyType.IsValueType)
                        {
                            Console.WriteLine($"    {toTypeVariableName}.{p.Name} = {fromTypeVariableName}.{p.Name};");
                        }
                        else
                        {
                            Console.WriteLine($"    {toTypeVariableName}.{p.Name} ??= {fromTypeVariableName}.{p.Name};");
                        }
                    }
                }
                else
                {
                    if (showMissingDestination)
                    {
                        Console.WriteLine($"//    {p.PropertyType.Name} {p.Name}" + " {get;set;}");
                    }
                }
            }
        }


    }
}
