using System;
using System.Text.RegularExpressions;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenMod.Economy.Models;

namespace OpenMod.Economy.Extensions;

public static class ConfigurationHelper
{
    private static readonly Regex s_FieldRegex =
        new(@"Failed to convert configuration value at '(?<field>\S*)' to type '(?<type>\S*)'");

    private static readonly Regex s_ValueRegex = new(@"(?<value>\S*) is not a valid value for (?<type>\S*)\.");

    public static void RegisterConfig<TConfig>(this ContainerBuilder containerBuilder, string? keyName = null)
        where TConfig : notnull, new()
    {
        containerBuilder
            .Register<IConfiguration, ILogger<TConfig>, TConfig>((configuration, logger) =>
                configuration.CreateAndBind<TConfig>(logger, keyName))
            .As<TConfig>()
            .SingleInstance();
    }

    private static TConfig CreateAndBind<TConfig>(this IConfiguration configuration, ILogger logger,
        string? keyName = null)
        where TConfig : notnull, new()
    {
        if (string.IsNullOrEmpty(keyName))
        {
            var configType = typeof(TConfig);
            if (configType == typeof(DatabaseSettings))
                keyName = "database";

            else if (configType == typeof(EconomySettings))
                keyName = "economy";

            else
                keyName = typeof(TConfig).Name;
        }

        var instance = new TConfig();
#if NETSTANDARD2_1_OR_GREATER
        configuration.BindConfig(keyName, instance, logger);
#else
        configuration.BindConfig(keyName!, instance, logger);
#endif
        return instance;
    }

    public static void BindConfig(this IConfiguration configuration, string keyName, object instance, ILogger logger)
    {
        try
        {
            configuration.Bind(keyName, instance);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.InnerException is not FormatException formatException)
                throw;

            var fieldMatch = s_FieldRegex.Match(ex.Message);
            if (!fieldMatch.Success)
                throw;

            var valueMatch = s_ValueRegex.Match(formatException.Message);
            logger.LogError("Invalid config found, option {0} with value {1}", fieldMatch.Groups["field"],
                valueMatch.Success ? valueMatch.Groups["value"] : "??");

            var tp = typeof(DatabaseSettings).Assembly.GetType(fieldMatch.Groups["type"].Value);
            if (tp.IsEnum)
                logger.LogWarning("Valid values: {0}", Enum.GetValues(tp));
        }
    }
}