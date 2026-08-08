namespace ChekersAPI
{
    /// <summary>
    /// Connects to Postgres in the background and keeps retrying until it succeeds.
    ///
    /// Deliberately not a blocking startup step. The checkers engine holds every live game
    /// in memory and needs no database whatsoever; only the leaderboard does. Failing
    /// startup on an unreachable database would turn "the leaderboard is briefly down" into
    /// "nobody can play", and on a first deploy it would crash-loop against a Postgres that
    /// is still bootstrapping — while the readiness probe, which must not depend on the
    /// database for the same reason, would happily report the pod healthy the moment it
    /// finally came up.
    /// </summary>
    public sealed class DatabaseStartup : BackgroundService
    {
        private readonly ILogger<DatabaseStartup> _logger;

        public DatabaseStartup(ILogger<DatabaseStartup> logger) => _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delay = TimeSpan.FromSeconds(2);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!Db.Ready)
                    {
                        await Db.InitializeAsync(_logger, stoppingToken);
                    }
                    GameMetrics.GamesAllTime.Set(
                        await Db.ReadCounterAsync(Db.GamesStartedCounter, stoppingToken));
                    GameMetrics.DatabaseUp.Set(1);
                    delay = TimeSpan.FromSeconds(30);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    GameMetrics.DatabaseUp.Set(0);
                    _logger.LogWarning(ex, "leaderboard database unavailable; games are unaffected, retrying");
                    delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
                }

                try { await Task.Delay(delay, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
