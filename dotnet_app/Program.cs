namespace AppHealthcheck {
    public class Program {
        private static Client Client = new Client();
        private static ILogger Logger => Client.Logger;
        public static async Task Main() {
            Client.Build();
            List<Exception> exceptions = new List<Exception>();

            // Start a stop watch so we can track how long the entire test ran
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var healthCheckServices = new List<Models.IHealthCheckService>{
                new HealthChecks.HeartBeats(Client),
                new HealthChecks.LoggingService(Client),
                new HealthChecks.PositionService(Client),
                new HealthChecks.LinkService(Client),
                new HealthChecks.SensorService(Client)
            };

            foreach (var service in healthCheckServices) {
                try {
                    await service.RunTests();
                } catch (Exception ex) {
                    Logger.LogError($"{service.GetType().Name} failed: {ex.Message}");
                    exceptions.Add(ex);
                }
            }

            // Stop the stop watch and output the run time
            stopwatch.Stop();

            TimeSpan ts = stopwatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);

            if (exceptions.Count == 0) {
                Logger.LogInformation("Healthchecks completed successfully.  RunTime: {runTime}", elapsedTime);
            } else {
                Logger.LogError("Healthchecks completed with {errorCount} error(s).  RunTime: {runTime}", exceptions.Count, elapsedTime);
                throw new Exception("Healthchecks completed with errors.  See logs for details.");
            }

            Client.Shutdown();
        }
    }
}