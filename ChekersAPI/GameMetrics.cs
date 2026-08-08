using Prometheus;

namespace ChekersAPI
{
    /// <summary>
    /// Two kinds of number, deliberately kept apart.
    ///
    /// Counters here are process-lifetime and reset when the pod restarts. That is correct
    /// for Prometheus, which detects the reset and still computes rates across it, and it
    /// is what "games started in the last hour" should be built from.
    ///
    /// The lifetime total is a gauge fed from Postgres. A Prometheus counter cannot answer
    /// "how many games have ever been played" — every deploy would erase it. It counts from
    /// the day this ran on Orbit and not before: the Firestore version stored only the last
    /// five winners, so there is no history to backfill and claiming otherwise would be a
    /// made-up number on a dashboard.
    /// </summary>
    public static class GameMetrics
    {
        public static readonly Counter GamesStarted = Metrics.CreateCounter(
            "checkers_games_started_total", "Games started since this process began.");

        public static readonly Counter MovesRequested = Metrics.CreateCounter(
            "checkers_moves_total", "Engine move requests served since this process began.",
            new CounterConfiguration { LabelNames = new[] { "outcome" } });

        public static readonly Counter WinsRecorded = Metrics.CreateCounter(
            "checkers_wins_total", "Human wins accepted onto the leaderboard since this process began.");

        public static readonly Gauge GamesAllTime = Metrics.CreateGauge(
            "checkers_games_all_time",
            "Games started since this service moved to Orbit. Durable across restarts; not backfilled from Firebase.");

        public static readonly Gauge DatabaseUp = Metrics.CreateGauge(
            "checkers_database_up", "1 when the leaderboard database is reachable, 0 when it is not.");
    }
}
