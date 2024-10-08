namespace AppHealthcheck {
    internal partial class HealthChecks {
        internal class HeartBeats : Models.IHealthCheckService {
            private Client Client { get; set; }
            private static ILogger Logger => Client.Logger;

            internal HeartBeats(Client client) {
                Client = client;
            }

            Task Models.IHealthCheckService.RunTests() => Task.Run(() => {
                Logger.LogInformation($"[HealthCheck {nameof(HeartBeats)}]: Starting Healthcheck '{nameof(HeartBeats)}'");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                CheckAllServicesAreOnline();

                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Logger.LogInformation($"[HealthCheck {nameof(HeartBeats)}]: Healthcheck '{nameof(HeartBeats)}' RunTime: {elapsedTime}");
            });

            private void CheckAllServicesAreOnline() {
                double heartbeatPulseTiming = double.Parse(Microsoft.Azure.SpaceFx.Core.GetConfigSetting("heartbeatpulsetimingms").Result);
                heartbeatPulseTiming = heartbeatPulseTiming * (double) 2;
                TimeSpan timeSpan = TimeSpan.FromMilliseconds(heartbeatPulseTiming);

                Logger.LogInformation($"[HealthCheck {nameof(HeartBeats)}]: Waiting for {timeSpan.TotalSeconds} seconds, then checking for services heard...");
                Thread.Sleep(timeSpan);

                List<HeartBeatPulse> heartBeats = Microsoft.Azure.SpaceFx.SDK.Client.ServicesOnline();

                List<string> expectedServices = new() { "hostsvc-sensor", "hostsvc-logging", "hostsvc-position", "hostsvc-link", "platform-mts", "platform-deployment" };

                heartBeats.ForEach((_heartBeat) => {
                    Logger.LogInformation($"[HealthCheck {nameof(HeartBeats)}]: Service Online: {_heartBeat.AppId}");
                });

                Logger.LogInformation($"[HealthCheck {nameof(HeartBeats)}]: Total Services Online: {heartBeats.Count}");

                if (heartBeats.Count == 0)
                    throw new Exception($"Unable to check services.  No services will heard after {timeSpan.TotalSeconds}.  See error log and retry.");


                List<string> missingServices = expectedServices
                    .Where(service => !heartBeats.Any(hb => hb.AppId == service))
                    .ToList();

                missingServices.ForEach(service => Logger.LogError($"[HealthCheck {nameof(HeartBeats)}]: Missing expected service '{service}'. No heartbeats received."));
                expectedServices.Except(missingServices).ToList().ForEach(service => Logger.LogInformation($"[HealthCheck {nameof(HeartBeats)}]: Heard expected service '{service}'"));

                if (missingServices.Any()) {
                    throw new Exception($"The following services are missing: {string.Join(", ", missingServices)}");
                }
            }
        }
    }
}