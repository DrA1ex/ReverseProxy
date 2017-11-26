using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ReverseProxy.Common.Utils
{
    public static class ConfigUtils
    {
        private static IConfiguration Config { get; } = new ConfigurationBuilder()
            .AddJsonFile("config.json")
            .Build();

        public static string GetString(string parameterName)
        {
            var value = Config[parameterName];
            if(value == null)
            {
                throw new KeyNotFoundException($"{parameterName} configuration parameter is not set");
            }
            return value;
        }

        public static string TryGetString(string parameterName)
        {
            var value = Config[parameterName];
            return value;
        }

        public static int GetInt(string parameterName)
        {
            var valueString = Config[parameterName];
            if(string.IsNullOrEmpty(valueString))
            {
                throw new KeyNotFoundException($"{parameterName} configuration parameter is not set");
            }

            if(!int.TryParse(valueString, out var value))
            {
                throw new KeyNotFoundException(
                    $"Cannot convert value of parameter '{parameterName}' to int: {valueString}");
            }

            return value;
        }
    }
}