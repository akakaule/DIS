using BH.DIS.Core.Logging;
using System;

namespace BH.DIS.SDK.Logging
{
    public class SerilogAdapter : Core.Logging.ILogger
    {
        private readonly Serilog.ILogger _logger;

        public SerilogAdapter(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void Error(string messageTemplate, params object[] propertyValues) => 
            _logger.Error(messageTemplate, propertyValues);

        public void Error(Exception exception, string messageTemplate, params object[] propertyValues) => 
            _logger.Error(exception, messageTemplate, propertyValues);

        public void Fatal(string messageTemplate, params object[] propertyValues) =>
            _logger.Fatal(messageTemplate, propertyValues);

        public void Fatal(Exception exception, string messageTemplate, params object[] propertyValues) =>
            _logger.Fatal(exception, messageTemplate, propertyValues);

        public void Information(string messageTemplate, params object[] propertyValues) =>
            _logger.Information(messageTemplate, propertyValues);

        public void Information(Exception exception, string messageTemplate, params object[] propertyValues) =>
            _logger.Information(exception, messageTemplate, propertyValues);

        public void Verbose(string messageTemplate, params object[] propertyValues) =>
            _logger.Verbose(messageTemplate, propertyValues);

        public void Verbose(Exception exception, string messageTemplate, params object[] propertyValues) =>
            _logger.Verbose(exception, messageTemplate, propertyValues);
    }
}
