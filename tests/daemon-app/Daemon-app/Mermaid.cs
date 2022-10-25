using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Daemon_app
{
    public static class Mermaid
    {
        /// <summary>
        /// Display a mermaid diagram of the composition graph
        /// </summary>
        /// <param name="services"></param>
        public static void DisplayMermaidOfDiGraph(IServiceCollection services)
        {
            Console.WriteLine("```mermaid");
            Console.WriteLine("classDiagram");
            foreach (var s in services)
            {
                string interfaceName = GetMermaidName(s.ServiceType);
                Console.WriteLine($"class {interfaceName}");
                if (s.ServiceType.IsInterface)
                {
                    Console.WriteLine($"<<interface>> {interfaceName}");
                }
                if (s.ImplementationType != null)
                {
                    string implementation = GetMermaidName(s.ImplementationType);
                    Console.WriteLine($"class {implementation}");
                    //                    Console.WriteLine($"{interfaceName} <|-- {implementation}");
                    Console.WriteLine($"{interfaceName} <|.. {implementation}");

                    var constructor = s.ImplementationType.GetConstructors().FirstOrDefault();
                    foreach (var parameter in constructor.GetParameters())
                    {
                        string parameterTypeName = GetMermaidName(parameter.ParameterType);
                        Console.WriteLine($"class {parameterTypeName}");
                        Console.WriteLine($"    {implementation} --> {parameterTypeName}");
                    }
                }
            }
            Console.WriteLine("```");
        }

        /// <summary>
        /// Get the mermaid name of a type
        /// </summary>
        /// <param name="t"></param>
        /// <param name="mermaidSeparator"></param>
        /// <returns></returns>
        private static string GetMermaidName(Type t, bool mermaidSeparator = true)
        {
            if (t.Name.StartsWith("OptionMonitor"))
            {

            }
            if (t.IsGenericTypeDefinition)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(t.Name.Replace("`1", ""));
                if (mermaidSeparator)
                    sb.Append("~");
                else
                    sb.Append("-");
                sb.Append(string.Join(", ", t.GetGenericArguments().Select(genericType => GetMermaidName(genericType, false))));
                if (mermaidSeparator)
                    sb.Append("~");
                else
                    sb.Append("-");
                return sb.ToString();
            }
            else if (t.IsGenericType)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(t.Name.Replace("`1", ""));
                if (mermaidSeparator)
                    sb.Append("~");
                else
                    sb.Append("-");
                sb.Append(string.Join(", ", t.GetGenericArguments().Select(genericType => GetMermaidName(genericType, false))));
                if (mermaidSeparator)
                    sb.Append("~");
                else
                    sb.Append("-");
                return sb.ToString();

            }
            else
            {
                return t.Name.Replace("`1", "").Replace("[[", "~").Replace("]]", "~");
            }
        }
    }
}
