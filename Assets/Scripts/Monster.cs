using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : Entity
{

    public enum State
    {
        Wandering,
        Aggro,
    }

    [Serializable]
    public class DropChance
    {
        public Item Item = Item.GOLD;
        public int Quantity = 1;
        public float Weight;
    }

    public static readonly Color DamageColor = new Color(248f / 255, 128f / 255, 0);
    public Sprite[] Sprites;
    public float AnimationSpeed = .4f;
    public bool AnimateOnlyDuringAggro;
    public float AttackRange = 1.5f;
    public float AggroRange = 5;
    public int Damage;
    public float AttackCooldown;
    public int ExperienceGiven;
    public Area StayInArea;
    public DropChance[] DropTable;
    public int Regen;

    public State state { get; private set; }
    public Player aggro { get; private set; }
    public float cantAggroBefore { get; private set; }
    
    private readonly Dictionary<string, int> damageByPlayer = new Dictionary<string, int>();
    private float nextAttack;
    private int anim;
    private float lastRegen;

    public float distanceToAggro
    {
        get
        {
            if (!aggro || aggro.isDead) // aggro is null or dead
            {
                return float.PositiveInfinity;
            }
            return (transform.localPosition - aggro.transform.localPosition).magnitude;
        }
    }


    void Start()
    {
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        while (true)
        {
            if (AnimateOnlyDuringAggro)
            {
                if (state == State.Aggro)
                {
                    anim = (anim + 1) % Sprites.Length;
                    yield return new WaitForSeconds(AnimationSpeed);
                }
                else
                {
                    anim = 0;
                    yield return null;
                }
            }
            else
            {
                anim = (anim + 1) % Sprites.Length;
                yield return new WaitForSeconds(AnimationSpeed);
            }
        }
    }

    public override void FrameUpdate()
    {
        Visual.sprite = Sprites[anim];
        Visual.transform.localEulerAngles = new Vector3(0, 0, (int) direction);

        if (Regen > 0)
        {
            if (lastRegen + 1 < Time.time)
            {
                Life = Mathf.Clamp(Life + Regen, 0, maxLife);
                lastRegen = Time.time;
            }
        }

        switch (state)
        {
            case State.Aggro:
                {
                    if (!aggro || aggro.isDead) // aggro dead or lost
                    {
                        LooseAggro();
                    }
                    else if (distanceToAggro < AttackRange * GameConstants.CellSize) // close enough to attack
                    {
                        TurnToward(aggro);
                        if (nextAttack < Time.time)
                        {
                            aggro.TakeDamage(this, Damage);
                            nextAttack = Time.time + AttackCooldown;
                            // TODO anim
                        }
                    }
                    else if (distanceToAggro > AggroRange * GameConstants.CellSize) // too far
                    {
                        LooseAggro();
                    }
                    else if (StayInArea != Area.NONE && Field.instance.GetArea(currentX, currentY) != StayInArea) // leave area
                    {
                        LooseAggro();
                    }
                    else if (!hasPath)
                    {
                        MoveTo(aggro.currentX, aggro.currentY, true);
                    }
                }
                break;

            case State.Wandering:
                {
                    if (Time.time > cantAggroBefore)
                    {
                        aggro = Field.instance.GetAggro(this, AggroRange);
                        if (aggro != null)
                        {
                            state = State.Aggro;
                            MoveTo(aggro.currentX, aggro.currentY, true);
                        }
                        cantAggroBefore = Time.time + 2;
                    }
                    else
                    {
                        if (!hasPath)
                        {
                            WanderInArea();
                        }
                    }
                }
                break;
        }
    }

    private void LooseAggro()
    {
        state = State.Wandering;
        aggro = null;
        path = null;
        cantAggroBefore = Time.time + 2;
    }

    public override void OnMovedOneStep()
    {
        if (state == State.Aggro && aggro)
        {
            //Debug.LogWarning("OnMovedOneStep() path=" + path.PathRepr());
            MoveTo(aggro.currentX, aggro.currentY, true);
            //Debug.LogWarning("AFTER OnMovedOneStep() path=" + path.PathRepr());
        }
    }

    public void WanderInArea()
    {
        if (StayInArea != Area.NONE)
        {
            var pos = Field.instance.posPerArea[StayInArea].RandomElement();
            MoveTo(pos.GetX(), pos.GetY(), false);
        }
    }

    public void TakeDamage(Player by, int damage)
    {
        Field.instance.ShowEffect(transform.localPosition, "-" + damage, DamageColor);
        damageByPlayer.Incr(by.Name, damage);
        Life -= damage;
        if (Life <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        foreach (var dmg in damageByPlayer)
        {
            Player player;
            if (Field.instance.players.TryGetValue(dmg.Key, out player))
            {
                player.GainExperience(ExperienceGiven); // TODO share experience !
            }
        }

        Drop();

        Destroy(gameObject);
        Field.instance.monsters.Remove(this);
    }

    private void Drop()
    {
        var existingDrop = Field.instance.GetDropAt(this);
        if (existingDrop != null)
        {
            return; // cannot drop twice on the same tile
        }

        var dropTable = new ProbabilityTable<DropChance>();
        foreach (var d in DropTable)
        {
            dropTable.Add(d, d.Weight);
        }
        var dropChance = dropTable.ChooseOne();
        if (dropChance.Item != Item.NONE)
        {
            var dropObject = (GameObject) Instantiate(Field.instance.DropPrefab);
            dropObject.transform.parent = Field.instance.transform;
            dropObject.transform.localPosition = new Vector3(currentX * GameConstants.CellSize, currentY * GameConstants.CellSize);
            dropObject.name = dropChance.Item + " @ " + currentX + ":" + currentY;
            var drop = dropObject.GetComponent<Drop>();
            drop.Item = dropChance.Item;
            drop.Quantity = dropChance.Quantity;
            Field.instance.drops.Add(drop);
        }
    }

}
