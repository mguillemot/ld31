using System.Linq;
using UnityEngine;

public class Npc : Entity
{

    public bool Smith;
    public Item[] Buyable;
    public Item[] Sellable;

    public void Speak(Player player)
    {
        if (Smith)
        {
            Say(1, "Hi lad! I'm Jack, the blacksmith.");
            Say(4, "I BUY and SELL sturdy tools!");
        }
        else
        {
            Say(1, "Hello young one! I am Mellisandre.");
            Say(4, "I BUY and SELL magic tools.");
            Say(7, "I also HEAL wounded people.");
        }
    }

    public void RequestHeal(Player player)
    {
        if (Smith)
        {
            Say(1, "You don't want me to do that to you, lad...");
        }
        else
        {
            if (player.Life == player.maxLife)
            {
                Say(1, "You are already in perfect health, boy!");
            }
            else
            {
                player.Life = player.maxLife;
                Say(1, "Here, may the True One guide you!");
                Field.instance.ShowEffect(transform.localPosition, "FULL HEALTH", Color.green);
            }
        }
    }

    public void RequestBuy(Player player, Item item)
    {
        if (item == Item.NONE)
        {
            if (Smith)
            {
                Say(1, "I sell " + Buyable.Select(i => i + " (" + i.GetBuyPrice() + " GOLD)").ToList().ProperEnumeration() + ", lad!");
            }
            else
            {
                Say(1, "Wonderful! I sell " + Buyable.Select(i => i + " (" + i.GetBuyPrice() + " GOLD)").ToList().ProperEnumeration());
            }
        }
        else if (Buyable.Contains(item))
        {
            var price = item.GetBuyPrice();
            if (player.Gold < price)
            {
                if (Smith)
                {
                    Say(1, "You don't have the " + price + " GOLD, lad!");
                }
                else
                {
                    Say(1, "You don't have the " + price + " GOLD, boy!");
                }
            }
            else if (!player.Pickup(item, 1))
            {
                if (Smith)
                {
                    Say(1, "You should empty your pockets first, lad!");
                }
                else
                {
                    Say(1, "You are already burdened, my poor...");
                }
            }
            else
            {
                if (Smith)
                {
                    Say(1, "Here your new " + item + ", lad!");
                }
                else
                {
                    if (item == Item.POTION)
                    {
                        Say(1, "Drink it with care, boy...");
                    }
                    else
                    {
                        Say(1, "Take great care of it, boy...");
                    }
                }
                player.Loose(Item.GOLD, price);
                Field.instance.ShowEffect(transform.localPosition, "-" + price + " GOLD", Color.yellow);
            }
        }
        else
        {
            if (Smith)
            {
                Say(1, "I don't have that in stock, lad.");
            }
            else
            {
                Say(1, "Unfortunately I don't sell such merchandise.");
            }
        }
    }

    public void RequestSell(Player player, Item item)
    {
        if (item == Item.NONE)
        {
            if (Smith)
            {
                Say(1, "I buy all and weapons and armors, lad!");
            }
            else
            {
                Say(1, "I buy any magic item, boy.");
            }
        }
        else if (Sellable.Contains(item) && player.Loose(item, 1))
        {
            var price = item.GetSellPrice();
            if (Smith)
            {
                Say(1, "Sure, here's your " + price + " GOLD, lad!");
            }
            else
            {
                Say(1, "Here is your " + price + " GOLD.");
            }
            player.Pickup(Item.GOLD, price);
        }
        else
        {
            if (Smith)
            {
                Say(1, "I don't buy that kind of stuff, lad.");
            }
            else
            {
                Say(1, "I don't buy this, boy.");
            }
        }
    }

}
