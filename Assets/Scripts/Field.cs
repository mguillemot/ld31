using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class Field : MonoBehaviour
{

    private static Field _instance;
    public static Field instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Field>();
            }
            return _instance;
        }
    }

    public GameObject DropPrefab;
    public GameObject PlayerPrefab;
    public GameObject TextEffectPrefab;
    public GameObject SpellEffectPrefab;
    public Image Background;
    public Gradient Daylight;
    public float DaylightSpeed = 3600;

    public readonly HashSet<long> blocking = new HashSet<long>();
    public readonly Dictionary<long, Area> areaPerPos = new Dictionary<long, Area>();
    public readonly Dictionary<Area, List<long>> posPerArea = new Dictionary<Area, List<long>>();
    public readonly Dictionary<string, Player> players = new Dictionary<string, Player>();
    public readonly List<Monster> monsters = new List<Monster>();
    public readonly List<Npc> npcs = new List<Npc>();
    public readonly List<Drop> drops = new List<Drop>();

    void Awake()
    {
        // Bottom right forest
        blocking.UnionWith(Position.MakeRectangle(1, 1, 4, 1));
        blocking.UnionWith(Position.MakeRectangle(2, 1, 2, 2));
        blocking.Add(Position.Make(3, 1));
        // Bottom forest
        blocking.UnionWith(Position.MakeRectangle(16, 1, 20, 1));
        blocking.UnionWith(Position.MakeRectangle(18, 2, 19, 2));
        // Barricade
        blocking.UnionWith(Position.MakeRectangle(1, 10, 2, 10));
        blocking.UnionWith(Position.MakeRectangle(5, 10, 6, 10));
        // Fountain
        blocking.UnionWith(Position.MakeRectangle(7, 16, 8, 17));
        // Left house
        blocking.UnionWith(Position.MakeRectangle(3, 20, 6, 22));
        // Right house
        blocking.UnionWith(Position.MakeRectangle(9, 20, 12, 22));
        // Top forest
        blocking.UnionWith(Position.MakeRectangle(18, 22, 24, 22));
        blocking.UnionWith(Position.MakeRectangle(20, 21, 22, 21));
        // Lake
        blocking.UnionWith(Position.MakeRectangle(25, 16, 27, 16));
        blocking.UnionWith(Position.MakeRectangle(24, 17, 28, 17));
        blocking.UnionWith(Position.MakeRectangle(25, 18, 27, 18));
        // Middle forest
        blocking.UnionWith(Position.MakeRectangle(8, 7, 9, 8));
        blocking.UnionWith(Position.MakeRectangle(7, 9, 10, 9));
        blocking.UnionWith(Position.MakeRectangle(7, 10, 11, 10));
        blocking.UnionWith(Position.MakeRectangle(8, 11, 30, 11));
        blocking.UnionWith(Position.MakeRectangle(10, 12, 30, 12));
        blocking.UnionWith(Position.MakeRectangle(15, 13, 17, 13));
        blocking.UnionWith(Position.MakeRectangle(22, 13, 25, 13));
        blocking.UnionWith(Position.MakeRectangle(30, 13, 30, 13));
        blocking.UnionWith(Position.MakeRectangle(31, 11, 32, 20));
        blocking.UnionWith(Position.MakeRectangle(33, 16, 33, 19));
        blocking.UnionWith(Position.MakeRectangle(30, 21, 33, 21));
        blocking.UnionWith(Position.MakeRectangle(28, 22, 35, 22));
        blocking.UnionWith(Position.MakeRectangle(22, 10, 30, 10));
        blocking.UnionWith(Position.MakeRectangle(25, 9, 27, 9));
        // Top right forest
        blocking.Add(Position.Make(39, 21));
        blocking.Add(Position.Make(39, 22));
        blocking.Add(Position.Make(40, 22));
        // Right forest
        blocking.Add(Position.Make(37, 9));
        blocking.UnionWith(Position.MakeRectangle(38, 9, 38, 11));
        blocking.UnionWith(Position.MakeRectangle(39, 10, 39, 14));
        blocking.UnionWith(Position.MakeRectangle(40, 9, 40, 16));
        // Right house
        blocking.UnionWith(Position.MakeRectangle(38, 6, 40, 7));
        // Bottom right forest
        blocking.UnionWith(Position.MakeRectangle(30, 1, 40, 1));
        blocking.UnionWith(Position.MakeRectangle(32, 2, 36, 2));
        blocking.UnionWith(Position.MakeRectangle(33, 3, 36, 3));
        blocking.UnionWith(Position.MakeRectangle(34, 4, 35, 5));
        blocking.UnionWith(Position.MakeRectangle(40, 2, 40, 3));

        // Areas:
        SetArea(Area.VILLAGE, Position.MakeRectangle(5, 15, 10, 18));
        SetArea(Area.INN, Position.MakeRectangle(2, 17, 3, 19));
        SetArea(Area.SMITH, Position.MakeRectangle(9, 19, 12, 19));
        SetArea(Area.LAKE, Position.MakeRectangle(21, 13, 30, 22));
        SetArea(Area.GATE, Position.MakeRectangle(3, 10, 4, 10));
        SetArea(Area.GATEFRONT, Position.MakeRectangle(1, 1, 16, 10));
        SetArea(Area.DESERT, Position.MakeRectangle(1, 1, 36, 10));
        SetArea(Area.CAMPFIRE, Position.MakeRectangle(37, 1, 40, 10));
        SetArea(Area.BOSS, Position.MakeRectangle(36, 17, 48, 19));
    }

    private void SetArea(Area area, IEnumerable<long> pos)
    {
        posPerArea[area] = new List<long>();
        foreach (var p in pos)
        {
            if (!blocking.Contains(p))
            {
                areaPerPos[p] = area;
                posPerArea[area].Add(p);
            }
        }
    }

    void Start()
    {
        monsters.AddRange(FindObjectsOfType<Monster>());
        npcs.AddRange(FindObjectsOfType<Npc>());
        drops.AddRange(FindObjectsOfType<Drop>());
        foreach (var player in FindObjectsOfType<Player>())
        {
            players[player.Name] = player;
        }

        // Visual sorting
        var trees = FindObjectsOfType<Tree>().ToList();
        trees.Sort((t1, t2) => -t1.transform.localPosition.y.CompareTo(t2.transform.localPosition.y));
        for (int i = 0; i < trees.Count; i++)
        {
            trees[i].transform.SetSiblingIndex(i);
        }
    }

    void Update()
    {
        // Daylight
        var t = (Time.time % DaylightSpeed) / DaylightSpeed;
        Background.color = Daylight.Evaluate(t);

        // Visual sorting
        var entities = FindObjectsOfType<Entity>().ToList();
        entities.Sort((t1, t2) => -t1.transform.localPosition.y.CompareTo(t2.transform.localPosition.y));
        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].transform.SetSiblingIndex(1 + i); // 0 is always the background
        }
    }

    public void ClientArrives(GameManager.Client client, bool waited)
    {
        if (players.ContainsKey(client.Nickname))
        {
            Debug.LogWarning("Client " + client.Nickname + " arrives but he already has a player => ignore");
            return;
        }

        Debug.Log("Client " + client.Nickname + " arrives");

        var pos = posPerArea[Area.VILLAGE].RandomElement();
        var playerObject = (GameObject) Instantiate(PlayerPrefab);
        playerObject.transform.parent = transform;
        playerObject.transform.localPosition = new Vector3(pos.GetX() * GameConstants.CellSize, pos.GetY() * GameConstants.CellSize);
        playerObject.name = "Player " + client.Nickname;
        client.Player = playerObject.GetComponent<Player>();
        client.Player.Name = client.Nickname;
        client.Player.GainExperience(client.SavedExperience);
        client.Player.Gold = client.SavedGold;
        client.Player.Potions = client.SavedPotions;
        client.Player.Weapon = client.SavedWeapon;
        client.Player.Equipment = client.SavedEquipment;
        client.Player.Backpack = client.SavedBackpack;
        players[client.Nickname] = client.Player;

        if (waited)
        {
            ProcessCommand(client, client.Nickname + " is finally there!");
        }
        else
        {
            ProcessCommand(client, client.Nickname + " is there!");
        }
    }

    public void ClientLeaves(GameManager.Client client)
    {
        Debug.Log("Client " + client.Nickname + " leaves");

        Player player;
        if (players.TryGetValue(client.Nickname, out player))
        {
            player.Die(false); // will unregister from players
        }
        else
        {
            Debug.LogWarning("Client " + client.Nickname + " leaves but he didn't have a player");
        }

        client.Player = null;
    }

    public void ProcessCommand(GameManager.Client client, string message)
    {
        if (message.StartsWith("!!!")) // system message
        {
            return; 
        }

        Player player;
        if (players.TryGetValue(client.Nickname, out player))
        {
            player.ProcessInput(message);
        }
        else
        {
            Debug.LogWarning("Cannot process command for player " + client.Nickname + " who is not connected: " + message);
        }
    }

    public void ShowEffect(Vector3 pos, string repr, Color color)
    {
        var effectObject = (GameObject) Instantiate(TextEffectPrefab);
        effectObject.transform.SetParent(transform, true);
        effectObject.transform.localPosition = pos;
        var text = effectObject.GetComponent<Text>();
        text.text = repr;
        text.color = color;
        StartCoroutine(TweenEffect(text));
    }

    private IEnumerator TweenEffect(Text text)
    {
        var startTime = Time.time;
        var initialPos = text.transform.localPosition;
        var finalPos = text.transform.localPosition + new Vector3(0, 50);
        var initialColor = text.color;
        var finalColor = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
        while (Time.time - startTime < 2)
        {
            var t = Mathf.InverseLerp(0f, 2f, Time.time - startTime);
            text.transform.localPosition = Vector3.Lerp(initialPos, finalPos, t);
            text.color = Color.Lerp(initialColor, finalColor, t);
            yield return null;
        }
        Destroy(text.gameObject);
    }

    public void ShowEffect(Vector3 pos, Color color)
    {
        var effectObject = (GameObject) Instantiate(SpellEffectPrefab);
        effectObject.transform.SetParent(transform, true);
        effectObject.transform.localPosition = pos;
        var image = effectObject.GetComponent<Image>();
        image.color = color;
        StartCoroutine(TweenEffect(image));
    }

    private IEnumerator TweenEffect(Image image)
    {
        var startTime = Time.time;
        var initialColor = image.color;
        var finalColor = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
        while (Time.time - startTime < 2)
        {
            if (Time.time - startTime < .5f)
            {
                var scaleT = Mathf.InverseLerp(0, .5f, Time.time - startTime);
                image.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, scaleT);
            }
            var colorT = Mathf.InverseLerp(0f, 2f, Time.time - startTime);
            image.color = Color.Lerp(initialColor, finalColor, colorT);
            yield return null;
        }
        Destroy(image.gameObject);
    }

    public Area GetArea(int x, int y)
    {
        Area area;
        areaPerPos.TryGetValue(Position.Make(x, y), out area);
        return area;
    }

    // See: http://en.wikipedia.org/wiki/A*_search_algorithm
    public List<long> ComputePath(long start, long goal)
    {
        //Debug.LogWarning("ComputePath(" + start + "," + goal + ")");
        var closedSet = new HashSet<long>();
        var openSet = new List<long> {start}; // sorted by fScore
        var cameFrom = new Dictionary<long, long>();
        var gScore = new Dictionary<long, float>();
        var fScore = new Dictionary<long, float>();
        Func<long, long, int> h = delegate(long from, long to)
                                  {
                                      var dx = from.GetX() - to.GetX();
                                      var dy = from.GetY() - to.GetY();
                                      return dx * dx + dy * dy;
                                  };
        Func<long, long, float> dist = delegate(long from, long to)
                                       {
                                           if (blocking.Contains(from) || blocking.Contains(to))
                                           {
                                               return 10000;
                                           }
                                           return h(from, to);
                                       };
        Func<long, List<long>> reconstructPath = delegate(long c)
                                                 {
                                                     var totalPath = new List<long> {c};
                                                     while (cameFrom.ContainsKey(c))
                                                     {
                                                         c = cameFrom[c];
                                                         totalPath.Insert(0, c);
                                                     }
                                                     totalPath.RemoveAt(0);
                                                     return totalPath;
                                                 };

        gScore[start] = 0;
        fScore[start] = gScore[start] + h(start, goal);

        while (openSet.Count > 0)
        {
            var current = openSet[0];
            if (current == goal)
            {
                var path = reconstructPath(goal);
                //Debug.LogWarning("Found path: " + string.Join(" => ", path.Select(p => p.GetX() + ":" + p.GetY()).ToArray()));
                return path;
            }
            openSet.RemoveAt(0);
            closedSet.Add(current);
            foreach (var neighbour in GetNeighbourPositions(current))
            {
                if (closedSet.Contains(neighbour))
                {
                    continue;
                }
                var tentativeGScore = gScore[current] + dist(current, neighbour);
                if (!openSet.Contains(neighbour) || tentativeGScore < gScore[neighbour])
                {
                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentativeGScore;
                    fScore[neighbour] = gScore[neighbour] + h(neighbour, goal);
                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
            openSet.Sort((n1, n2) => fScore[n1].CompareTo(fScore[n2]));
        }
        Debug.LogWarning("No path found");
        return null;
    }

    private IEnumerable<long> GetNeighbourPositions(long position)
    {
        for (int x = position.GetX() - 1; x <= position.GetX() + 1; x++)
        {
            for (int y = position.GetY() - 1; y <= position.GetY() + 1; y++)
            {
                if (x >= 1 && x <= GameConstants.Width && y >= 1 && y <= GameConstants.Height && !blocking.Contains(Position.Make(x, y)))
                {
                    yield return Position.Make(x, y);
                }
            }
        }
    }

    public IEnumerable<Player> GetAllAggro(Entity monster, float range)
    {
        var monsterX = monster.currentX;
        var monsterY = monster.currentY;
        foreach (var player in players.Values)
        {
            if (!player.isDead)
            {
                var dx = player.currentX - monsterX;
                var dy = player.currentY - monsterY;
                if (dx * dx + dy * dy <= range * range)
                {
                    yield return player;
                }
            }
        }
    }

    public Player GetAggro(Entity monster, float range)
    {
        var targets = GetAllAggro(monster, range).ToList();
        if (targets.Count == 0)
        {
            return null;
        }
        return targets.RandomElement();
    }

    public IEnumerable<Monster> GetAllNearbyMonsters(Player player, float range)
    {
        var playerX = player.currentX;
        var playerY = player.currentY;
        foreach (var monster in monsters)
        {
            if (monster) // weird bug?
            {
                var dx = monster.currentX - playerX;
                var dy = monster.currentY - playerY;
                if (dx * dx + dy * dy <= range * range)
                {
                    yield return monster;
                }
            }
        }
    }

    public Monster GetNearbyMonster(Player player, float range)
    {
        foreach (var monster in GetAllNearbyMonsters(player, range))
        {
            return monster;
        }
        return null;
    }

    public Monster GetNearestMonster(Player player)
    {
        var playerX = player.currentX;
        var playerY = player.currentY;
        Monster nearest = null;
        var nearestDistance = 12 * 12 + 1; // do not look for farther than 12 cells away
        foreach (var monster in monsters)
        {
            var dx = monster.currentX - playerX;
            var dy = monster.currentY - playerY;
            var dist = dx * dx + dy * dy;
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearest = monster;
            }
        }
        return nearest;
    }

    public Npc GetNearbyNpc(Player player)
    {
        const int MaxDist = 3;
        var playerX = player.currentX;
        var playerY = player.currentY;
        foreach (var npc in npcs)
        {
            var dx = npc.currentX - playerX;
            var dy = npc.currentY - playerY;
            if (dx >= -MaxDist && dx <= MaxDist && dy >= -MaxDist && dy <= MaxDist)
            {
                return npc;
            }
        }
        return null;
    }

    public Drop GetDropAt(Entity entity)
    {
        var x = entity.currentX;
        var y = entity.currentY;
        foreach (var drop in drops)
        {
            var dx = drop.currentX - x;
            var dy = drop.currentY - y;
            if (dx == 0 && dy == 0)
            {
                return drop;
            }
        }
        return null;
    }

    public IEnumerable<Drop> GetDropNear(Entity entity)
    {
        const int MaxDist = 1;
        var x = entity.currentX;
        var y = entity.currentY;
        foreach (var drop in drops)
        {
            var dx = drop.currentX - x;
            var dy = drop.currentY - y;
            if (dx >= -MaxDist && dx <= MaxDist && dy >= -MaxDist && dy <= MaxDist)
            {
                yield return drop;
            }
        }
    }

    public Drop GetNearestDrop(Entity entity, Item item)
    {
        var x = entity.currentX;
        var y = entity.currentY;
        Drop nearest = null;
        var nearestDistance = 12 * 12 + 1; // do not look for farther than 12 cells away
        foreach (var drop in drops)
        {
            if (drop.Item == item)
            {
                var dx = drop.currentX - x;
                var dy = drop.currentY - y;
                var dist = dx * dx + dy * dy;
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearest = drop;
                }
            }
        }
        return nearest;
    }

}
