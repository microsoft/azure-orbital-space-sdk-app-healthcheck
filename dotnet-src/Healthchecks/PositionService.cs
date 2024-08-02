namespace AppHealthcheck {
    internal partial class HealthChecks {
        internal class PositionService : Models.IHealthCheckService {
            private Client Client { get; set; }
            private static ILogger Logger => Client.Logger;

            internal PositionService(Client client) {
                Client = client;
            }

            Task Models.IHealthCheckService.RunTests() => Task.Run(() => {
                Logger.LogInformation($"[HealthCheck {nameof(PositionService)}]: Starting Healthcheck '{nameof(PositionService)}'");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                GetPosition();

                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Logger.LogInformation($"[HealthCheck {nameof(PositionService)}]: Healthcheck '{nameof(PositionService)}' RunTime: {elapsedTime}");
            });

            private void GetPosition() {
                Logger.LogInformation($"[HealthCheck {nameof(PositionService)}]: Requesting Position");
                PositionResponse _response = Microsoft.Azure.SpaceFx.SDK.Position.LastKnownPosition().Result;

                Logger.LogInformation($"[HealthCheck {nameof(PositionService)}]: Received response with status: {_response.ResponseHeader.Status}");

                if (_response.ResponseHeader.Status != StatusCodes.Successful && _response.ResponseHeader.Status != StatusCodes.NotFound) {
                    Logger.LogError($"[HealthCheck {nameof(PositionService)}]: Did not receive successful response from position service.  Status: {_response.ResponseHeader.Status}");
                    throw new Exception($"Did not receive successful response from position service.  Status: {_response.ResponseHeader.Status}");
                }
            }
        }
    }
}