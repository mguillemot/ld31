using UnityEngine;

public class Drop : Entity
{

    public Sprite Potion;
    public Sprite Gold;
    public Sprite Diamond;
    public Sprite Knife;
    public Sprite Sword;
    public Sprite Axe;
    public Sprite Wand;
    public Sprite Armor;
    public Sprite Robe;

    public Item Item;
    public int Quantity = 1;

    private float spawnAt;

    void Start()
    {
        spawnAt = Time.time;
    }

    void Update()
    {
        if (Time.time - spawnAt > 90)
        {
            Die();
        }

        switch (Item)
        {
            case Item.GOLD:
                Visual.sprite = Gold;
                break;

            case Item.POTION:
                Visual.sprite = Potion;
                break;

            case Item.DIAMOND:
                Visual.sprite = Diamond;
                break;

            case Item.KNIFE:
                Visual.sprite = Knife;
                break;

            case Item.SWORD:
                Visual.sprite = Sword;
                break;

            case Item.AXE:
                Visual.sprite = Axe;
                break;

            case Item.WAND:
                Visual.sprite = Wand;
                break;

            case Item.ARMOR:
                Visual.sprite = Armor;
                break;

            case Item.ROBE:
                Visual.sprite = Robe;
                break;
        }
    }

    public bool Pickup(Player player)
    {
        if (Item.IsWeapon())
        {
            if (player.Weapon == Item.NONE)
            {
                player.Say(1, "I'm using a " + Item + "!");
                player.Weapon = Item;
                Die();
                return true;
            }
            else if (player.Backpack == Item.NONE)
            {
                player.Say(1, "I put a " + Item + " in my backpack!");
                player.Backpack = Item;
                Die();
                return true;
            }
            else
            {
                player.Say(1, "I'm already full :(");
                return false;
            }
        }
        else if (Item.IsEquipment())
        {
            if (player.Equipment == Item.NONE)
            {
                player.Say(1, "I'm using a " + Item + "!");
                player.Equipment = Item;
                Die();
                return true;
            }
            else if (player.Backpack == Item.NONE)
            {
                player.Say(1, "I put a " + Item + " in my backpack!");
                player.Backpack = Item;
                Die();
                return true;
            }
            else
            {
                player.Say(1, "I'm already full :(");
                return false;
            }
        }
        else if (Item == Item.POTION)
        {
            player.Potions++;
            player.Say(0, "I now have " + player.Potions + " POTION!");
            Die();
            return true;
        }
        else if (Item == Item.GOLD)
        {
            player.Gold += Quantity;
            player.Say(0, "I now have " + player.Gold + " GOLD!");
            Die();
            return true;
        }
        else // diamond
        {
            if (player.Backpack == Item.NONE)
            {
                player.Say(1, "I put a " + Item + " in my backpack!");
                player.Backpack = Item;
                Die();
                return true;
            }
            else
            {
                player.Say(1, "I'm already full :(");
                return false;
            }
        }
    }

    public void Die()
    {
        Destroy(gameObject);
        Field.instance.drops.Remove(this);
    }

}
