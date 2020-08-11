#region

using System;
using Microsoft.Extensions.Localization;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;

#endregion

[assembly: PluginMetadata("Openmod.Economy", Author = "OpenMod", DisplayName = "Openmod.Economy")]

namespace OpenMod.Economy
{
    public sealed class Economy : OpenModUniversalPlugin
    {
        internal readonly IStringLocalizer StringLocalizer;

        public Economy(IServiceProvider serviceProvider, IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            StringLocalizer = stringLocalizer;
        }
    }
}