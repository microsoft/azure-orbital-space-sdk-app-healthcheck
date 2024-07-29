namespace AppHealthcheck {
    internal partial class HealthChecks {
        internal class LoggingService {
            private Client Client { get; set; }
            private static ILogger Logger => Client.Logger;

            internal LoggingService(Client client) {
                Client = client;
            }

            internal void RunTests() {
                Logger.LogInformation($"Starting Test '{nameof(LoggingService)}'");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                SendLogs();
                SendTelemetry();

                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Logger.LogInformation($"Test '{nameof(LoggingService)}' RunTime: {elapsedTime}");
            }

            private void SendLogs() {
                LogMessage message = new() {
                    RequestHeader = new RequestHeader {
                        TrackingId = Guid.NewGuid().ToString()
                    },
                    Message = "Test Log Message",
                    LogLevel = LogMessage.Types.LOG_LEVEL.Info
                };


                Logger.LogInformation("Sending test Log message and waiting for response");
                var _response = Microsoft.Azure.SpaceFx.SDK.Logging.SendLogMessage(message, waitForResponse: true).Result;

                Logger.LogInformation($"Received response with status: {_response.ResponseHeader.Status}");

                if (_response.ResponseHeader.Status != StatusCodes.Successful) {
                    Logger.LogError($"Did not receive successful response from logging service.  Status: {_response.ResponseHeader.Status}");
                    throw new Exception($"Did not receive successful response from logging service.  Status: {_response.ResponseHeader.Status}");
                }
            }


            private void SendTelemetry() {
                Logger.LogInformation("Sending test telemetry message and waiting for response");
                var _response = Logging.SendTelemetry(metricName: "testMetric", metricValue: 1000, waitForResponse: false).Result;

                Logger.LogInformation($"Received response with status: {_response.ResponseHeader.Status}");

                if (_response.ResponseHeader.Status != StatusCodes.Successful) {
                    Logger.LogError($"Did not receive successful response from logging service.  Status: {_response.ResponseHeader.Status}");
                    throw new Exception($"Did not receive successful response from logging service.  Status: {_response.ResponseHeader.Status}");
                }
            }
        }
    }
}