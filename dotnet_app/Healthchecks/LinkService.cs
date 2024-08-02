namespace AppHealthcheck {
    internal partial class HealthChecks {
        internal class LinkService : Models.IHealthCheckService {
            private Client Client { get; set; }
            private static ILogger Logger => Client.Logger;

            internal LinkService(Client client) {
                Client = client;
            }

            Task Models.IHealthCheckService.RunTests() => Task.Run(() => {
                Logger.LogInformation($"[HealthCheck {nameof(LinkService)}]: Starting Healthcheck '{nameof(LinkService)}'");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                SendFile();

                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Logger.LogInformation($"[HealthCheck {nameof(LinkService)}]: Healthcheck '{nameof(LinkService)}' RunTime: {elapsedTime}");
            });

            private void SendFile() {
                Logger.LogInformation($"[HealthCheck {nameof(LinkService)}]: Cleaning out inbox '{Microsoft.Azure.SpaceFx.Core.GetXFerDirectories().Result.inbox_directory}'...");

                foreach (string file in Directory.GetFiles(Microsoft.Azure.SpaceFx.Core.GetXFerDirectories().Result.inbox_directory)) {
                    File.Delete(file);
                }

                Logger.LogInformation($"[HealthCheck {nameof(LinkService)}]: Generating Link Request and waiting for response");
                LinkResponse _response = Microsoft.Azure.SpaceFx.SDK.Link.SendFileToApp(destinationAppId: Client.APP_ID, file: Path.Combine(Environment.CurrentDirectory, "sampleData", "astronaut.jpg"), overwriteDestinationFile: true).Result;
                Logger.LogInformation($"[HealthCheck {nameof(LinkService)}]: Received response with status: {_response.ResponseHeader.Status}");


                if (_response.ResponseHeader.Status != StatusCodes.Successful) {
                    Logger.LogError($"[HealthCheck {nameof(LinkService)}]: Did not receive successful response from link service.  Status: {_response.ResponseHeader.Status}");
                    throw new Exception($"Did not receive successful response from link service.  Status: {_response.ResponseHeader.Status}");
                }
            }
        }
    }
}