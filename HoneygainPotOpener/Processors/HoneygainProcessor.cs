using System;

using NuciWeb;
using NuciWeb.Automation;

using HoneygainPotOpener.Configuration;

namespace HoneygainPotOpener.Processors
{
    public sealed class HoneygainProcessor(
        IWebProcessor webProcessor,
        BotSettings settings)
        : IHoneygainProcessor
    {
        public static string HomePageUrl => "https://honeygain.com";

        public static string DashboardUrl => "https://dashboard.honeygain.com";

        public static string LogInUrl => $"{DashboardUrl}/login";

        private static string EmailAddressInputSelector => Select.ById("email");

        private static string PasswordInputSelector => Select.ById("password");

        private static string LogInButtonSelector => Select.ByXPath("//form/button");

        private static string DashboardReadySelector => Select.ById("traffic-legend-container");

        private static string CookiesConsentButtonSelector
            => Select.ByXPath("//img[@alt='cookie consent']/../div/button[2]");

        private static string ClaimRewardButtonSelector
            => Select.ByXPath("//span[text()='Claim reward']/../../../../../button");

        private static string OpenPotButtonSelector
            => Select.ByXPath("//span[text()='Open Lucky Pot']/../../..");

        public void LogIn()
        {
            webProcessor.GoToUrl(LogInUrl);

            AcceptCookiesIfNeeded();

            EnterCredentials();

            webProcessor.Click(LogInButtonSelector);
            webProcessor.WaitForElementToBeVisible(DashboardReadySelector);
        }

        public void ClaimReward()
        {
            if (!webProcessor.IsElementVisible(ClaimRewardButtonSelector))
            {
                throw new InvalidOperationException(
                    "There is no reward available to be claimed at this time.");
            }

            webProcessor.Click(ClaimRewardButtonSelector);
        }

        public void OpenPot()
        {
            webProcessor.WaitForElementToBeVisible(OpenPotButtonSelector);

            if (!webProcessor.IsElementVisible(OpenPotButtonSelector))
            {
                throw new InvalidOperationException(
                    "The Lucky Pot cannot be opened at this time.");
            }

            webProcessor.Click(OpenPotButtonSelector);
        }

        private void EnterCredentials()
        {
            webProcessor.SetText(EmailAddressInputSelector, settings.EmailAddress);
            webProcessor.SetText(PasswordInputSelector, settings.Password);
        }

        private void AcceptCookiesIfNeeded()
        {
            webProcessor.WaitForElementToBeVisible(CookiesConsentButtonSelector);

            if (!webProcessor.IsElementVisible(CookiesConsentButtonSelector))
            {
                return;
            }

            webProcessor.Click(CookiesConsentButtonSelector);
        }
    }
}
