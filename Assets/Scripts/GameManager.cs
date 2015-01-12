using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Data.SqliteClient;
using NetIrc2;
using NetIrc2.Events;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GameManager : MonoBehaviour
{

    private static GameManager _instance;
    public static GameManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }

    public class Client
    {
        public string Nickname;
        public int ProcessedCommands;
        public int IgnoredCommands;
        public DateTime ConnectedAt;
        public DateTime UpdatedAt;
        public bool InGame;
        public Player Player;
        public int Playtime;

        // Save data:
        public int SavedExperience;
        public int SavedGold;
        public int SavedPotions;
        public Item SavedWeapon;
        public Item SavedEquipment;
        public Item SavedBackpack;
    }

    public class Command
    {
        public string Nickname;
        public string Content;
    }

    private IrcClient irc;
    private SqliteConnection db;
    private readonly Dictionary<string, Client> clients = new Dictionary<string, Client>();
    private readonly object commandQueueLock = new object();
    private readonly Queue<Command> commandQueue = new Queue<Command>();
    private readonly Queue<Client> waitQueue = new Queue<Client>();

    private float lastSent;
    private readonly Queue<string> ircQueue = new Queue<string>();

    public int ingameClients
    {
        get { return clients.Values.Count(c => c.InGame); }
    }

    void Start()
    {
        Debug.Log("Game starting");

        if (!Application.isEditor)
        {
            StartIRC();
        }

        OpenDB();

        if (Application.isEditor)
        {
            var localClient = new Client
                              {
                                  Nickname = "Picotachi-kun",
                                  ConnectedAt = DateTime.UtcNow,
                                  UpdatedAt = DateTime.UtcNow,
                                  InGame = true
                              };
            clients[localClient.Nickname] = localClient;
            LoadClient(localClient);
            Field.instance.ClientArrives(localClient, false);
        }

        InvokeRepeating("SavePeriodic", 60, 60);
    }

    private string guiCommand = "";
    void OnGUI()
    {
        if (!Application.isEditor)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        {
            var ircConnected = (irc != null) && irc.IsConnected;
            GUILayout.Label("IRC: " + ircConnected);
            GUILayout.Label("Players: " + FindObjectsOfType<Player>().Length);
            GUI.SetNextControlName("Command");
            guiCommand = GUILayout.TextField(guiCommand);
            if (GUI.GetNameOfFocusedControl() == "Command" && Event.current.keyCode == KeyCode.Return)
            {
                FindObjectsOfType<Player>().FirstOrDefault(p => p.Name == "Picotachi-kun").ProcessInput(guiCommand);
                guiCommand = "";
                GUI.FocusControl(null);
            }
        }
        GUILayout.EndArea();

        if (Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() != "Command")
        {
            GUI.FocusControl("Command");
        }
    }

    public void SayOnIRC(string to, string msg)
    {
        ircQueue.Enqueue(to.ToUpperInvariant() + ">> " + msg);
        while (ircQueue.Count > 5)
        {
            ircQueue.Dequeue();
        }
    }

    void SavePeriodic()
    {
        Debug.Log("Periodic save of " + clients.Count + " clients");
        foreach (var client in clients.Values)
        {
            SaveClient(client);
        }
    }

    private void OpenDB()
    {
        Debug.Log("Opening database: " + GameConstants.DatabaseFile);
        db = new SqliteConnection(GameConstants.DatabaseFile);
        db.Open();

        using (var command = db.CreateCommand())
        {
            command.CommandText = "PRAGMA journal_mode = WAL;";
            command.ExecuteNonQuery();
        }

        using (var command = db.CreateCommand())
        {
            command.CommandText = "PRAGMA journal_mode";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                Debug.Log("DB journal mode is: " + reader.GetString(0));
            }
        }

        using (var command = db.CreateCommand())
        {
            command.CommandText = @"CREATE TABLE IF NOT EXISTS players (nickname TEXT NOT NULL PRIMARY KEY, 
                                                                        experience INTEGER NOT NULL DEFAULT 0, 
                                                                        gold INTEGER NOT NULL DEFAULT 0, 
                                                                        potions INTEGER NOT NULL DEFAULT 0, 
                                                                        weapon TEXT, 
                                                                        equipment TEXT, 
                                                                        backpack TEXT, 
                                                                        playtime INTEGER NOT NULL DEFAULT 0, 
                                                                        created_at TEXT NOT NULL, 
                                                                        updated_at TEXT NOT NULL)";
            command.ExecuteNonQuery();
        }

        using (var command = db.CreateCommand())
        {
            command.CommandText = @"CREATE TABLE IF NOT EXISTS logs (nickname TEXT NOT NULL, command TEXT NOT NULL, created_at TEXT NOT NULL)";
            command.ExecuteNonQuery();
        }

        using (var command = db.CreateCommand())
        {
            command.CommandText = @"CREATE TABLE IF NOT EXISTS events (nickname TEXT NOT NULL, event TEXT NOT NULL, context TEXT, created_at TEXT NOT NULL)";
            command.ExecuteNonQuery();
        }
    }

    private void LoadClient(Client client)
    {
        try
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = @"SELECT experience, gold, potions, weapon, equipment, backpack, playtime FROM players WHERE nickname = @nickname LIMIT 1";
                command.Parameters.Add(new SqliteParameter("@nickname", client.Nickname));
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        client.SavedExperience = reader.GetInt32(0);
                        client.SavedGold = reader.GetInt32(1);
                        client.SavedPotions = reader.GetInt32(2);
                        client.SavedWeapon = reader.IsDBNull(3) ? Item.NONE : Utils.TryParse(reader.GetString(3), Item.NONE);
                        client.SavedEquipment = reader.IsDBNull(4) ? Item.NONE : Utils.TryParse(reader.GetString(4), Item.NONE);
                        client.SavedBackpack = reader.IsDBNull(5) ? Item.NONE : Utils.TryParse(reader.GetString(5), Item.NONE);
                        client.Playtime = reader.GetInt32(6);
                    }
                }
            }
            Debug.Log("Loaded client " + client.Nickname + " had " + client.SavedExperience + " XP");
        }
        catch (Exception e)
        {
            Debug.LogError("Exception raised while loading client: " + e);
        }
    }

    private void SaveClient(Client client)
    {
        try
        {
            using (var transaction = db.BeginTransaction())
            {
                Debug.Log("Saving client " + client.Nickname + " with " + client.Player.Experience + " XP, " + client.Player.Gold + " Gold...");
                using (var command = db.CreateCommand())
                {
                    command.CommandText = @"INSERT OR IGNORE INTO players (nickname, created_at, updated_at) VALUES (@nickname, datetime('now'), datetime('now'))";
                    command.Parameters.Add(new SqliteParameter("@nickname", client.Nickname));
                    command.ExecuteNonQuery();
                }
                if (client.InGame && client.Player != null)
                {
                    using (var command = db.CreateCommand())
                    {
                        command.CommandText = @"UPDATE players SET experience = @experience, gold = @gold, potions = @potions, 
                                                       weapon = @weapon, equipment = @equipment, backpack = @backpack, updated_at = datetime('now') 
                                                 WHERE nickname = @nickname";
                        command.Parameters.Add(new SqliteParameter("@nickname", client.Nickname));
                        command.Parameters.Add(new SqliteParameter("@experience", client.Player.Experience));
                        command.Parameters.Add(new SqliteParameter("@gold", client.Player.Gold));
                        command.Parameters.Add(new SqliteParameter("@potions", client.Player.Potions));
                        command.Parameters.Add(new SqliteParameter("@weapon", (client.Player.Weapon != Item.NONE) ? client.Player.Weapon.ToString() : null));
                        command.Parameters.Add(new SqliteParameter("@equipment", (client.Player.Equipment != Item.NONE) ? client.Player.Equipment.ToString() : null));
                        command.Parameters.Add(new SqliteParameter("@backpack", (client.Player.Backpack != Item.NONE) ? client.Player.Backpack.ToString() : null));
                        command.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception raised while saving client: " + e);
        }
    }

    public void StoreEvent(string nickname, string ev, string context)
    {
        try
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = @"INSERT INTO events (nickname, event, context, created_at) VALUES (@nickname, @event, @context, datetime('now'))";
                command.Parameters.Add(new SqliteParameter("@nickname", nickname));
                command.Parameters.Add(new SqliteParameter("@event", ev));
                command.Parameters.Add(new SqliteParameter("@context", context));
                command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception raised while storing event: " + e);
        }
    }

    private void StoreLog(string nickname, string content)
    {
        try
        {
            using (var command = db.CreateCommand())
            {
                command.CommandText = @"INSERT INTO logs (nickname, command, created_at) VALUES (@nickname, @command, datetime('now'))";
                command.Parameters.Add(new SqliteParameter("@nickname", nickname));
                command.Parameters.Add(new SqliteParameter("@command", content));
                command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception raised while storing event: " + e);
        }
    }

    void Update()
    {
        // IRC queue
        if (Time.time - lastSent >= 1.5f && ircQueue.Count > 0)
        {
            var msg = ircQueue.Dequeue();
            irc.Message(new IrcString(GameConstants.IrcChannel), new IrcString(msg));
            lastSent = Time.time;
        }

        // Process command queue
        lock (commandQueueLock)
        {
            while (commandQueue.Count > 0)
            {
                var command = commandQueue.Dequeue();
                StoreLog(command.Nickname, command.Content);
                Client client;
                if (!clients.TryGetValue(command.Nickname, out client))
                {
                    client = new Client
                             {
                                 Nickname = command.Nickname,
                                 ConnectedAt = DateTime.UtcNow,
                             };
                    clients[client.Nickname] = client;
                    LoadClient(client);

                    if (ingameClients < GameConstants.MaxClientsInGame)
                    {
                        client.InGame = true;
                        Debug.Log("Client " + client.Nickname + " arrives in game (#" + ingameClients + ") and tells command: " + command.Content);
                        Field.instance.ClientArrives(client, false);
                    }
                    else
                    {
                        waitQueue.Enqueue(client);
                        Debug.Log("Put " + client.Nickname + " in queue (#" + waitQueue.Count + ") and ignore its command for now: " + command.Content);
                    }
                }
                if (client.InGame)
                {
                    client.ProcessedCommands++;
                    if (command.Content == "!!!leave")
                    {
                        client.InGame = false;
                        clients.Remove(client.Nickname);
                        SaveClient(client); // must be done BEFORE ClientLeaves() which will set client.Player to null
                        Field.instance.ClientLeaves(client);
                    }
                    else
                    {
                        Field.instance.ProcessCommand(client, command.Content);
                    }
                }
                else
                {
                    client.IgnoredCommands++;
                }
                client.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Process wait queue
        while (ingameClients < GameConstants.MaxClientsInGame && waitQueue.Count > 0)
        {
            var client = waitQueue.Dequeue();
            client.InGame = true;
            Debug.Log("Client " + client.Nickname + " arrives in game (#" + ingameClients + ") from the wait queue!");
            Field.instance.ClientArrives(client, true);
        }
    }

    void OnDestroy()
    {
        if (irc != null && irc.IsConnected)
        {
            irc.Close();
            irc = null;
        }
        if (db != null)
        {
            SavePeriodic();
            db.Dispose();
            db = null;
        }
    }

    private void StartIRC()
    {
        irc = new IrcClient();
        irc.Connected += delegate(object sender, EventArgs args)
                         {
                             Debug.Log("IRC connected. Sending auth.");
                             irc.LogIn(GameConstants.IrcUser, GameConstants.IrcUser, GameConstants.IrcUser, null, null, GameConstants.IrcPassword);
                         };
        irc.Closed += delegate(object sender, EventArgs args)
                      {
                          Debug.Log("IRC closed.");
                      };
        irc.GotJoinChannel += delegate(object sender, JoinLeaveEventArgs args)
                              {
                                  var nickname = args.Identity.Nickname.ToString();
                                  Debug.Log("IRC " + nickname + " joined channel " + args.GetChannelList()[0]);
                                  if (nickname != GameConstants.IrcUser)
                                  {
                                      lock (commandQueueLock)
                                      {
                                          commandQueue.Enqueue(new Command
                                                               {
                                                                   Nickname = nickname,
                                                                   Content = "!!!join"
                                                               });
                                      }
                                  }
                              };
        irc.GotLeaveChannel += delegate(object sender, JoinLeaveEventArgs args)
                               {
                                   var nickname = args.Identity.Nickname.ToString();
                                   Debug.Log("IRC " + nickname + " left channel " + args.GetChannelList()[0]);
                                   if (nickname != GameConstants.IrcUser)
                                   {
                                       commandQueue.Enqueue(new Command
                                                            {
                                                                Nickname = nickname,
                                                                Content = "!!!leave"
                                                            });
                                   }
                               };
        irc.GotWelcomeMessage += delegate(object sender, SimpleMessageEventArgs args)
                                 {
                                     Debug.Log("IRC welcome: " + args.Message);
                                     irc.Join(GameConstants.IrcChannel);
                                 };
        irc.GotIrcError += delegate(object sender, IrcErrorEventArgs args)
                           {
                               Debug.LogError("IRC error: " + args.Error + " " + args.Data);
                           };
        irc.GotMessage += delegate(object sender, ChatMessageEventArgs args)
                          {
                              var nickname = args.Sender.Nickname.ToString();
                              var content = args.Message.ToString();
                              Debug.Log("IRC message from " + nickname + ": " + content);
                              if (nickname != "jtv")
                              {
                                  lock (commandQueueLock)
                                  {
                                      commandQueue.Enqueue(new Command
                                                           {
                                                               Nickname = nickname,
                                                               Content = content
                                                           });
                                  }
                              }
                          };
        irc.GotNotice += delegate(object sender, ChatMessageEventArgs args)
                         {
                             Debug.Log("IRC notice from " + args.Sender.Nickname + ": " + args.Message);
                         };
        irc.Connect(GameConstants.IrcServer, GameConstants.IrcPort);
    }

}
