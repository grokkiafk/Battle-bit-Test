using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI;
using CommunityServerAPI.Models;
using CommunityServerAPI.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Proxies.Internal;

class Program
{
    static void Main(string[] args)
    {
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        listener.Start(29294);

        Thread.Sleep(-1);
        var builder = InteractableServiceProvider.Builder.ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddDbContext<DatabaseContext>((ServiceProvider, options) =>
            {
                // Allows for automatic inclusion of relations
                options.UseLazyLoadingProxies();

                // Here we connect to our database with a connection string.
                // Do not store your connection string in the code like this for production: Use environment variables or some other secret management instead.
                const string dbConnectionString = "server=localhost;port=3306;database=commapi;user=root;password=";
                options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString));
            });

            services.AddScoped<PlayerRepository>();
        });

        var app = builder.Build();

        InteractableServiceProvider.Services = app.Services.GetRequiredService<IServiceScopeFactory>();

        // Auto migrate on startup
        using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetService<DatabaseContext>();
            context?.Database.Migrate();
        }

        app.Run();
    }
}
class MyPlayer : Player<MyPlayer>
{

}
class MyGameServer : GameServer<MyPlayer>
{
    public List<string> ChatMessages = new List<string>();

    private readonly PlayerRepository _player;
    public MyGameServer()
    {
        using (var scope = InteractableServiceProvider.Services.CreateScope())
        {
            _player = scope.ServiceProvider.GetService<PlayerRepository>();
        }
    }
    public override async Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        return true;
    }
    public override async Task OnPlayerConnected(MyPlayer player)
    {
        await Console.Out.WriteLineAsync(this.GameIP + " Connected");
        player.Message("Test hello message");

    }
    public override async Task OnDisconnected()
    {
        await Console.Out.WriteLineAsync(this.GameIP + " Disconnected");
    }

    public override async Task OnSavePlayerStats(ulong steamId, PlayerStats stats)
    {
        var player = new ServerPlayer { steamId = steamId, stats = stats };
        // Check if there's already an entry in the DB, if so, update it, otherwise, create one.

        if (await _player.ExistsAsync(steamId))
        {
            await _player.UpdateAsync(player);
        }
        else
        {
            await _player.CreateAsync(player);
        }
    }

    public override async Task<PlayerStats> OnGetPlayerStats(ulong steamId, PlayerStats officialStats)
    // Here we try to get the player out of the database. Return a new PlayersStats() if null, otherwise
    // we will put player in a variable and return its stats.
    {
        return await _player.FindAsync(steamId) switch
        {
            null => new PlayerStats(),
            var player => player.stats
        };
    }

    public override async Task OnTick()
    {
        if (RoundSettings.State == GameState.WaitingForPlayers)
        {
            int numberOfPeopleInServer = this.CurrentPlayers;
            if (numberOfPeopleInServer > 4)
            {
                ForceStartGame();
            }
        }
        else if (RoundSettings.State == GameState.Playing)
        {

        }
    }
}
