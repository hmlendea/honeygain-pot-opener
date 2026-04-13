using System.Collections.Generic;

using NuciLog.Core;
using NuciWeb.Automation;

using HoneygainPotOpener.Logging;
using HoneygainPotOpener.Configuration;
using System;

namespace HoneygainPotOpener.Processors.HoneygainProcessor
{
    public sealed class HoneygainProcessor(
        IWebProcessor webProcessor,
        BotSettings settings,
        ILogger logger)
        : IHoneygainProcessor
    {
        public static string HomePageUrl => "https://honeygain.com";

        public static string DashboardUrl => "https://dashboard.honeygain.com";

        public void LogIn()
        {
            throw new NotImplementedException();
        }

        public void OpenPot()
        {
            throw new NotImplementedException();
        }
    }
}
