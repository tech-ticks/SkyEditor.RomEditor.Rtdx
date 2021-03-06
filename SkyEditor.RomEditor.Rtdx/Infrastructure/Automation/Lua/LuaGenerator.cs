﻿using Autofac.Core;
using System;
using System.Reflection;
using System.Text;

namespace SkyEditor.RomEditor.Infrastructure.Automation.Lua
{
    public interface ILuaGenerator : IScriptGenerator
    {
    }

    public class LuaGenerator : ILuaGenerator
    {
        public LuaGenerator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Generates a simple script to modify a simple object
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="source">The unmodified object to be used as a baseline</param>
        /// <param name="modified">The modified object</param>
        /// <param name="variableName">Name of the Lua variable</param>
        /// <param name="indentLevel">Level of indentation to apply to each line</param>
        /// <returns>A lua script segment</returns>
        public string GenerateSimpleObjectDiff<T>(T source, T modified, string variableName, int indentLevel)
        {
            var script = new StringBuilder();
            var indent = GenerateIndentation(indentLevel);

            var type = typeof(T);
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                var sourceValue = property.GetValue(source);
                var targetValue = property.GetValue(modified);
                if (sourceValue != null && sourceValue.Equals(targetValue))
                {
                    continue;
                }

                script.Append(indent);
                script.Append(variableName);
                script.Append(".");
                script.Append(property.Name);
                script.Append(" = ");
                script.Append(GenerateExpression(targetValue));
                script.AppendLine();
            }
            return script.ToString();
        }

        /// <summary>
        /// Represent the given value as a Lua expression
        /// </summary>
        public string GenerateExpression(object? value)
        {
            if (value is null)
            {
                return "nil";
            }
            else if (value is bool valueBool)
            {
                return valueBool ? "true" : "false";
            }
            else if (value is string)
            {
                return $"\"{value}\"";
            }
            else if (value is byte 
                || value is short 
                || value is ushort 
                || value is int 
                || value is uint 
                || value is long 
                || value is ulong
                || value is float
                || value is double)
            {
                return value.ToString()!;
            }
            else
            {
                var type = value.GetType();
                if (type.IsGenericType && type.GetGenericTypeDefinition() == NullableType)
                {
                    var hasValue = (bool) (type.GetProperty("HasValue")?.GetValue(value) ?? false);
                    if (hasValue)
                    {
                        return GenerateExpression(type.GetProperty("Value")?.GetValue(value));
                    }
                    else
                    {
                        return GenerateExpression(null);
                    }
                }
                var converterAttribute = type.GetCustomAttribute<LuaExpressionGeneratorAttribute>();
                if (converterAttribute != null)
                {
                    var generatorType = converterAttribute.Generator;
                    var converter = (ILuaExpressionGenerator)serviceProvider.GetService(generatorType)!;
                    return converter.Generate(value);
                }
            }

            throw new ArgumentException($"Unsupported variable type: {value.GetType().FullName}");
        }
        private static readonly Type NullableType = typeof(Nullable<>);

        public static string GenerateIndentation(int indentLevel)
        {
            // Spaces only (sorry tabs gang)
            // Probably best to add support for tabs here later to avoid flame wars
            var data = new byte[4 * indentLevel];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0x20;
            }
            return Encoding.ASCII.GetString(data);
        }
    }
}
