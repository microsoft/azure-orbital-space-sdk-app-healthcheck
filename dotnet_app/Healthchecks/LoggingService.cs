namespace AppHealthcheck {
    internal partial class HealthChecks {
        internal class LoggingService : Models.IHealthCheckService {
            private Client Client { get; set; }
            private static ILogger Logger => Client.Logger;

            internal LoggingService(Client client) {
                Client = client;
            }

            Task Models.IHealthCheckService.RunTests() => Task.Run(() => {
                Logger.LogInformation($"[HealthCheck {nameof(LoggingService)}]: Starting Healthcheck '{nameof(LoggingService)}'");
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
                Logger.LogInformation($"[HealthCheck {nameof(LoggingService)}]: Healthcheck '{nameof(LoggingService)}' RunTime: {elapsedTime}");
            });

            private void SendLogs() {
                LogMessage message = new() {
                    RequestHeader = new RequestHeader {
                        TrackingId = Guid.NewGuid().ToString()
                    },
                    Message = "Test Log Message",
                    LogLevel = LogMessage.Types.LOG_LEVEL.Info
                };


                Logger.LogInformation($"[HealthCheck {nameof(LoggingService)}]: Sending test Log message and waiting for response");
                var _response = Microsoft.Azure.SpaceFx.SDK.Logging.SendLogMessage(message, waitForResponse: true).Result;

                Logger.LogInformation($"[HealthCheck {nameof(LoggingService)}]: Received response with status: {_response.ResponseHeader.Status}");

                if (_response.ResponseHeader.Status != StatusCodes.Successful) {
                    Logger.LogError($"[HealthCheck {nameof(LoggingService)}]: Did not receive successful response from logging service.  Status: {_response.ResponseHeader.Status}");
                    throw new Exception($"Did not receive successful response from logging service.  Status: {_response.ResponseHeader.Status}");
                }
            }


            private void SendTelemetry() {
                Logger.LogInformation($"[HealthCheck {nameof(LoggingService)}]: Sending test telemetry message and waiting for response");
                var _response = Logging.SendTelemetry(metricName: "testMetric", metricValue: 1000, waitForResponse: true).Result;

                Logger.LogInformation($"[HealthCheck {nameof(LoggingService)}]: Received response with status: {_response.ResponseHeader.Status}");

                if (_response.ResponseHeader.Status != StatusCodes.Successful) {
                    Logger.LogError($"[HealthCheck {nameof(LoggingService)}]: Did not receive successful response from logging service.  Status: {_response.ResponseHeader.Status}");
                    throw new Exception($"Did not receive successful response from logging service.  Status: {_response.ResponseHeader.Status}");
                }
            }


        }
    }
}