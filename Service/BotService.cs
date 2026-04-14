using NuciLog.Core;

using HoneygainPotOpener.Logging;
using HoneygainPotOpener.Processors;
using System.Collections.Generic;
using HoneygainPotOpener.Configuration;
using System;

namespace HoneygainPotOpener.Service
{
    public class BotService(
        IHoneygainProcessor processor,
        BotSettings settings,
        ILogger logger) : IBotService
    {
        public void Run()
        {
            LogIn();
            OpenPot();
        }

        void LogIn()
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, settings.EmailAddress)
            ];

            logger.Info(
                MyOperation.LogIn,
                OperationStatus.Started,
                logInfos);

            try
            {
                processor.LogIn();
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.LogIn,
                    OperationStatus.Failure,
                    ex,
                    logInfos);
            }

            logger.Info(
                MyOperation.LogIn,
                OperationStatus.Success,
                logInfos);
        }

        void OpenPot()
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, settings.EmailAddress)
            ];

            logger.Info(
                MyOperation.OpenPot,
                OperationStatus.Started,
                logInfos);

            try
            {
                processor.OpenPot();
            }
            catch (Exception ex)
            {
                logger.Error(
                    MyOperation.OpenPot,
                    OperationStatus.Failure,
                    ex,
                    logInfos);
            }

            logger.Info(
                MyOperation.OpenPot,
                OperationStatus.Success,
                logInfos);
        }
    }
}
