﻿#region

using System;
using OpenMod.API.Ioc;

#endregion

namespace OpenMod.Economy.API
{
    [Service]
    public interface IEconomyDispatcher
    {
        void Enqueue(Action action);
        void LoadDispatcher();
    }
}