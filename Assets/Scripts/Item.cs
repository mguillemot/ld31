public enum Item
{
    NONE,

    // Misc
    GOLD = 1,
    POTION,
    DIAMOND,       // drop, just for money

    // Weapons
    KNIFE = 100,   // sold at blacksmith
    SWORD,         // drop on low level monsters
    AXE,           // become WARRIOR
    WAND,          // become MAGE

    // Equipment
    ARMOR = 200,   // + defense
    ROBE,          // + mana
}

public static class ItemUtils
{

    public static int GetBuyPrice(this Item item)
    {
        switch (item)
        {
            case Item.KNIFE:
                return 100;

            case Item.ARMOR:
                return 1000;

            case Item.POTION:
                return 50;

            default:
                return 0;
        }
    }

    public static int GetSellPrice(this Item item)
    {
        switch (item)
        {
            case Item.KNIFE:
                return 50;

            case Item.SWORD:
                return 500;

            case Item.AXE:
                return 1000;

            case Item.WAND:
                return 1000;

            case Item.ARMOR:
                return 500;

            case Item.POTION:
                return 25;

            case Item.DIAMOND:
                return 1000;

            default:
                return 0;
        }
    }

    public static int GetHeal(this Item item)
    {
        switch (item)
        {
            case Item.POTION:
                return 100;

            default:
                return 0;
        }
    }

    public static bool IsWeapon(this Item item)
    {
        return (item.GetAttack() > 0);
    }

    public static int GetAttack(this Item item)
    {
        switch (item)
        {
            case Item.KNIFE:
                return 20;

            case Item.SWORD:
                return 80;

            case Item.AXE:
                return 300;

            case Item.WAND:
                return 400;

            default:
                return 0;
        }
    }

    public static float GetRange(this Item item)
    {
        switch (item)
        {
            case Item.KNIFE:
                return 1.5f;

            case Item.SWORD:
                return 2;

            case Item.AXE:
                return 2;

            case Item.WAND:
                return 5;

            default:
                return 0;
        }
    }

    public static float GetAttackCooldown(this Item item)
    {
        switch (item)
        {
            case Item.KNIFE:
                return 1.5f;

            case Item.SWORD:
                return 2;

            case Item.AXE:
                return 3;

            case Item.WAND:
                return 5;

            default:
                return float.NaN;
        }
    }

    public static bool IsEquipment(this Item item)
    {
        return (item.GetDefense() > 0); // TODO
    }

    public static int GetDefense(this Item item)
    {
        switch (item)
        {
            case Item.ARMOR:
                return 10;

            case Item.ROBE:
                return 3;

            default:
                return 0;
        }
    }

}
