using NuciLog.Core;
using NuciWeb.Automation;

using HoneygainPotOpener.Configuration;
using System;
using NuciWeb;

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

        public static string LogInUrl => $"{DashboardUrl}/login";

        public void LogIn()
        {
            webProcessor.GoToUrl(LogInUrl);

            AcceptCookiesIfNeeded();

            string emailAddressInputSelector = Select.ById("email");
            string passwordInputSelector = Select.ById("password");
            string logInButtonSelector = Select.ByXPath("//form/button");
            string trafficLegendContainerSelector = Select.ById("traffic-legend-container");

            webProcessor.SetText(emailAddressInputSelector, settings.EmailAddress);
            webProcessor.SetText(passwordInputSelector, settings.Password);

            webProcessor.Click(logInButtonSelector);
            webProcessor.WaitForElementToBeVisible(trafficLegendContainerSelector);
        }

        public void OpenPot()
        {
            throw new NotImplementedException();
        }

        private void AcceptCookiesIfNeeded()
        {
            string acceptSelectedCookiesButtonSelector = Select.ByXPath("//img[@alt='cookie consent']/../div/button[2]");

            webProcessor.WaitForElementToBeVisible(acceptSelectedCookiesButtonSelector);

            if (webProcessor.IsElementVisible(acceptSelectedCookiesButtonSelector))
            {
                webProcessor.Click(acceptSelectedCookiesButtonSelector);
            }
        }
    }
}
