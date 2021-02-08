#region

using System;
using Microsoft.Extensions.Localization;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;

#endregion

[assembly:
    PluginMetadata("OpenMod.Economy", Author = "OpenMod,Rube200", DisplayName = "Openmod.Economy",
        Website = "https://github.com/openmodplugins/OpenMod.Economy")]

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
