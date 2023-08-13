using System.ComponentModel.DataAnnotations.Schema;
using BattleBitAPI.Common;
using Microsoft.EntityFrameworkCore;

namespace CommunityServerAPI.Models;

public class ServerPlayer
{
    // Remove auto increment etc.
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong steamId;

    public PlayerStats stats;
}