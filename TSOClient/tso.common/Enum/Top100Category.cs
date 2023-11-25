using System;

namespace FSO.Common.Enum
{
    public enum Top100Category
    {
        lot_money = 1,
        lot_offbeat = 2,
        lot_romance = 3,
        lot_services = 4,
        lot_shopping = 5,
        lot_skills = 6,
        lot_welcome = 7,
        lot_games = 8,
        lot_entertainment = 9,
        lot_residence = 10,

        avatar_most_famous = 11,
        avatar_best_karma = 12,
        avatar_friendliest = 13,
        avatar_most_infamous = 14,
        avatar_meanest = 15
    }

    public static class Top100CategoryUtils
    {
        public static bool IsAvatarCategory(this Top100Category category)
        {
            return !category.IsLotCategory();
        }

        public static bool IsLotCategory(this Top100Category category)
        {
            switch (category)
            {
                case Top100Category.lot_money:
                case Top100Category.lot_offbeat:
                case Top100Category.lot_romance:
                case Top100Category.lot_services:
                case Top100Category.lot_shopping:
                case Top100Category.lot_skills:
                case Top100Category.lot_welcome:
                case Top100Category.lot_games:
                case Top100Category.lot_entertainment:
                case Top100Category.lot_residence:
                    return true;
                default:
                    return false;
            }
        }

        public static Top100Category FromLotCategory(LotCategory category)
        {
            switch (category)
            {
                case LotCategory.money:
                    return Top100Category.lot_money;
                case LotCategory.offbeat:
                    return Top100Category.lot_offbeat;
                case LotCategory.romance:
                    return Top100Category.lot_romance;
                case LotCategory.services:
                    return Top100Category.lot_services;
                case LotCategory.shopping:
                    return Top100Category.lot_shopping;
                case LotCategory.skills:
                    return Top100Category.lot_skills;
                case LotCategory.welcome:
                    return Top100Category.lot_welcome;
                case LotCategory.games:
                    return Top100Category.lot_games;
                case LotCategory.entertainment:
                    return Top100Category.lot_entertainment;
                case LotCategory.residence:
                    return Top100Category.lot_residence;
            }
            throw new Exception("Unknown lot category");
        }

        public static LotCategory ToLotCategory(this Top100Category category)
        {
            switch (category)
            {
                case Top100Category.lot_money:
                    return LotCategory.money;
                case Top100Category.lot_offbeat:
                    return LotCategory.offbeat;
                case Top100Category.lot_romance:
                    return LotCategory.romance;
                case Top100Category.lot_services:
                    return LotCategory.services;
                case Top100Category.lot_shopping:
                    return LotCategory.shopping;
                case Top100Category.lot_skills:
                    return LotCategory.skills;
                case Top100Category.lot_welcome:
                    return LotCategory.welcome;
                case Top100Category.lot_games:
                    return LotCategory.games;
                case Top100Category.lot_entertainment:
                    return LotCategory.entertainment;
                case Top100Category.lot_residence:
                    return LotCategory.residence;
            }
            return LotCategory.none;
        }
    }

    public enum Top100CategoryType
    {
        AVATAR = 1,
        LOT = 2
    }
}
