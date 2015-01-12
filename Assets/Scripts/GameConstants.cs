using System.IO;
using UnityEngine;

public static class GameConstants
{

    // General
    public const int MaxClientsInGame = 32;

    // Field
    public const int CellSize = 32;
    public const int Width = 40; // 40*32=1280
    public const int Height = 22; // 22*32=704

    // IRC
    public const string IrcServer = "irc.twitch.tv";
    public const int IrcPort = 6667;
    public const string IrcChannel = "#erhune";
    public const string IrcUser = "erhune";
    public const string IrcPassword = "oauth:11a59964l7yy4j5ifwccqzn7rmyzux";

    // Persistance
    public static readonly string DatabaseFile = Path.Combine("URI=file:" + Application.dataPath, "../Data/game.db");

}
