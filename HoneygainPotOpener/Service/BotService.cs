using System;
using System.Collections.Generic;

using NuciLog.Core;

using HoneygainPotOpener.Configuration;
using HoneygainPotOpener.Logging;
using HoneygainPotOpener.Processors;

namespace HoneygainPotOpener.Service
{
    public sealed class BotService(
        IHoneygainProcessor processor,
        BotSettings settings,
        ILogger logger) : IBotService
    {
        public ExecutionReport Run()
        {
            List<Operation> failedOperations = [];

            ExecuteOperation(MyOperation.LogIn, processor.LogIn, false, failedOperations);
            ExecuteOperation(MyOperation.ClaimReward, processor.ClaimReward, true, failedOperations);
            ExecuteOperation(MyOperation.OpenPot, processor.OpenPot, false, failedOperations);

            return new ExecutionReport { FailedOperations = failedOperations };
        }

        private void ExecuteOperation(
            Operation operation,
            Action action,
            bool isFailureExpected,
            List<Operation> failedOperations)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, settings.EmailAddress)
            ];

            logger.Info(operation, OperationStatus.Started, logInfos);

            try
            {
                action();
            }
            catch (Exception exception)
            {
                LogFailure(operation, exception, isFailureExpected, logInfos);

                if (!isFailureExpected)
                {
                    failedOperations.Add(operation);
                }

                return;
            }

            logger.Info(operation, OperationStatus.Success, logInfos);
        }

        private void LogFailure(
            Operation operation,
            Exception exception,
            bool isFailureExpected,
            IEnumerable<LogInfo> logInfos)
        {
            if (isFailureExpected)
            {
                logger.Warn(operation, OperationStatus.Failure, exception, logInfos);

                return;
            }

            logger.Error(operation, OperationStatus.Failure, exception, logInfos);
        }
    }
}
