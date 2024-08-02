namespace AppHealthcheck {
    internal partial class HealthChecks {
        internal class SensorService : Models.IHealthCheckService {
            private Client Client { get; set; }
            private static ILogger Logger => Client.Logger;
            private const string DEMO_TEMPERATURE_SENSOR_ID = "DemoTemperatureSensor";
            private TimeSpan MAX_TIMESPAN_TO_WAIT_FOR_MSG = TimeSpan.FromSeconds(30);

            internal SensorService(Client client) {
                Client = client;
            }

            Task Models.IHealthCheckService.RunTests() => Task.Run(() => {
                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Starting Healthcheck '{nameof(SensorService)}'");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                DemoTemperatureSensorTest();

                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Healthcheck '{nameof(SensorService)}' RunTime: {elapsedTime}");
            });

            private void DemoTemperatureSensorTest() {
                DateTime maxTimeToWait = DateTime.Now.Add(MAX_TIMESPAN_TO_WAIT_FOR_MSG);
                SensorData? sensorData = null;
                string trackingId = Guid.NewGuid().ToString();

                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Querying for Available Sensors...");

                SensorsAvailableResponse _sensorsAvailable = Microsoft.Azure.SpaceFx.SDK.Sensor.GetAvailableSensors().Result;
                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Received response with status: {_sensorsAvailable.ResponseHeader.Status}");

                if (_sensorsAvailable.ResponseHeader.Status != StatusCodes.Successful) {
                    Logger.LogError($"[HealthCheck {nameof(SensorService)}]: Did not receive successful response from sensor service while querying Sensors Available.  Status: {_sensorsAvailable.ResponseHeader.Status}");
                    throw new Exception($"Did not receive successful response from sensor service while querying Sensors Available.  Status: {_sensorsAvailable.ResponseHeader.Status}");
                }

                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Received {_sensorsAvailable.Sensors.Count} sensors: {string.Join(", ", _sensorsAvailable.Sensors)}");


                if (!_sensorsAvailable.Sensors.Any(_sensor => String.Equals(_sensor.SensorID, DEMO_TEMPERATURE_SENSOR_ID, StringComparison.OrdinalIgnoreCase))) {
                    Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Sensor '{DEMO_TEMPERATURE_SENSOR_ID}' not found.  Nothing to do");
                    return;
                }

                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Found sensor {DEMO_TEMPERATURE_SENSOR_ID}.");

                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Registering for Sensor Data");

                void SensorDataEventHandler(object? _, SensorData _sensorData) {
                    if (_sensorData.SensorID == DEMO_TEMPERATURE_SENSOR_ID) {
                        sensorData = _sensorData;
                        Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Received Sensor Data from sensor '{sensorData.SensorID}'");
                    }
                }

                Client.SensorDataEvent += SensorDataEventHandler;


                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Sending Tasking Precheck request");


                TaskingPreCheckResponse taskingPreCheckResponse = Sensor.SensorTaskingPreCheck(sensorId: DEMO_TEMPERATURE_SENSOR_ID).Result;

                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Tasking Precheck response: {taskingPreCheckResponse.ResponseHeader.Status}");

                if (taskingPreCheckResponse.ResponseHeader.Status != Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful) {
                    throw new ApplicationException($"[HealthCheck {nameof(SensorService)}]: Failed to TaskPreCheck sensor '{DEMO_TEMPERATURE_SENSOR_ID}'");
                }



                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Sending Tasking request");

                TaskingResponse taskingResponse = Sensor.SensorTasking(sensorId: DEMO_TEMPERATURE_SENSOR_ID).Result;

                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Tasking response: {taskingResponse.ResponseHeader.Status}");

                if (taskingResponse.ResponseHeader.Status != Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful) {
                    throw new ApplicationException($"Failed to Tas sensor '{DEMO_TEMPERATURE_SENSOR_ID}'");
                }


                Logger.LogInformation($"[HealthCheck {nameof(SensorService)}]: Waiting for Sensor Data");



                while (sensorData == null && DateTime.Now <= maxTimeToWait) {
                    Thread.Sleep(100);
                }

                if (sensorData == null) throw new TimeoutException($"Failed to hear {sensorData?.GetType().Name} after {MAX_TIMESPAN_TO_WAIT_FOR_MSG}.");


            }
        }
    }
}