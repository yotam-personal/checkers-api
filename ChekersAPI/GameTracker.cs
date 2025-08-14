using System;
using System.Collections.Concurrent;
using System.Threading;
using CheckersEngine;

// singleton class
public class GameTracker
{
    private const string k_Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Lazy<GameTracker> instance = new Lazy<GameTracker>(() => new GameTracker());
    public static GameTracker Instance => instance.Value;

    private ConcurrentDictionary<string, DateTime> games;
    private ConcurrentDictionary<string, GameObject> ongoingGames;
    private Timer cleanupTimer;
    private static ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random());

    private GameTracker()
    {
        this.games = new ConcurrentDictionary<string, DateTime>();
        this.ongoingGames = new ConcurrentDictionary<string, GameObject>();
        // Set up a timer to clean up expired games every minute
        this.cleanupTimer = new Timer(CleanupExpiredGames!, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public void AddGame(GameObjectRequest i_Request)
    {
        games[i_Request.Id] = DateTime.Now;
        //place holder color
        ongoingGames[i_Request.Id] = new GameObject(i_Request);
    }

    public bool TryAccessGame(string gameId, out GameObject o_GameObject)
    {
        // o_GameOnject could be null, but it's ok!
        bool accessed = ongoingGames.TryGetValue(gameId, out o_GameObject!);
        if (accessed)
        {
            // Update the last access time
            games[gameId] = DateTime.Now;
        }

        return accessed;
    }

    private void CleanupExpiredGames(object state)
    {
        foreach (var game in games)
        {
            if (DateTime.Now - game.Value > TimeSpan.FromMinutes(10))
            {
                DateTime removedValue;
                GameObject removedGameObject;
                games.TryRemove(game.Key, out removedValue);
                ongoingGames.TryRemove(game.Key, out removedGameObject);
            }
        }
    }

    public async Task<string> GenerateUniqueGameIdAsync()
    {
        return await Task.Run(() =>
        {
            string newGameId;
            do
            {
                var random = threadLocalRandom.Value;
                newGameId = new string(Enumerable.Repeat(k_Chars, 8)
                  .Select(s => s[random!.Next(s.Length)]).ToArray());
            } while (games.ContainsKey(newGameId));

            return newGameId;
        });
    }
}
