using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Player : Entity
{

    public Sprite[] Body;        // down, left, right, up (alphabetic order)
    public Sprite[] BodyAttack;
    public Sprite[] EquipNone;
    public Sprite[] EquipArmor;
    public Sprite[] EquipRobe;
    public Sprite[] Walk;
    public Sprite[] AttackNone;
    public Sprite[] AttackKnife;
    public Sprite[] AttackSword;
    public Sprite[] AttackAxe;
    public Sprite[] AttackWand;

    // base Visual for the body    // Body or BodyAttack
    public Image EquipmentVisual;  // EquipX
    public Image HandsVisual;      // Walk or AttackX
    public const float BarehandCooldown = 1;
    public const int BarehandDamage = 10;
    public int Experience;
    public int Gold;
    public Item Weapon;
    public Item Equipment;
    public Item Backpack;
    public int Potions;

    private int anim;
    private float nextAttack;
    private float attackingFor;
    private float lastRegen;
    private float cantCastBefore;

    public int level
    {
        get
        {
            var bar = 10;
            var level = 1;
            while (true)
            {
                if (Experience < bar)
                {
                    return level;
                }
                bar *= 2;
                level++;
            }
        }
    }

    public int attack
    {
        get
        {
            var baseDmg = (Weapon != Item.NONE) ? Weapon.GetAttack() : BarehandDamage;
            return Mathf.FloorToInt(baseDmg * (1f + level / 10f));
        }
    }

    public float range
    {
        get { return (Weapon != Item.NONE) ? Weapon.GetRange() : 1.5f; }
    }

    public float attackCooldown
    {
        get { return (Weapon != Item.NONE) ? Weapon.GetAttackCooldown() : BarehandCooldown; }
    }

    public int defense
    {
        get { return level + Equipment.GetDefense(); }
    }

    public float regenTick
    {
        get { return 1; }
    }

    public void GainExperience(int xp)
    {
        var levelBefore = level;
        Experience += xp;
        if (level != levelBefore)
        {
            Field.instance.ShowEffect(transform.localPosition, "LEVEL " + level + "!", Color.yellow);
            Debug.Log(Name + " level up to " + level);
            maxLife += 50 * (level - levelBefore);
            Life += 50 * (level - levelBefore);
        }
        else
        {
            Field.instance.ShowEffect(transform.localPosition, "+" + xp + " XP", Color.yellow);
        }
    }

    void Start()
    {
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        const float Speed = .4f;
        while (true)
        {
            if (hasPath)
            {
                anim = 1;
                yield return new WaitForSeconds(Speed);
                anim = 2;
                yield return new WaitForSeconds(Speed);
            }
            else
            {
                anim = 0;
                yield return null;
            }
        }
    }

    public override void FrameUpdate()
    {
        if (isDead)
        {
            return;
        }

        if (Time.time > lastRegen + regenTick)
        {
            var delta = level + 1;
            Life = Mathf.Clamp(Life + delta, 0, maxLife);
            lastRegen = Time.time;
        }

        NameUi.text = Name;
        //NameUi.text = string.Format("{0} EXP{1} H{2}/{3} M{4}/{5} G{6}", Name, Experience, Life, maxLife, Mana, maxMana, Gold);

        if (Name == "Picotachi-kun" && Input.GetMouseButtonDown(2))
        {
            var x = Mathf.FloorToInt(Input.mousePosition.x / GameConstants.CellSize) + 1;
            var y = Mathf.FloorToInt(Input.mousePosition.y / GameConstants.CellSize) + 1;
            ProcessInput("GO " + x + ":" + y);
        }

        var nearMonster = Field.instance.GetNearbyMonster(this, range);
        //Debug.LogWarning("nearMonster=" + nearMonster + " path?" + hasPath + " nextAttack=" + nextAttack);
        if (nearMonster != null && !hasPath)
        {
            TurnToward(nearMonster);
            if (nextAttack < Time.time)
            {
                nearMonster.TakeDamage(this, attack);
                nextAttack = Time.time + attackCooldown;
                attackingFor = .5f;
            }
        }

        var dirIdx = 0;
        switch (direction)
        {
            case Direction.Down:
                dirIdx = 0;
                break;

            case Direction.Left:
                dirIdx = 1;
                break;

            case Direction.Right:
                dirIdx = 2;
                break;

            case Direction.Up:
                dirIdx = 3;
                break;
        }

        Sprite[] equip;
        switch (Equipment)
        {
            case Item.ARMOR:
                equip = EquipArmor;
                break;

            case Item.ROBE:
                equip = EquipRobe;
                break;

            default:
                equip = EquipNone;
                break;
        }

        if (attackingFor > 0)
        {
            Visual.sprite = BodyAttack[dirIdx];
            EquipmentVisual.sprite = equip[dirIdx * 3 + anim];
            switch (Weapon)
            {
                case Item.KNIFE:
                    HandsVisual.sprite = AttackKnife[dirIdx];
                    break;

                case Item.SWORD:
                    HandsVisual.sprite = AttackSword[dirIdx];
                    break;

                case Item.AXE:
                    HandsVisual.sprite = AttackAxe[dirIdx];
                    break;

                case Item.WAND:
                    HandsVisual.sprite = AttackWand[dirIdx];
                    break;

                default:
                    HandsVisual.sprite = AttackNone[dirIdx];
                    break;
            }
        }
        else
        {
            Visual.sprite = Body[dirIdx];
            EquipmentVisual.sprite = equip[dirIdx * 3 + anim];
            HandsVisual.sprite = Walk[dirIdx];
        }
        attackingFor -= Time.deltaTime;
    }

    public bool Pickup(Item item, int quantity)
    {
        if (item.IsWeapon())
        {
            if (Weapon == Item.NONE)
            {
                Weapon = item;
                return true;
            }
            if (Backpack == Item.NONE)
            {
                Backpack = item;
                return true;
            }
            return false;
        }
        if (item.IsEquipment())
        {
            if (Equipment == Item.NONE)
            {
                Equipment = item;
                return true;
            }
            if (Backpack == Item.NONE)
            {
                Backpack = item;
                return true;
            }
            return false;
        }
        if (item == Item.POTION)
        {
            Potions++;
            return true;
        }
        if (item == Item.GOLD)
        {
            Gold += quantity;
            return true;
        }
        if (Backpack == Item.NONE)
        {
            Backpack = item;
            return true;
        }
        return false;
    }

    public bool Loose(Item item, int quantity)
    {
        if (item == Backpack)
        {
            Backpack = Item.NONE;
            return true;
        }
        if (item == Weapon)
        {
            Weapon = Item.NONE;
            return true;
        }
        if (item == Equipment)
        {
            Equipment = Item.NONE;
            return true;
        }
        if (item == Item.POTION && Potions > 0)
        {
            Potions--;
            return true;
        }
        if (item == Item.GOLD && Gold >= quantity)
        {
            Gold -= quantity;
            return true;
        }
        return false;
    }

    public void TakeDamage(Monster by, int damage)
    {
        if (isDead)
        {
            return;
        }

        Field.instance.ShowEffect(transform.localPosition, "-" + damage, Color.red);
        Life -= damage;
        lastRegen = Time.time;
        if (Life <= 0)
        {
            Die(true);
        }
    }

    public void Die(bool realDeath)
    {
        if (!isDead)
        {
            isDead = true;

            if (realDeath)
            {
                GameManager.instance.StoreEvent(Name, "Death", null);
                StartCoroutine(DieAnimation());
            }
            else
            {
                Destroy(gameObject);
            }

            Field.instance.players.Remove(Name);
        }
    }

    private IEnumerator DieAnimation()
    {
        var sayings = new[]
                      {
                          "*urgh* I die with honor!",
                          "*urgh* I die with passion!",
                          "*urgh* I die with rage!",
                          "*urgh* I die without fear!",
                          "*urgh* I die satisfied!",
                          "*urgh* I die too soon!",
                          "*urgh* I die for my village!",
                          "*urgh* I die for my everyone!",
                      };
        Say(0, sayings.RandomElement());
        GameManager.instance.SayOnIRC(Name, "You are dead :(");
        var startTime = Time.time;
        while (Time.time - startTime < 3)
        {
            var t = Mathf.InverseLerp(0, .5f, (Time.time - startTime) % .5f);
            Visual.transform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(0, 360, t));
            EquipmentVisual.transform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(0, 360, t));
            HandsVisual.transform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(0, 360, t));
            yield return null;
        }

        Visual.enabled = false;
        EquipmentVisual.enabled = false;
        HandsVisual.enabled = false;
        Visual.transform.localEulerAngles = Vector3.zero;
        EquipmentVisual.transform.localEulerAngles = Vector3.zero;
        HandsVisual.transform.localEulerAngles = Vector3.zero;

        yield return new WaitForSeconds(10);

        // Respawn
        Visual.enabled = true;
        EquipmentVisual.enabled = true;
        HandsVisual.enabled = true;
        var pos = Field.instance.posPerArea[Area.VILLAGE].RandomElement();
        transform.localPosition = new Vector3(pos.GetX() * GameConstants.CellSize, pos.GetY() * GameConstants.CellSize);
        Life = maxLife;
        isDead = false;
        Field.instance.players[Name] = this;
        Field.instance.ShowEffect(transform.localPosition, "RESPAWN", Color.white);
        GameManager.instance.SayOnIRC(Name, "You respawned at the VILLAGE.");
    }

    public void ProcessInput(string input)
    {
        if (input == null || isDead)
        {
            return;
        }
        input = input.Trim();
        if (input == "")
        {
            return;
        }
        var parts = input.Split(' ');
        var param0 = parts[0].ToUpperInvariant();
        var param1 = (parts.Length > 1) ? parts[1].ToUpperInvariant() : null;
        var coordParam = (parts.Length > 1) ? parts.Skip(1).Select(p => p.Split(':')).FirstOrDefault(i => i.Length == 2) : null;
        var itemParam = (parts.Length > 1) ? parts.Skip(1).Select(p => Utils.TryParse(p.ToUpperInvariant(), Item.NONE)).FirstOrDefault(i => i != Item.NONE) : Item.NONE;
        var areaParam = (parts.Length > 1) ? parts.Skip(1).Select(p => Utils.TryParse(p.ToUpperInvariant(), Area.NONE)).FirstOrDefault(i => i != Area.NONE) : Area.NONE;
        var numberParam = (parts.Length > 1) ? parts.Skip(1).Select(delegate(string p)
                                   {
                                       int i;
                                       int.TryParse(p.ToUpperInvariant(), out i);
                                       return i;
                                   }).FirstOrDefault(i => i != 0) : 0;

        SayQuickly(0, input);

        if (param0.StartsWith("INV") || param0.StartsWith("ITEM")) // INVentory, ITEMs
        {
            var possessions = new List<string>();
            if (Gold > 0)
            {
                possessions.Add(Gold + " " + Item.GOLD);
            }
            if (Weapon != Item.NONE)
            {
                possessions.Add("a " + Weapon);
            }
            if (Equipment != Item.NONE)
            {
                possessions.Add("a " + Equipment);
            }
            if (Backpack != Item.NONE)
            {
                possessions.Add("a " + Backpack + " in my backpack");
            }
            if (Potions > 0)
            {
                possessions.Add(Potions + " " + Item.POTION);
            }
            if (possessions.Count == 0)
            {
                Say(1, "I don't have anything :(");
                GameManager.instance.SayOnIRC(Name, "You don't have anything :(");
            }
            else
            {
                Say(1, "I have " + possessions.ProperEnumeration());
                GameManager.instance.SayOnIRC(Name, "You have " + possessions.ProperEnumeration().Replace(" my ", " your ") + ".");
            }
            return;
        }

        if (param0.StartsWith("STAT")) // STATs
        {
            var i = 1;
            var resp = "Level " + level + " (" + Experience + " XP). Life: " + Life + " / " + maxLife + " HP. ";
            Say(i, "I am level " + level + " (" + Experience + " XP)");
            resp += ". ";
            i += 2;
            Say(i, "I have " + Life + " / " + maxLife + " HP");
            i += 2;
            if (Equipment == Item.ROBE)
            {
                Say(i, "I can cast HEAL and ARMAGEDDON");
                resp += "Spells: HEAL and ARMAGEDDON. ";
                i += 2;
            }
            Say(i, "I attack for " + attack + " every " + attackCooldown.ToString("0.#") + "s");
            resp += "Attack: " + attack + " per " + attackCooldown.ToString("0.#") + "s. ";
            i += 2;
            Say(i, "I have a defense of " + defense);
            resp += "Defense: " + defense + ".";
            i += 2;
            GameManager.instance.SayOnIRC(Name, resp);
            return;
        }

        if (param0.StartsWith("WHERE"))
        {
            var area = Field.instance.GetArea(currentX, currentY);
            if (area != Area.NONE)
            {
                Say(1, "I am at " + currentX + ":" + currentY + " (" + area + ")");
                GameManager.instance.SayOnIRC(Name, "You are at " + currentX + ":" + currentY + " (" + area + ").");
            }
            else
            {
                Say(1, "I am at " + currentX + ":" + currentY);
                GameManager.instance.SayOnIRC(Name, "You are at " + currentX + ":" + currentY + ".");
            }
            return;
        }

        if (param0.StartsWith("GO"))
        {
            if (areaParam != Area.NONE)
            {
                MoveTo(areaParam);
                GameManager.instance.SayOnIRC(Name, "Moving to " + areaParam + "...");
            }
            else if (itemParam != Item.NONE)
            {
                var nearestDrop = Field.instance.GetNearestDrop(this, itemParam);
                if (nearestDrop != null)
                {
                    MoveTo(nearestDrop.currentX, nearestDrop.currentY, false);
                }
                else
                {
                    Say(1, "I don't see a " + itemParam + " around...");
                }
            }
            else if (coordParam != null)
            {
                int x, y;
                if (int.TryParse(coordParam[0], out x) && int.TryParse(coordParam[1], out y))
                {
                    if (MoveTo(x, y, false))
                    {
                        GameManager.instance.SayOnIRC(Name, "Moving to " + x + ":" + y + "...");
                        return;
                    }
                }
                Say(1, "I can't go there...");
                GameManager.instance.SayOnIRC(Name, "You can't go there.");
            }
            else if (param1 != null && Field.instance.players.Keys.Any(k => k.ToUpperInvariant() == param1))
            {
                var toPlayer = Field.instance.players.First(p => p.Key.ToUpperInvariant() == param1).Value;
                Say(1, "Moving toward " + toPlayer.Name + "...");
                MoveTo(toPlayer.currentX, toPlayer.currentY, false);
                GameManager.instance.SayOnIRC(Name, "Moving toward " + toPlayer.Name + "...");
            }
            else
            {
                Say(1, "Where is that?");
                GameManager.instance.SayOnIRC(Name, "Invalid destination.");
            }
            return;
        }

        if (param0.StartsWith("CAST") || param0.StartsWith("SPEL"))
        {
            if (Equipment != Item.ROBE)
            {
                Say(1, "I'm not a wizard...");
                GameManager.instance.SayOnIRC(Name, "You are not a wizard...");
            }
            else if (Time.time < cantCastBefore)
            {
                Say(1, "I'm too tired...");
                GameManager.instance.SayOnIRC(Name, "You are too tired...");
            }
            else if (parts.Skip(1).Any(p => p.ToUpperInvariant().StartsWith("HEAL")))
            {
                GameManager.instance.SayOnIRC(Name, "++ HEAL ++");
                Field.instance.ShowEffect(transform.localPosition, Color.white);
                Field.instance.ShowEffect(transform.localPosition, "++ HEAL ++", Color.white);
                foreach (var target in Field.instance.GetAllAggro(this, 4))
                {
                    if (target.Life < target.maxLife && !target.isDead)
                    {
                        var healed = target.maxLife - target.Life;
                        target.Life = target.maxLife;
                        if (target != this)
                        {
                            Field.instance.ShowEffect(target.transform.localPosition, "+" + healed + " HP", Color.cyan);
                        }
                    }
                }
                cantCastBefore = Time.time + 30;
            }
            else if (parts.Skip(1).Any(p => p.ToUpperInvariant().StartsWith("ARMA")))
            {
                GameManager.instance.SayOnIRC(Name, "++ ARMAGEDDON ++");
                Field.instance.ShowEffect(transform.localPosition, Color.red);
                Field.instance.ShowEffect(transform.localPosition, "++ ARMAGEDDON ++", Color.black);
                foreach (var target in Field.instance.GetAllNearbyMonsters(this, 4).ToArray()) // ToArray() because they might die in the process...
                {
                    target.TakeDamage(this, 1000);
                }
                cantCastBefore = Time.time + 30;
            }
            else
            {
                Say(1, "I don't know this spell...");
            }
            return;
        }

        if (param0.StartsWith("CHARGE"))
        {
            var nearestMonster = Field.instance.GetNearestMonster(this);
            if (nearestMonster != null)
            {
                MoveTo(nearestMonster.currentX, nearestMonster.currentY, true);
            }
            else
            {
                Say(1, "No monster around here...?");
            }
            return;
        }

        if (param0.StartsWith("USE") || param0.StartsWith("EQUIP") || param0.StartsWith("DRINK"))
        {
            if (itemParam != Item.NONE)
            {
                if (itemParam == Item.POTION)
                {
                    if (Potions == 0)
                    {
                        Say(1, "I don't have any POTION :(");
                    }
                    else if (Life == maxLife)
                    {
                        Say(1, "I'm already in perfect health!");
                    }
                    else
                    {
                        Potions--;
                        Life = maxLife;
                        Say(1, "*gulp*");
                        Field.instance.ShowEffect(transform.localPosition, "FULL HEALTH", Color.green);
                    }
                }
                else if (itemParam == Backpack && itemParam.IsWeapon())
                {
                    Backpack = Weapon;
                    Weapon = itemParam;
                    Say(1, "I equipped my " + itemParam);
                }
                else if (itemParam == Backpack && itemParam.IsEquipment())
                {
                    Backpack = Equipment;
                    Equipment = itemParam;
                    Say(1, "I equipped my " + itemParam);
                }
                else
                {
                    Say(1, "I can't use *this*...");
                }
            }
            else
            {
                Say(1, "I don't know this item :(");
            }
            return;
        }

        if (param0.StartsWith("PICKUP") || param0.StartsWith("TAKE"))
        {
            var picked = 0;
            foreach (var drop in Field.instance.GetDropNear(this))
            {
                if (drop.Pickup(this))
                {
                    picked++;
                }
            }
            if (picked == 0)
            {
                Say(1, "The is nothing to pickup here :(");
            }
            return;
        }

        if (param0.StartsWith("DROP") || param0.StartsWith("THROW"))
        {
            if (numberParam == 0)
            {
                numberParam = 1;
            }
            Debug.LogWarning(itemParam + " " + numberParam + " " + Gold);
            var nearDrop = Field.instance.GetDropAt(this);
            if (nearDrop != null)
            {
                Say(1, "There is already something here :(");
            }
            else if (Loose(itemParam, numberParam))
            {
                var dropObject = (GameObject) Instantiate(Field.instance.DropPrefab);
                dropObject.transform.parent = Field.instance.transform;
                dropObject.transform.localPosition = new Vector3(currentX * GameConstants.CellSize, currentY * GameConstants.CellSize);
                dropObject.name = itemParam + " @ " + currentX + ":" + currentY;
                var drop = dropObject.GetComponent<Drop>();
                drop.Item = itemParam;
                drop.Quantity = numberParam;
                Field.instance.drops.Add(drop);
            }
            else
            {
                Say(1, "I don't have that :(");
            }
            return;
        }

        if (param0.StartsWith("HELP") || param0.StartsWith("?"))
        {
            if (Experience == 0)
            {
                Say(1, "I should GO TO THE LAKE and kill some bugs.");
                GameManager.instance.SayOnIRC(Name, "You should GO TO THE LAKE and kill some bugs.");
            }
            else if (Weapon == Item.NONE)
            {
                Say(1, "I should buy a KNIFE at the SMITH...");
                GameManager.instance.SayOnIRC(Name, "You should should buy a KNIFE at the SMITH...");
            }
            else if (Equipment == Item.NONE)
            {
                Say(1, "Getting an ARMOR at the SMITH would help...");
                GameManager.instance.SayOnIRC(Name, "Getting an ARMOR at the SMITH would help...");
            }
            else if (level < 4)
            {
                Say(1, "Might get some experience in the DESERT...");
                GameManager.instance.SayOnIRC(Name, "You might get some experience in the DESERT...");
            }
            else if (level < 8)
            {
                Say(1, "Might get some experience at the CAMPFIRE...");
                GameManager.instance.SayOnIRC(Name, "You might get some experience at the CAMPFIRE...");
            }
            else
            {
                Say(1, "I think I'm ready for the BOSS...");
                GameManager.instance.SayOnIRC(Name, "You should be ready for the BOSS, now!");
            }
            return;
        }

        if (param0.StartsWith("SPEAK") || param0.StartsWith("HEL") || param0.StartsWith("TALK"))
        {
            var nearNpc = Field.instance.GetNearbyNpc(this);
            if (nearNpc == null)
            {
                Say(1, "No-one around :(");
            }
            else
            {
                nearNpc.Speak(this);
            }
            return;
        }

        if (param0.StartsWith("HEAL"))
        {
            var nearNpc = Field.instance.GetNearbyNpc(this);
            if (nearNpc == null)
            {
                Say(1, "No-one around :(");
            }
            else
            {
                nearNpc.RequestHeal(this);
            }
            return;
        }

        if (param0.StartsWith("BUY"))
        {
            var nearNpc = Field.instance.GetNearbyNpc(this);
            if (nearNpc == null)
            {
                Say(1, "No merchant around :(");
            }
            else
            {
                nearNpc.RequestBuy(this, itemParam);
            }
            return;
        }

        if (param0.StartsWith("SELL")) 
        {
            var nearNpc = Field.instance.GetNearbyNpc(this);
            if (nearNpc == null)
            {
                Say(1, "No merchant around :(");
            }
            else
            {
                nearNpc.RequestSell(this, itemParam);
            }
            return;
        }

        Say(0, input);
    }

}
