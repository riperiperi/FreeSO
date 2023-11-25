using FSO.Common.Enum;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Common.Domain.Top100
{
    public class Top100Domain : ITop100Domain
    {
        private List<Top100CategoryEntry> _Categories;

        public Top100Domain()
        {
            _Categories = new List<Top100CategoryEntry>();


            var lotCategories = new Top100Category[] { Top100Category.lot_money, Top100Category.lot_offbeat, Top100Category.lot_romance, Top100Category.lot_services, Top100Category.lot_shopping,
                                                    Top100Category.lot_skills, Top100Category.lot_welcome, Top100Category.lot_games, Top100Category.lot_entertainment, Top100Category.lot_residence};
            var lotLabels = new string[] { "Money", "Offbeat", "Romance", "Services", "Shopping", "Skills", "Welcome", "Games", "Entertainment", "Residence" };

            for(var i=0; i < lotCategories.Length; i++)
            {
                _Categories.Add(new Top100CategoryEntry {
                    Id = (uint)lotCategories[i],
                    Category = lotCategories[i],
                    CategoryType = Top100CategoryType.LOT,
                    Name = lotLabels[i]
                });
            }

            var avatarCategories = new Top100Category[] {
                Top100Category.avatar_most_famous,
                Top100Category.avatar_best_karma,
                Top100Category.avatar_friendliest,
                Top100Category.avatar_most_infamous,
                Top100Category.avatar_meanest
            };
            var avatarLabels = new string[] { "Most Famous", "Best Karma", "Friendliest", "Most Infamous", "Meanest" };

            for(var i=0; i < avatarCategories.Length; i++)
            {
                _Categories.Add(new Top100CategoryEntry
                {
                    Id = (uint)avatarCategories[i],
                    Category = avatarCategories[i],
                    CategoryType = Top100CategoryType.AVATAR,
                    Name = avatarLabels[i]
                });
            }
        }

        public IEnumerable<Top100CategoryEntry> Categories
        {
            get { return _Categories; }
        }

        public Top100CategoryEntry Get(uint id)
        {
            return Get((Top100Category)id);
        }

        public Top100CategoryEntry Get(Top100Category category)
        {
            return _Categories.FirstOrDefault(x => x.Category == category);
        }
    }

    public class Top100CategoryEntry
    {
        public uint Id;
        public Top100Category Category;
        public Top100CategoryType CategoryType;
        public string Name;
    }
}
