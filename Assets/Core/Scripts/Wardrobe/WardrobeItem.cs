using System;
using System.Collections.Generic;
namespace Game
{
    [Serializable]
    public struct WardrobeItem
    {
        public ushort id;
        public byte plus;
        public bool isUsed => id > 0;
        public ScriptableWardrobe data
        {
            get
            {
                // show a useful error message if the key can't be found note: ScriptableWardrobe.OnValidate 'is in resource folder' check causes Unity SendMessage warnings and false positives. this solution is a lot better.      
                if(!ScriptableWardrobe.dict.ContainsKey(id))
                    throw new KeyNotFoundException("There is no ScriptableWardrobe with hash=" + id + ". Make sure that all ScriptableItems are in the Resources folder so they are loaded properly.");
                return ScriptableWardrobe.dict[id];
            }
        }
        public int hpBonus => data.hp.Get(plus);
        public WardrobeItem(ushort id = 0, byte plus = 0)
        {
            this.id = id;
            this.plus = plus;
        }
        public void Set(Game.DatabaseModels.Clothing clothing)
        {
            id = clothing.id;
            plus = clothing.plus;
        }
        public void UnEquip()
        {
            id = 0;
            plus = 0;
        }
        public Item GetInventoryItem()
        {
            if(id == 0 || data == null)
                return default;
            return new Item
            {
                id = data.itemId,
                plus = plus
            };
        }
    }
}
