using UnityEngine;
using Mirror;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite; // from https://github.com/praeclarum/sqlite-net
using System.Linq;
using UnityEngine.AI;
//using Game.Components;
using Game.DatabaseModels;
using nm = Game.Network.Messages;
/*
    TODO: Replace every multi-record save [InsertOrReplace] with [Insert] that deletes records first
*/
namespace Game
{
    public class Database : MonoBehaviour
    {
        public static Database singleton;
        public string databaseFile = "Database.sqlite";
        SQLiteConnection connection;
        void Awake() => singleton = this;
    #region Character
        public bool CharacterExists(string characterName)
        {
            return connection.FindWithQuery<Characters>("SELECT * FROM Characters WHERE name=?", characterName) != null;
        }
        public int CharactersCount(ulong accId) => connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Characters WHERE accId=?", accId);
        public async Task<nm.CharactersAvailable> LoadAvailableCharacters(ulong accId)
        {
            nm.CharactersAvailable message = new nm.CharactersAvailable();
            List<Characters> table = connection.Query<Characters>("SELECT id, name, classType, classRank, gender, avatar, level, tribeId, showWardrobe FROM Characters WHERE accId=?", accId);
            if(table.Count > 0)
            {
                message.characters = new nm.CharactersAvailable.CharacterPreview[table.Count];
                List<Task> tasks = new List<Task>();
                for(int io = 0; io < table.Count; io++)
                {
                    int i = io;
                    tasks.Add(Task.Run(() =>
                    {
                        message.characters[i].id = table[i].id.Value;
                        message.characters[i].name = table[i].name;
                        message.characters[i].classInfo = new PlayerClassData(table[i].classType, table[i].classRank);
                        message.characters[i].gender = table[i].gender;
                        message.characters[i].level = table[i].level;
                        message.characters[i].tribeId = table[i].tribeId;
                        message.characters[i].avatar = table[i].avatar;
                        message.characters[i].showWardrobe = table[i].showWardrobe;

                        // clothing
                        message.characters[i].wardrobe = new ushort[4];
                        if(table[i].showWardrobe)
                        {
                            List<Clothing> clothing = connection.Query<Clothing>("SELECT id FROM Clothing WHERE owner=?", table[i].id.Value);
                            if(clothing.Count > 0)
                            {
                                for(int c = 0; c < clothing.Count; c++)
                                    message.characters[i].wardrobe[(int)ScriptableWardrobe.dict[clothing[c].id].category] = clothing[c].id;
                            }
                        }
                        // equipment
                        message.characters[i].equipment = new EquipmentPreview[2];
                        List<Equipments> equips = connection.Query<Equipments>("SELECT id, quality, slot FROM Equipments WHERE owner=? AND slot=? OR slot=?", table[i].id.Value, (int)EquipmentsCategory.Weapon, (int)EquipmentsCategory.Armor);
                        if(equips.Count > 0)
                        {
                            for(int e = 0; e < equips.Count; e++)
                            {
                                int slot = equips[e].slot == (int)EquipmentsCategory.Armor ? 0 : 1;
                                message.characters[i].equipment[slot].id = equips[e].id;
                                message.characters[i].equipment[slot].quality = equips[e].quality;
                            }
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }
            return message;
        }
        public async Task<GameObject> CharacterLoad(uint characterId, ulong accId)
        {
            Characters row = connection.FindWithQuery<Characters>("SELECT * FROM Characters WHERE id=? AND accId=?", characterId, accId);
            if(row != null) {
                if(ScriptableClass.dict.ContainsKey(row.classType)) {
                    GameObject go = Instantiate(ScriptableClass.dict[row.classType].prefab);
                    Player player = go.GetComponent<Player>();
                    await SetCharacterData(player, row);
                    // set place in world
                    player.Warp(new Vector3(row.x, row.y, row.z));
                    StartCoroutine(player.CheckUps(row.lastsaved));
                    
                    return go;
                } else Debug.LogError("no prefab found for class: " + row.classType.ToString());
            }
            return null;
        }
        public async Task SetCharacterData(Player player, Characters row)
        {
            List<Task> tasks = new List<Task>();
            //info
            player.id = row.id.Value;
            player.name = row.name;
            player.accId = row.accId;
            player.classInfo = new PlayerClassData(row.classType, row.classRank);
            player.gender = row.gender;
            player.level = row.level;
            player.own.experience = row.experience;
            player.avatar = row.avatar;
            player.frame = row.frame;
            player.cityId = row.city;
            player.privacy = row.privacy;
            player.activeTitle = row.activeTitle;
            player.teamId = row.teamId;
            player.showWardrop = row.showWardrobe;
            player.own.createdAt = row.createdAt;
            player.own.shareExpWithPet = row.shareExpWithPet;
            player.suspiciousActivities = row.suspiciousActivities;
            // attributes
            player.AvailableFreeRespawn = row.AvailableFreeRespawn;
            player.own.inventorySize = row.inventorySize;
            player.own.popularity = row.popularity;
            player.own.vitality = row.vitality;
            player.own.strength = row.strength;
            player.own.intelligence = row.intelligence;
            player.own.endurance = row.endurance;
            player.own.freepoints = row.freepoints;
            // currency
            player.own.gold = row.gold;
            player.own.diamonds = row.diamonds;
            player.own.b_diamonds = row.b_diamonds;
            // tribe
            player.tribeId = row.tribeId;
            player.own.tribeRank = row.tribeRank;
            player.own.tribeGoldContribution = row.tribeGoldContribution;
            player.own.tribeDiamondContribution = row.tribeDiamondContribution;
            // military
            player.own.TodayHonor = row.todayHonor;
            player.own.TotalHonor = row.totalHonor;
            player.own.killStrike = row.killCount;
            player.own.MonsterPoints = row.MonsterPoints;
            player.own.militaryRank = row.militaryRank;
            // daily
            player.own.dailyQuests = row.dailyQuests;
            player.own.tribeQuests = row.tribeQuests;
            player.own.arena1v1Points = row.arena1v1Points;
            player.own.arena1v1WinsToday = row.arena1v1WinsToday;
            player.own.arena1v1LossesToday = row.arena1v1LossesToday;
            player.own.SetDailySignInInfo(row.dailySign);

            tasks.Add(Task.Run(() => LoadEquipment(player)));
            tasks.Add(Task.Run(() => LoadInventory(player)));
            tasks.Add(Task.Run(() => LoadAccessories(player)));
            tasks.Add(Task.Run(() => LoadWardrobe(player)));
            tasks.Add(Task.Run(() => LoadSkills(player)));
            tasks.Add(Task.Run(() => LoadBuffs(player)));
            tasks.Add(Task.Run(() => LoadVIP(player, row.vipLevel)));
            tasks.Add(Task.Run(() => LoadPets(player, row.activePet)));
            tasks.Add(Task.Run(() => LoadMounts(player, row.activeMount)));
            tasks.Add(Task.Run(() => LoadTitles(player)));
            tasks.Add(Task.Run(() => LoadQuests(player)));
            tasks.Add(Task.Run(() => LoadGuild(player, row.guildId)));
            tasks.Add(Task.Run(() => LoadTeam(player)));
            tasks.Add(Task.Run(() => LoadAchievements(player)));
            
            if(row.spouseId > 0) 
                tasks.Add(Task.Run(() => LoadMarriage(player, row.spouseId)));
            
            player.own.tribe = TribeSystem.tribes[row.tribeId];
            TribeSystem.OnlineTribesMembers[row.tribeId].Add(player.id);

            await Task.WhenAll(tasks);

            player.health = player.healthMax;
            player.mana = player.manaMax;
        }
        public void SetCharacterOnline(uint id)
        {
            connection.Execute("UPDATE characters SET online=1 WHERE id=?", id);
        }
        public void CharacterCreate(Player player)
        {
            connection.BeginTransaction();// only use a transaction if not called within SaveMany transaction
            player.cityId = (byte)(player.tribeId - 1);
            connection.InsertOrReplace(new Characters { // in case creating new Character
                id = null,
                name = player.name,
                accId = player.accId,
                classType = player.classInfo.type,
                classRank = player.classInfo.rank,
                gender = player.gender,
                avatar = player.avatar,
                city = player.cityId,
                x = Storage.data.cities[player.cityId].StartingPoint().x,
                y = Storage.data.cities[player.cityId].StartingPoint().y,
                z = Storage.data.cities[player.cityId].StartingPoint().z,
                gold = Storage.data.newPlayer.gold,
                br = player.battlepower,
                vitality = player.own.vitality,
                strength = player.own.strength,
                intelligence = player.own.intelligence,
                endurance = player.own.endurance,
                tribeId = player.tribeId,
                createdAt = DateTime.Now.ToOADate()
            });
            SaveInventory(player);
            SaveEquipment(player);
            SaveSkills(player);
            SaveQuests(player);
            Save7DaysSignUp(player);
            connection.Commit();
            // more modifications
            TribeSystem.EditTroops(player.tribeId);
        }
        public async Task CharacterSave(Player player, bool online, bool useTransaction = true) {
            if(useTransaction) connection.BeginTransaction();
            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() =>
            {
                connection.InsertOrReplace(new Characters
                {
                    id = player.id,
                    name = player.name,
                    accId = player.accId,
                    classType = player.classInfo.type,
                    classRank = player.classInfo.rank,
                    gender = player.gender,
                    avatar = player.avatar,
                    city = player.cityId,
                    x = player.transform.position.x,
                    y = player.transform.position.y,
                    z = player.transform.position.z,
                    online = online,
                    lastsaved = DateTime.Now.ToOADate(),
                    createdAt = player.own.createdAt,
                    privacy = player.privacy,
                    vipLevel = player.own.vip.level,
                    level = (byte)player.level,
                    experience = player.own.experience,
                    br = player.battlepower,
                    vitality = player.own.vitality,
                    strength = player.own.strength,
                    intelligence = player.own.intelligence,
                    endurance = player.own.endurance,
                    freepoints = player.own.freepoints,
                    inventorySize = player.own.inventorySize,
                    gold = player.own.gold,
                    diamonds = player.own.diamonds,
                    b_diamonds = player.own.b_diamonds,
                    tribeId = player.tribeId,
                    tribeRank = player.own.tribeRank,
                    tribeGoldContribution = player.own.tribeGoldContribution,
                    tribeDiamondContribution = player.own.tribeDiamondContribution,
                    todayHonor = player.own.TodayHonor,
                    totalHonor = player.own.TotalHonor,
                    killCount = player.own.killStrike,
                    MonsterPoints = player.own.MonsterPoints,
                    militaryRank = player.own.militaryRank,
                    popularity = player.own.popularity,
                    dailyQuests = player.own.dailyQuests,
                    tribeQuests = player.own.tribeQuests,
                    arena1v1WinsToday = player.own.arena1v1WinsToday,
                    arena1v1LossesToday = player.own.arena1v1LossesToday,
                    arena1v1Points = player.own.arena1v1Points,
                    dailySign = player.own.GetDailySignInInfo(),
                    AvailableFreeRespawn = player.AvailableFreeRespawn,
                    guildId = player.InGuild() ? player.guild.id : 0,
                    activeTitle = player.activeTitle,
                    teamId = player.teamId,
                    activePet = player.activePet != null ? player.activePet.id : (ushort)0,
                    activeMount = player.mount.id,
                    showWardrobe = player.showWardrop,
                    shareExpWithPet = player.own.shareExpWithPet,
                    suspiciousActivities = player.suspiciousActivities
                });
            }));
            tasks.Add(Task.Run(() => SaveInventory(player)));
            tasks.Add(Task.Run(() => SaveEquipment(player)));
            tasks.Add(Task.Run(() => SaveAccessories(player)));
            tasks.Add(Task.Run(() => SaveAchievements(player)));
            tasks.Add(Task.Run(() => SaveWardrobe(player)));
            tasks.Add(Task.Run(() => SaveSkills(player)));
            tasks.Add(Task.Run(() => SaveBuffs(player)));
            tasks.Add(Task.Run(() => SaveQuests(player)));
            tasks.Add(Task.Run(() => SaveMailBox(player)));
            tasks.Add(Task.Run(() => SaveVIP(player)));
            tasks.Add(Task.Run(() => SavePets(player)));
            tasks.Add(Task.Run(() => SaveMounts(player)));
            tasks.Add(Task.Run(() => SaveTitles(player)));
            tasks.Add(Task.Run(() => SaveCharacterGuildInfo(player)));
            tasks.Add(Task.Run(() => SaveFriends(player)));
            tasks.Add(Task.Run(() => SaveMarriage(player)));
            await Task.WhenAll(tasks);
            if (useTransaction) connection.Commit();
        }
        public async Task CharacterSaveMany(IEnumerable<Player> players, bool online = true) {
            connection.BeginTransaction(); // transaction for performance
            List<Task> tasks = new List<Task>();
            foreach(Player player in players)
            {
                tasks.Add(CharacterSave(player, online, false));
            }
            await Task.WhenAll(tasks);
            connection.Commit(); // end transaction
        }
        public void CharacterLogOff(Player player)
        {
            // if occupaied in event
            if(player.own.occupation != null)
            {
                if(player.own.occupation == PlayerOccupation.RegisteredArena1v1)
                {
                    ArenaSystem.UnRegister1vs1(player);
                }
                else if(player.own.occupation == PlayerOccupation.ReadyArena1v1)
                {
                    ArenaSystem.RefuseMatch1v1(player);
                }
                else if(player.own.occupation == PlayerOccupation.InMatchArena1v1)
                {
                    ArenaSystem.LeaveMatch1v1(player);
                }
            }
            // if in any way have lastLocation saved return to it 
            if(player.lastLocation != Vector3.zero)
            {
                player.Warp(player.lastLocation);
            }
            if(player.IsTrading())
            {
                TradeSystem.Cancel(player);
            }
            // normal save
            CharacterSave(player, false, true);
            // save preview data
            connection.InsertOrReplace(new PreviewData
            {
                id = player.id,
                health = player.healthMax,
                mana = player.manaMax,
                pAtk = player.p_atk,
                mAtk = player.m_atk,
                pDef = player.p_def,
                mDef = player.m_def,
                block = player.blockChance,
                untiBlock = player.untiBlockChance,
                critRate = player.critRate,
                critDmg = player.critDmg,
                antiCrit = player.antiCrit,
                untiStun = player.untiStunChance,
                speed = player.speed,
                lastSave = DateTime.Now
            });

            if(player.stillIn7Days)
                Save7DaysSignUp(player);
            // remove from online
            player.SetGuildOnline(DateTime.Now.ToOADate());
            TribeSystem.OnlineTribesMembers[player.tribeId].Remove(player.id);
            if(player.InTeam())
                TeamSystem.SetMemberOnline(player.teamId, player.id, false);
        }
        public PreviewPlayerData CharacterPreview(uint playerId)
        {
            PreviewPlayerData result = new PreviewPlayerData();
            Characters charData = connection.FindWithQuery<Characters>("SELECT name, level, gender, classType, classRank, vipLevel, tribeId, guildId, militaryRank, br, wardropBody, wardropWeapon, wardropWing, wardropSoul, showWardrobe FROM Characters WHERE id=?", playerId);
            if(charData != null) {
                result.status = true;
                PreviewData preview = connection.FindWithQuery<PreviewData>("SELECT * FROM PreviewData WHERE id=?", playerId);
                List<Equipments> equipments = connection.Query<Equipments>("SELECT * FROM Equipments WHERE id=?", playerId);
                if(charData.guildId > 0)
                {
                    Guilds guild = connection.FindWithQuery<Guilds>("SELECT name FROM Guilds WHERE id=?", charData.guildId);
                    result.guildName = guild != null ? guild.name : "";
                }
                // info
                result.id = playerId;
                result.name = charData.name;
                result.level = charData.level;
                result.gender = charData.gender;
                result.classInfo = new PlayerClassData(charData.classType, charData.classRank);
                result.tribeId = charData.tribeId;
                result.militaryRank = charData.militaryRank;
                result.vipLevel = charData.vipLevel;
                //attributes
                result.br = charData.br;
                result.health = preview.health;
                result.mana = preview.mana;
                result.pAtk = preview.pAtk;
                result.mAtk = preview.mAtk;
                result.pDef = preview.pDef;
                result.mDef = preview.mDef;
                result.block = preview.block;
                result.untiBlock = preview.untiBlock;
                result.critRate = preview.critRate;
                result.critDmg = preview.critDmg;
                result.antiCrit = preview.antiCrit;
                result.untiStun = preview.untiStun;
                result.speed = preview.speed;

                // set equipments
                result.equipments = new Item[Storage.data.player.equipmentCount];
                for(int i = 0; i < equipments.Count; i++)
                {
                    int slot = equipments[i].slot;
                    result.equipments[slot].id = equipments[i].id;
                    result.equipments[slot].quality = new ItemQualityData(equipments[i].quality, equipments[i].qualityMax, equipments[i].progress);
                    result.equipments[slot].plus = equipments[i].plus;
                    result.equipments[slot].socket1 = new Socket(equipments[i].socket1);
                    result.equipments[slot].socket2 = new Socket(equipments[i].socket2);
                    result.equipments[slot].socket3 = new Socket(equipments[i].socket3);
                    result.equipments[slot].socket4 = new Socket(equipments[i].socket4);
                    result.equipments[slot].bound = equipments[i].bound;
                }
            }
            else result.status = false;
            
            return result;
        }
        public void CharacterDelete(uint id, ulong accId)
        {
            Characters player = connection.FindWithQuery<Characters>("SELECT guildId, teamId, spouseId FROM Characters WHERE id=? AND accId=?", id, accId);
            if(player != null) {
                connection.BeginTransaction(); // transaction for performance
                connection.Execute("DELETE FROM Characters WHERE id=? AND accId=?", id, accId);
                connection.Execute("DELETE FROM Inventory WHERE owner=?", id);
                connection.Execute("DELETE FROM Equipments WHERE owner=?", id);
                connection.Execute("DELETE FROM Accessories WHERE owner=?", id);
                connection.Execute("DELETE FROM Clothing WHERE owner=?", id);
                connection.Execute("DELETE FROM Skills WHERE id=?", id);
                connection.Execute("DELETE FROM Buffs WHERE id=?", id);
                connection.Execute("DELETE FROM Quests WHERE owner=?", id);
                connection.Execute("DELETE FROM Pets WHERE owner=?", id);
                connection.Execute("DELETE FROM Mounts WHERE owner=?", id);
                connection.Execute("DELETE FROM VIPs WHERE id=?", id);
                connection.Execute("DELETE FROM First7Days WHERE id=?", id);
                connection.Execute("DELETE FROM Titles WHERE id=?", id);
                connection.Execute("DELETE FROM GuildMembers WHERE id=?", id);
                connection.Execute("DELETE FROM GuildSkills WHERE id=?", id);
                connection.Execute("DELETE FROM Mails WHERE recieverId=?", id);
                connection.Execute("DELETE FROM Archive WHERE owner=?", id);
                connection.Execute("DELETE FROM Achievements WHERE owner=?", id);
                connection.Execute("DELETE FROM Marriages WHERE hasband=? OR wife=?", id, id);
                List<Friends> friends = connection.Query<Friends>("SELECT friend FROM Friends WHERE id=?", id);
                if(friends.Count > 0) connection.Execute("DELETE FROM Friends WHERE id=? OR friend=?", id, id);
                connection.Commit(); // end transaction
                if(player.guildId > 0) GuildSystem.LeaveGuild(player.guildId, id, false, false);
                if(player.teamId > 0) TeamSystem.LeaveTeam(player.teamId, id);
                //if(player.spouseId > 0 && Player.onlinePlayers.ContainsKey(player.spouseId))
                if(friends.Count > 0)
                {
                    for(int i = 0; i < friends.Count; i++)
                    {
                        if(Player.onlinePlayers.ContainsKey(friends[i].friend))
                            Player.onlinePlayers[friends[i].friend].RemoveFriend(id);
                    }
                }
            }
        }
    #endregion
    #region Tribe
        public Dictionary<byte, Tribe> LoadAllTribes()
        {
            List<Tribes> tribes = connection.Query<Tribes>("SELECT * FROM Tribes");
            Dictionary<byte, Tribe> result = new Dictionary<byte, Tribe>();
            if(tribes.Count > 0)
            {
                foreach(Tribes row in tribes)
                {
                    Tribe tribe = new Tribe();
                    tribe.Wealth = row.wealth;
                    tribe.Rank = row.rank;
                    tribe.TotalBR = row.totalBR;
                    tribe.Troops = row.troops;
                    result[row.id] = tribe;
                    TribeSystem.OnlineTribesMembers[row.id] = new List<uint>();
                }
            }
            return result;
        }
        public void SaveAllTribes()
        {
            byte[] keys = TribeSystem.tribes.Keys.ToArray();
            for(int i = 0; i < keys.Length; i++)
            {
                connection.InsertOrReplace(new Tribes
                {
                    id = keys[i],
                    rank = TribeSystem.tribes[keys[i]].Rank,
                    wealth = TribeSystem.tribes[keys[i]].Wealth,
                    totalBR = TribeSystem.tribes[keys[i]].TotalBR,
                    troops = TribeSystem.tribes[keys[i]].Troops
                });
            }
        }
        public uint SumTribeTotalBR(byte id)
        {
            uint sum = connection.ExecuteScalar<uint>("SELECT SUM(br) FROM characters WHERE tribeId=?", id);
            return sum > 0 ? sum : 0;
        }
    #endregion
    #region Inventory
        async Task LoadInventory(Player player)
        {
            for(int i = 0; i < player.own.inventorySize; i++)
                player.own.inventory.Add(new ItemSlot());
            List<Inventory> table = connection.Query<Inventory>("SELECT * FROM Inventory WHERE owner=?", player.id);
            foreach(Inventory row in table)
            {
                if(row.slot < player.own.inventorySize)
                {
                    if(ScriptableItem.dict.TryGetValue(row.id, out ScriptableItem itemData))
                    {
                        Item item = new Item(itemData);
                        item.quality = new ItemQualityData(row.quality, row.qualityMax, row.progress);
                        item.plus = row.plus;
                        item.socket1 = new Socket(row.socket1);
                        item.socket2 = new Socket(row.socket2);
                        item.socket3 = new Socket(row.socket3);
                        item.socket4 = new Socket(row.socket4);
                        item.durability = row.durability;
                        item.bound = row.bound;
                        player.own.inventory[row.slot] = new ItemSlot(item, row.amount);
                    }
                    else Debug.LogWarning("LoadInventory: skipped item " + row.id + " for " + player.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
                }
                else Debug.LogWarning("LoadInventory: skipped slot " + row.slot + " for " + player.name + " because it's bigger than size " + player.own.inventorySize);
            }
        }
        void SaveInventory(Player player)
        {
            connection.Execute("DELETE FROM Inventory WHERE owner=?", player.id);
            if(player.own.inventory.Count == 0)
                return;
            for(int i = 0; i < player.own.inventory.Count; i++)
            {
                if(player.own.inventory[i].amount > 0)
                {
                    ItemSlot slot = player.own.inventory[i];
                    connection.Insert(new Inventory
                    {
                        owner = player.id,
                        id = slot.item.id,
                        slot = i,
                        plus = slot.item.plus,
                        quality = slot.item.quality.current,
                        qualityMax = slot.item.quality.max,
                        progress = slot.item.quality.progress,
                        socket1 = slot.item.socket1.id,
                        socket2 = slot.item.socket2.id,
                        socket3 = slot.item.socket3.id,
                        socket4 = slot.item.socket4.id,
                        durability = slot.item.durability,
                        bound = slot.item.bound,
                        amount = slot.amount
                    });
                }
            }
        }
    #endregion
    #region Equipments
        private async Task LoadEquipment(Player player)
        {
            player.equipment.Initiate(Storage.data.player.equipmentCount);
            List<Equipments> table = connection.Query<Equipments>("SELECT * FROM Equipments WHERE owner=?", player.id);
            foreach (Equipments row in table) {
                if (row.slot < Storage.data.player.equipmentCount)
                {
                    if(ScriptableItem.dict.TryGetValue(row.id, out ScriptableItem itemData))
                    {
                        Item item = new Item(itemData);
                        item.id = row.id;
                        item.plus = row.plus;
                        item.quality = new ItemQualityData(row.quality, row.qualityMax, row.progress);
                        item.socket1 = new Socket(row.socket1);
                        item.socket2 = new Socket(row.socket2);
                        item.socket3 = new Socket(row.socket3);
                        item.socket4 = new Socket(row.socket4);
                        item.durability = row.durability;
                        item.bound = row.bound;
                        player.equipment[row.slot] = new ItemSlot(item, row.amount);
                    }
                    else Debug.LogWarning("LoadEquipment: skipped item " + row.id + " for " + player.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
                }
                else Debug.LogWarning("LoadEquipment: skipped slot " + row.slot + " for " + player.name + " because it's bigger than size " + Storage.data.player.equipmentCount);
            }
        }
        void SaveEquipment(Player player)
        {
            connection.Execute("DELETE FROM Equipments WHERE owner=?", player.id);
            if(player.equipment.Count == 0)
                return;
            for(int i = 0; i < player.equipment.Count; i++)
            {
                ItemSlot slot = player.equipment[i];
                if (slot.amount > 0)
                {
                    connection.Insert(new Equipments
                    {
                        owner = player.id,
                        id = slot.item.id,
                        slot = i,
                        plus = slot.item.plus,
                        progress = slot.item.quality.progress,
                        quality = slot.item.quality.current,
                        qualityMax = slot.item.quality.max,
                        socket1 = slot.item.socket1.id,
                        socket2 = slot.item.socket2.id,
                        socket3 = slot.item.socket3.id,
                        socket4 = slot.item.socket4.id,
                        durability = slot.item.durability,
                        bound = slot.item.bound,
                        amount = slot.amount
                    });
                }
            }
        }
    #endregion
    #region Accessories
        async Task LoadAccessories(Player player)
        {
            for(int i = 0; i < Storage.data.player.accessoriesCount; i++)
            {
                player.own.accessories.Add(new ItemSlot());
            }
            List<Accessories> table = connection.Query<Accessories>("SELECT * FROM Accessories WHERE owner=?", player.id);
            for(int i = 0; i < table.Count; i++)
            {
                if (table[i].slot < Storage.data.player.accessoriesCount)
                {
                    if (ScriptableItem.dict.TryGetValue(table[i].id, out ScriptableItem itemData))
                    {
                        Item item = new Item(itemData);
                        item.plus = table[i].plus;
                        item.quality = new ItemQualityData(table[i].quality, table[i].qualityMax, table[i].progress);
                        item.socket1 = new Socket(table[i].socket1);
                        item.socket2 = new Socket(table[i].socket2);
                        item.socket3 = new Socket(table[i].socket3);
                        item.socket4 = new Socket(table[i].socket4);
                        item.durability = table[i].durability;
                        item.bound = table[i].bound;
                        player.own.accessories[table[i].slot] = new ItemSlot(item, table[i].amount);
                    }
                    else Debug.LogWarning("LoadAccessories: skipped item " + table[i].id + " for " + player.name + " because it doesn't exist anymore. If it wasn't removed intentionally then make sure it's in the Resources folder.");
                }
                else Debug.LogWarning("LoadAccessories: skipped slot " + table[i].slot + " for " + player.name + " because it's bigger than size " + Storage.data.player.accessoriesCount);
            }
        }
        void SaveAccessories(Player player)
        {
            connection.Execute("DELETE FROM Accessories WHERE owner=?", player.id);
            if(player.own.accessories.Count == 0)
                return;
            for(int i = 0; i < player.own.accessories.Count; i++)
            {
                ItemSlot slot = player.own.accessories[i];
                if (slot.amount > 0)
                {
                    connection.InsertOrReplace(new Accessories
                    {
                        owner = player.id,
                        id = slot.item.id,
                        slot = i,
                        plus = slot.item.plus,
                        quality = slot.item.quality.current,
                        qualityMax = slot.item.quality.max,
                        progress = slot.item.quality.progress,
                        socket1 = slot.item.socket1.id,
                        socket2 = slot.item.socket2.id,
                        socket3 = slot.item.socket3.id,
                        socket4 = slot.item.socket4.id,
                        durability = slot.item.durability,
                        bound = slot.item.bound,
                        amount = slot.amount
                    });
                }
            }
        }
    #endregion
    #region Skills
        async Task LoadSkills(Player player)
        {
            //for(int i = 0; i < player.skillTemplates.Length; i++) 
            //    player.skills.Add(new Skill(player.skillTemplates[i]));
            
            List<Skills> table = connection.Query<Skills>("SELECT * FROM Skills WHERE id=?", player.id);
            if(table.Count < 1) 
                return;
            
            for(int i = 0; i < table.Count; i++)
            {
                if(table[i].skill == 0)
                {
                    Debug.Log(player.name + " has skill id=" + table[i].skill);
                    continue;
                }
                int index = player.skills.FindIndex(s => s.id == table[i].skill);
                if(index == -1) 
                    continue;
                if(ScriptableSkill.dict.TryGetValue(table[i].skill, out ScriptableSkill skillData))
                {
                    Skill skill = new Skill(skillData);
                    skill.level = (byte)Mathf.Clamp(table[i].level, 1, skill.maxLevel);
                    skill.experience = table[i].experience;
                    player.skills.Add(skill);
                }
            }
        }
        void SaveSkills(Player player)
        {
            if(player.skills.Count == 0)
                return;

            connection.Execute("DELETE FROM Skills WHERE id=?", player.id);

            for(int i = 0; i < player.skills.Count; i++)
            {
                if(player.skills[i].level < 1)
                    continue;
                connection.Insert(new Skills
                {
                    id = player.id,
                    skill = player.skills[i].id,
                    level = player.skills[i].level,
                    experience = player.skills[i].experience
                });
            }
        }
    #endregion
    #region Buffs
        async Task LoadBuffs(Player player)
        {
            List<Buffs> table = connection.Query<Buffs>("SELECT * FROM Buffs WHERE id=?", player.id);
            if(table.Count < 1)
                return;
            
            for(int i = 0; i < table.Count; i++)
            {
                if (ScriptableSkill.dict.TryGetValue(table[i].buff, out ScriptableSkill skillData))
                {
                    Buff buff = new Buff((BuffSkill)skillData, (byte)Mathf.Clamp(table[i].level, 1, skillData.maxLevel));
                    buff.buffTimeEnd = table[i].buffTimeEnd + NetworkTime.time;
                    player.buffs.Add(buff);
                }
            }
        }
        void SaveBuffs(Player player)
        {
            connection.Execute("DELETE FROM Buffs WHERE id=?", player.id);
            if(player.buffs.Count == 0)
                return;
            for(int i = 0; i < player.buffs.Count; i++)
            {
                connection.InsertOrReplace(new Buffs
                {
                    id = player.id,
                    buff = Convert.ToUInt16(player.buffs[i].name),
                    level = player.buffs[i].level,
                    buffTimeEnd = player.buffs[i].BuffTimeRemaining()
                });
            }
        }
    #endregion
    #region Quests
        async Task LoadQuests(Player player)
        {
            List<Quests> table = connection.Query<Quests>("SELECT * FROM Quests WHERE owner=?", player.id);
            if(table.Count > 0)
            {
                for(int i = 0; i < table.Count; i++)
                {
                    if(ScriptableQuest.dict.ContainsKey(table[i].id))
                    {
                        player.own.quests.Add(new Quest
                        {
                            id = table[i].id,
                            progress = table[i].progress,
                            completed = table[i].completed,
                            type = table[i].type,
                            quality = table[i].quality
                        });
                    } 
                    else player.Log($"[[Database.LoadQuests] invalid questId: {table[i].id}");
                }
            }
        }
        void SaveQuests(Player player)
        {
            connection.Execute("DELETE FROM Quests WHERE owner=?", player.id);
            for(int i = 0; i < player.own.quests.Count; i++)
            {
                connection.Insert(new Quests
                {
                    owner = player.id,
                    id = player.own.quests[i].id,
                    progress = player.own.quests[i].progress,
                    completed = player.own.quests[i].completed,
                    type = player.own.quests[i].type,
                    quality = player.own.quests[i].quality
                });
            }
        }
    #endregion
    #region Pets
        async Task LoadPets(Player player, ushort activeId)
        {
            List<Pets> table = connection.Query<Pets>("SELECT * FROM Pets WHERE owner=?", player.id);
            if(table.Count < 1)
                return;
            for(int i = 0; i < table.Count; i++)
            {
                player.own.pets.Add(new PetInfo
                {
                    id = table[i].id,
                    level = table[i].level,
                    experience = table[i].experience,
                    stars = table[i].stars,
                    tier = table[i].tier,
                    potential = table[i].potential,
                    vitality = table[i].vitality,
                    intelligence = table[i].intelligence,
                    endurance = table[i].endurance,
                    strength = table[i].strength,
                    status = activeId > 0 && table[i].id == activeId ? SummonableStatus.Deployed : SummonableStatus.Saved
                });
            }
        }
        public void SavePets(Player player)
        {
            if(player.own.pets.Count == 0)
                return;
            foreach(PetInfo pet in player.own.pets)
            {
                connection.Insert(new Pets
                {
                    id = pet.id,
                    owner = player.id,
                    level = pet.level,
                    experience = pet.experience,
                    stars = pet.stars,
                    tier = pet.tier,
                    potential = pet.potential,
                    vitality = pet.vitality,
                    strength = pet.strength,
                    intelligence = pet.intelligence,
                    endurance = pet.endurance,
                    br = pet.battlepower
                });
            }
        }
    #endregion
    #region Mounts
        async Task LoadMounts(Player player, ushort activeId)
        {
            List<Mounts> table = connection.Query<Mounts>("SELECT * FROM Mounts WHERE owner=?", player.id);
            if(table.Count < 1)
                return;
            
            int activeIndex = -1;
            for(int i = 0; i < table.Count; i++)
            {
                player.own.mounts.Add(new Mount
                {
                    id = table[i].id,
                    level = table[i].level,
                    experience = table[i].experience,
                    stars = table[i].stars,
                    tier = table[i].tier,
                    vitality = table[i].vitality,
                    intelligence = table[i].intelligence,
                    endurance = table[i].endurance,
                    strength = table[i].strength,
                    status = activeId > 0 && table[i].id == activeId ? SummonableStatus.Deployed : SummonableStatus.Saved
                });

                if(activeId > 0 && activeIndex == -1 && table[i].id == activeId)
                    activeIndex = i;
            }
            player.mount = new ActiveMount(activeIndex != -1 ? table[activeIndex].id : (ushort)0);
        }
        public void SaveMounts(Player player)
        {
            if(player.own.mounts.Count == 0)
                return;
            
            foreach(Mount mount in player.own.mounts)
            {
                connection.InsertOrReplace(new Mounts
                {
                    id = mount.id,
                    owner = player.id,
                    level = mount.level,
                    experience = mount.experience,
                    stars = mount.stars,
                    tier = mount.tier,
                    vitality = mount.vitality,
                    strength = mount.strength,
                    intelligence = mount.intelligence,
                    endurance = mount.endurance,
                    br = mount.battlepower
                });
            }
        }
    #endregion
    #region VIP
        async Task LoadVIP(Player player, byte level)
        {
            VIPs row = connection.FindWithQuery<VIPs>("SELECT * FROM VIPs WHERE id=?", player.id);
            if(row != null)
            {
                player.own.vip = new VIP
                {
                    level = level,
                    points = row.points,
                    quests = row.quests,
                    firstRewards = row.rewards == "" ? new int[]{} : Array.ConvertAll<string, int>(row.rewards.Split(','), int.Parse),
                    totalRecharge = row.totalRecharge,
                    todayRecharge = row.todayRecharge
                };
            }
        }
        public void SaveVIP(Player player)
        {
            connection.InsertOrReplace(new VIPs
            {
                id = player.id,
                points = player.own.vip.points,
                quests = player.own.vip.quests,
                rewards = player.own.vip.FirstRewardsString(),
                totalRecharge = player.own.vip.totalRecharge,
                todayRecharge = player.own.vip.todayRecharge
            });
        }
    #endregion
    #region Wardrobe
        async Task LoadWardrobe(Player player)
        {
            //wardrobe
            List<Wardrobes> table = connection.Query<Wardrobes>("SELECT id FROM Wardrobes WHERE owner=?", player.id);
            if(table.Count > 0)
            {
                for(int i = 0; i < table.Count; i++)
                {
                    if(ScriptableWardrobe.dict.ContainsKey(table[i].id))
                     {
                        player.own.wardrobe.Add(table[i].id);
                    }
                    else player.Log($"[Database.LoadWardrobe] invalid wardrobeId: {table[i].id}");
                }
            }
            //cloths
            for(int i = 0; i < 4; i++)
                player.wardrobe.Add(new WardrobeItem());

            List<Clothing> cTable = connection.Query<Clothing>("SELECT id, plus FROM Clothing WHERE owner=?", player.id);

            if(cTable.Count > 0)
            {
                for(int i = 0; i < cTable.Count; i++)
                {
                    player.wardrobe[(int)ScriptableWardrobe.dict[cTable[i].id].category] 
                    = new WardrobeItem(cTable[i].id, cTable[i].plus);
                }
            }
        }
        void SaveWardrobe(Player player)
        {
            if(player.own.wardrobe.Count < 1)
                return;
            connection.Execute("DELETE FROM Wardrobes WHERE owner=?", player.id);
            for(int i = 0; i < player.own.wardrobe.Count; i++)
            {
                connection.Insert(new Wardrobes
                {
                    owner = player.id,
                    id = player.own.wardrobe[i],
                });
            }
            connection.Execute("DELETE FROM Clothing WHERE owner=?", player.id);
            for(int i = 0; i < player.wardrobe.Count; i++)
            {
                if(player.wardrobe[i].isUsed) {
                    connection.Insert(new Clothing
                    {
                        owner = player.id,
                        id = player.wardrobe[i].id,
                        plus = player.wardrobe[i].plus
                    });
                }
            }
        }
    #endregion
    #region Hot Events
        public void LoadHotEvents()
        {
            HotEventsSystem.ResetChecks();
            List<HotEvents> table = new List<HotEvents>();
            table = connection.Query<HotEvents>("SELECT * FROM HotEvents WHERE endsAt > ? AND finished=0 ORDER BY id", DateTime.Now.ToOADate());
            if(table.Count > 0)
            {
                foreach(HotEvents row in table)
                {
                    string[] names = row.name.Split(',');
                    string[] descriptions = row.description.Split(',');
                    HotEvent hotEvent = new HotEvent();
                    hotEvent.id = row.id.Value;
                    hotEvent.name = new string[] {names[0], names[1]};
                    hotEvent.type = row.type;
                    hotEvent.startsAt = row.startsAt;
                    hotEvent.endsAt = row.endsAt;
                    hotEvent.renewable = row.renewable;
                    hotEvent.objectives = HotEventsSystem.DecodeObjectives(row.objectives, row.rewards); // objs + rewards
                    hotEvent.description = new string[] {descriptions[0], descriptions[1]};
                    HotEventsSystem.events.Add(hotEvent);
                    HotEventsSystem.checks[hotEvent.type] = true;
                }
            }
        }
        public void CreateHotEvent(HotEvent data)
        {
            string[] objs = HotEventsSystem.EncodeObjectives(data.objectives);
            connection.Insert(new HotEvents
            {
                id = null,
                name = String.Join(",", data.name),
                description = String.Join(",", data.description),
                type = data.type,
                startsAt = data.startsAt,
                endsAt = data.endsAt,
                renewable = data.renewable,
                objectives = objs[0],
                rewards = objs[1]
            });
        }
        public void LoadHotEventsProgress(Player player)
        {
            /*if(HotEventsSystem.events.Count > 0) {
                List<character_hotevents> table = connection.Query<character_hotevents>("SELECT * FROM character_hotevents WHERE id=? AND eventId IN " + HotEventsSystem.eventsIds + " ORDER BY eventId", player.id);
                for(int i = 0; i < HotEventsSystem.events.Count; i++) {
                    int progressIndex = table.FindIndex(e => table[i].eventId == HotEventsSystem.events[i].id);
                    HotEventProgress progress = new HotEventProgress(HotEventsSystem.events[i].id);
                    if(progressIndex > -1) {
                        progress.progress = table[i].progress;
                        progress.completeTimes = Array.ConvertAll<string, int>((table[i].completeTimes).Split(','), int.Parse);
                    } else {
                        Array.Resize(ref progress.completeTimes, HotEventsSystem.events[i].objectives.Count);
                    }
                    player.own.HotEventsProgress.Add(progress);
                }
            }*/
        }
        void SaveHotEventsProgress(Player player)
        {
            /*connection.Execute("DELETE FROM character_hotevents WHERE id=?", player.id);
            for(int i = 0; i < player.own.HotEventsProgress.Count; ++i) {
                connection.InsertOrReplace(new character_hotevents {
                    id = player.id,
                    eventId = player.own.HotEventsProgress[i].id,
                    progress = player.own.HotEventsProgress[i].progress,
                    completeTimes = Utils.ArrayToString(player.own.HotEventsProgress[i].completeTimes)
                });
            }*/
        }
    #endregion
    #region First 7Days
        public void Load7Days(Player player)
        {
            First7Days info = connection.FindWithQuery<First7Days>("SELECT * FROM First7Days WHERE id=?",player.id);
            if(info != null)
            {
                player.signUp7Days = Utils.StringToIntArray(info.signUp);
                player.recharge7Days = Utils.StringToIntArray(info.recharged);
                player.recharge7DaysRewards = Utils.StringToIntArray(info.rechargeRewards);
            }
        }
        void Save7DaysSignUp(Player player)
        {
            connection.InsertOrReplace(new First7Days
            {
                id = player.id,
                signUp = Utils.ArrayToString(player.signUp7Days),
                recharged = Utils.ArrayToString(player.recharge7Days),
                rechargeRewards = Utils.ArrayToString(player.recharge7DaysRewards)
            });
        }
    #endregion
    #region Titles
        async Task LoadTitles(Player player) {
            List<Titles> table = connection.Query<Titles>("SELECT title FROM Titles WHERE id=?", player.id);
            if(table.Count > 0)
            {
                for(int i = 0; i < table.Count; i++)
                {
                    player.own.titles.Add(table[i].title);
                }
            }
        }
        void SaveTitles(Player player)
        {
            if(player.own.titles.Count == 0)
                return;
            for(int i = 0; i < player.own.titles.Count; i++)
            {
                connection.Insert(new Titles
                {
                    id = player.id,
                    title = player.own.titles[i]
                });
            }
        }
    #endregion
    #region Guild
        async Task LoadGuild(Player player, uint guildId)
        {
            if(guildId > 0 && GuildSystem.guilds.TryGetValue(guildId, out Guild guild))
            {
                player.guild = new GuildPublicInfo(guild.id, guild.Name);
                player.own.guild = guild;
                GuildMember data = GuildSystem.GetMemberById(guildId, player.id);
                player.own.guildContribution = data.contribution;
                player.own.guildRank = data.rank;
            }
            GuildSkills skills = connection.FindWithQuery<GuildSkills>("SELECT * FROM GuildSkills WHERE id=?", player.id);
            player.own.guildSkills = new SyncListByte() {0, 0, 0, 0, 0, 0};
            if(skills != null)
            {
                player.own.guildSkills[0] = skills.vitalityLvl;
                player.own.guildSkills[1] = skills.strengthLvl;
                player.own.guildSkills[2] = skills.intelligenceLvl;
                player.own.guildSkills[3] = skills.enduranceLvl;
                player.own.guildSkills[4] = skills.critLvl;
                player.own.guildSkills[5] = skills.blockLvl;
            }
        } 
        public void SaveCharacterGuildInfo(Player player)
        {
            GuildMember myInfo = GuildSystem.GetMemberById(player.guild.id, player.id);
            if(player.InGuild())
            {
                connection.InsertOrReplace(new GuildMembers
                {
                    id = player.id,
                    guildId = player.guild.id,
                    rank = myInfo.rank,
                    contribution = myInfo.contribution
                });
            }
            else
            {
                connection.Execute("DELETE FROM GuildMembers WHERE id=?", player.id);
            }
            connection.InsertOrReplace(new GuildSkills
            {
                id = player.id,
                vitalityLvl = player.own.guildSkills[0],
                strengthLvl = player.own.guildSkills[1],
                intelligenceLvl = player.own.guildSkills[2],
                enduranceLvl = player.own.guildSkills[3],
                critLvl = player.own.guildSkills[4],
                blockLvl = player.own.guildSkills[5]
            });
        }
        public void LoadAllGuildsInfo()
        {
            foreach (byte tribe in TribeSystem.tribes.Keys)
            {
                GuildSystem.guildsJoinInfo[tribe] = new GuildJoinInfo[]{};
            }
            List<Guilds> table = connection.Query<Guilds>("SELECT * FROM Guilds");
            if(table.Count < 1)
                return;
            for (int i = 0; i < table.Count; i++)
            {
                Guild guild = new Guild
                {
                    id = table[i].id,
                    Name = table[i].name,
                    tribeId = table[i].tribeId,
                    level = table[i].level,
                    exp = table[i].experience,
                    AutoAccept = table[i].autoAccept,
                    notice = table[i].notice,
                    JoinLevel = table[i].joinLevel,
                    assets = new GuildAssets
                    {
                        wealth = table[i].wealth,
                        wood = table[i].wood,
                        stone = table[i].stone,
                        iron = table[i].iron,
                        food = table[i].food
                    },
                    buildings = new GuildBuildings
                    {
                        hall = table[i].hall,
                        academy = table[i].academy,
                        storage = table[i].storage,
                        shop = table[i].shop
                    }
                };
                List<GuildMembers> membersTable = connection.Query<GuildMembers>("SELECT * FROM GuildMembers WHERE guildId=? ORDER BY rank DESC", guild.id);
                GuildSystem.members[guild.id] = new GuildMember[membersTable.Count];
                int elitesCount = 0;
                bool hasVice = false;
                if(membersTable.Count > 0)
                {
                    for (int m = 0; m < membersTable.Count; m++)
                    {
                        Characters character = connection.FindWithQuery<Characters>("SELECT name, level, classType, classRank, br, lastsaved FROM Characters WHERE id=?", membersTable[m].id);
                        if(character != null)
                        {
                            GuildSystem.members[guild.id][m] = new GuildMember
                            {
                                id = membersTable[m].id,
                                Name = character.name,
                                level = character.level,
                                classInfo = new PlayerClassData(character.classType, character.classRank),
                                br = character.br,
                                rank = membersTable[m].rank,
                                contribution = membersTable[m].contribution,
                                online = character.lastsaved
                            };
                            if(membersTable[m].rank == GuildRank.Master)
                                guild.masterName = character.name;
                            else if(membersTable[m].rank == GuildRank.Vice)
                                hasVice = true;
                            else if(membersTable[m].rank == GuildRank.Elite)
                                elitesCount++;
                        }
                    }
                }
                guild.membersCount = (byte)membersTable.Count;
                GuildSystem.guilds[guild.id] = guild;
                GuildSystem.elitesCount[guild.id] = elitesCount;
                GuildSystem.hasVice[guild.id] = hasVice;
                GuildSystem.AddJoinInfo(guild);
                GuildSystem.joinRequests[guild.id] = new GuildJoinRequest[]{}; // add to requests
            }
        }
        public void SaveGuilds()
        {
            if(GuildSystem.guilds.Count < 1) //safe guard for new servers with no guilds
                return;
            foreach(Guild guild in GuildSystem.guilds.Values)
            {
                connection.InsertOrReplace(new Guilds
                {
                    id = guild.id,
                    name = guild.Name,
                    tribeId = guild.tribeId,
                    level = guild.level,
                    br = guild.br,
                    experience = guild.exp,
                    autoAccept = guild.AutoAccept,
                    joinLevel = guild.JoinLevel,
                    notice = guild.notice,
                    wealth = guild.assets.wealth,
                    wood = guild.assets.wood,
                    stone = guild.assets.stone,
                    iron = guild.assets.iron,
                    food = guild.assets.food,
                    hall = guild.buildings.hall,
                    academy = guild.buildings.academy,
                    storage = guild.buildings.storage,
                    shop = guild.buildings.shop
                });
                for(int m = 0; m < GuildSystem.members[guild.id].Length; m++)
                {
                    connection.InsertOrReplace(new GuildMembers
                    {
                        id = GuildSystem.members[guild.id][m].id,
                        guildId = guild.id,
                        contribution = GuildSystem.members[guild.id][m].contribution,
                        rank = GuildSystem.members[guild.id][m].rank
                    });
                }
            }

        }
        public bool CheckIfPlayerNotInGuild(uint id)
        {
            return connection.FindWithQuery<Characters>("SELECT * FROM Characters WHERE guildId=0 AND id=?", id) != null;
        }
        public GuildMember? GetJoinInfoOfflinePlayer(uint id) {
            Characters info = connection.FindWithQuery<Characters>("SELECT name, level, br, classType, classRank, lastsaved FROM Characters WHERE id=?", id);
            if(info != null)
            {
                return new GuildMember
                {
                    id = id,
                    Name = info.name,
                    level = info.level,
                    classInfo = new PlayerClassData(info.classType, info.classRank),
                    br = info.br,
                    rank = GuildRank.Member,
                    online = info.lastsaved
                };
            }
            return null;
        }
        public void UpdateCharacterGuildInfo(uint guildId, uint member, bool delete = false)
        {
            if(delete)
            {
                connection.Execute("DELETE FROM GuildMembers WHERE id=?", member);
                return;
            }
            if(GuildSystem.members.ContainsKey(guildId))
            {
                for (int i = 0; i < GuildSystem.members[guildId].Length; i++)
                {
                    if(GuildSystem.members[guildId][i].id == member)
                    {
                        connection.InsertOrReplace(new GuildMembers
                        {
                            id = member,
                            guildId = guildId,
                            contribution = GuildSystem.members[guildId][i].contribution,
                            rank = GuildSystem.members[guildId][i].rank
                        });
                        return;
                    }
                }
            }
        }
        public void AddOrRemovePlayerFromGuild(uint id, uint guildId)
        {
            connection.Execute("UPDATE Characters SET guildId=? WHERE id=?", guildId, id);
            if(guildId > 0)
            {
                connection.InsertOrReplace(new GuildMembers
                {
                    id = id,
                    guildId = guildId,
                    contribution = 0,
                    rank = GuildRank.Member
                });
            }
        }
        public void RemoveGuild(uint id)
        {
            connection.BeginTransaction(); // transaction for performance
            connection.Execute("DELETE FROM Guilds WHERE id=?", id);
            connection.Execute("DELETE FROM GuildMembers WHERE guildId=?", id);
            connection.Commit(); // end transaction
        }
        public void SetNextGuildId()
        {
            Guilds maxInDB = connection.FindWithQuery<Guilds>("SELECT id FROM Guilds WHERE id=(SELECT MAX(id) FROM Guilds)");
            if(maxInDB != null)
            {
                GuildSystem.nextGuildId = maxInDB.id + 1;
            }
        }
    #endregion
    #region Friends
        public void LoadFriends(Player player)
        {
            List<Friends> table = connection.Query<Friends>("SELECT friend FROM Friends WHERE id=?", player.id);
            if(table.Count > 0)
            {
                for(int i = 0; i < table.Count; i++)
                {
                    if(Player.onlinePlayers.TryGetValue(table[i].friend, out Player friendData))
                    {
                        player.own.friends.Add(new Friend
                        {
                            id = table[i].friend,
                            name = friendData.name,
                            classInfo = friendData.classInfo,
                            avatar = friendData.avatar,
                            level = (byte)friendData.level,
                            tribe = friendData.tribeId,
                            br = friendData.battlepower,
                            lastOnline = 0
                        });
                        friendData.SetFriendOnline(player.id);
                        Components.ChatComponent.SendSystemMsgTo(table[i].friend, player.name + (friendData.currentLang == Languages.En ? " is now Online" : " الان متصل"));
                    } 
                    else
                    {
                        player.own.friends.Add(LoadOfflineFriend(table[i].friend));
                    }
                }
            }
        }
        public Friend LoadOfflineFriend(uint id)
        {
            Characters friendInfo = connection.FindWithQuery<Characters>("SELECT name, classType, classRank, avatar, level, br, tribeId, lastsaved FROM Characters WHERE id=?", id);
            Friend friend = new Friend();
            if(friendInfo != null)
            {
                friend.id = id;
                friend.name = friendInfo.name;
                friend.classInfo = new PlayerClassData(friendInfo.classType, friendInfo.classRank);
                friend.avatar = friendInfo.avatar;
                friend.level = friendInfo.level;
                friend.tribe = friendInfo.tribeId;
                friend.br = friendInfo.br;
                friend.lastOnline = friendInfo.lastsaved;
            }
            return friend;
        }
        public void RemoveFriend(uint id, uint friend)
        {
            connection.Execute("DELETE FROM Friends WHERE id=? AND friend=?", id, friend);
            connection.Execute("DELETE FROM Friends WHERE id=? AND friend=?", friend, id);
        }
        void SaveFriends(Player player)
        {
            if(player.own.friends.Count > 0)
                return;
            for(int i = 0; i < player.own.friends.Count; i++)
            {
                connection.Insert(new Friends
                {
                    id = player.id,
                    friend = player.own.friends[i].id
                });
                connection.Insert(new Friends
                {
                    id = player.own.friends[i].id,
                    friend = player.id
                });
            }
        }
    #endregion
    #region Mail
        public void LoadMailBox(Player player)
        {
            List<Mails> table = connection.Query<Mails>("SELECT * FROM Mails WHERE recieverId=? ORDER BY send_time DESC", player.id);
            if(table.Count < 1)
                return;
            for(int i = 0; i < table.Count; i++)
            {
                player.own.mailBox.Add(new Mail
                {
                    id = table[i].id.Value,
                    category = table[i].category,
                    //sender = table[i].sender,
                    //senderName = table[i].category != MailCategory.Private ? "" : CharacterNameByIndex(table[i].sender),
                    subject = table[i].subject,
                    content = table[i].content,
                    opened = table[i].opened,
                    sentAt = table[i].send_time,
                    items = GetMailItems(table[i].id.Value),
                    currency = new Currencies
                    {
                        gold = table[i].gold,
                        diamonds = table[i].diamonds,
                        b_diamonds = table[i].b_diamonds,
                        recieved = table[i].currencyRecieved
                    }
                });
            }
        }
        MailItemSlot[] GetMailItems(uint mailId)
        {
            List<MailItems> table = connection.Query<MailItems>("SELECT * FROM MailItems WHERE mailId=?", mailId);
            MailItemSlot[] items = new MailItemSlot[table.Count];
            if(table.Count > 0)
            {
                for(int i = 0; i < table.Count; i++)
                {
                    if(ScriptableItem.dict.TryGetValue(table[i].id, out ScriptableItem itemData))
                    {
                        items[i] = new MailItemSlot(new Item
                        {
                            id = table[i].id,
                            plus = table[i].plus,
                            quality = new ItemQualityData(table[i].quality, table[i].qualityMax, table[i].progress),
                            socket1 = new Socket(table[i].socket1),
                            socket2 = new Socket(table[i].socket2),
                            socket3 = new Socket(table[i].socket3),
                            socket4 = new Socket(table[i].socket4),
                            durability = table[i].durability,
                            bound = table[i].bound
                        },
                        table[i].amount, table[i].recieved);
                    }
                }
            }
            return items;
        }
        public void SaveMail(Mail mail, uint reciever)
        {
            uint? id = null;
            if(mail.id > 0)
                id = mail.id;
            connection.Insert(new Mails
            {
                id = id,
                recieverId = reciever,
                category = mail.category,
                //sender = mail.sender,
                subject = mail.subject,
                content = mail.content,
                opened = mail.opened,
                send_time = mail.sentAt,
                gold = mail.currency.gold,
                diamonds = mail.currency.diamonds,
                b_diamonds = mail.currency.b_diamonds,
                currencyRecieved = mail.currency.recieved
            });
            if(mail.items.Length > 0)
            {
                uint mailId = mail.id == 0 ? connection.ExecuteScalar<uint>("SELECT last_insert_rowid()") : mail.id;
                for(int i = 0; i < mail.items.Length; i++)
                {
                    connection.InsertOrReplace(new MailItems
                    {
                        mailId = mailId,
                        id = mail.items[i].item.id,
                        plus = mail.items[i].item.plus,
                        quality = mail.items[i].item.quality.current,
                        qualityMax = mail.items[i].item.quality.max,
                        progress = mail.items[i].item.quality.progress,
                        socket1 = mail.items[i].item.socket1.id,
                        socket2 = mail.items[i].item.socket2.id,
                        socket3 = mail.items[i].item.socket3.id,
                        socket4 = mail.items[i].item.socket4.id,
                        amount = mail.items[i].amount,
                        durability = mail.items[i].item.durability,
                        bound = mail.items[i].item.bound,
                        recieved = mail.items[i].recieved
                    });
                }
            }
        }
        void SaveMailBox(Player player)
        {
            if(player.own.mailBox.Count > 0)
            {
                double time = DateTime.Now.AddMonths(-1).ToOADate();
                for(int i = 0; i < player.own.mailBox.Count; i++) {
                    if(player.own.mailBox[i].sentAt <= time)
                    {
                        if(player.own.mailBox[i].id > 0)
                        {
                            DeleteMail(player.own.mailBox[i].id);
                        }
                        player.own.mailBox.RemoveAt(i);
                    }
                    else
                    {
                        SaveMail(player.own.mailBox[i], player.id);
                    }
                }
            }
        }
        public void ClearOutDatedMails()
        {
            List<Mails> table = connection.Query<Mails>("SELECT id FROM Mails WHERE send_time <= ?", DateTime.Now.AddMonths(-1).ToOADate());
            if(table.Count > 0)
            {
                for(int i = 0; i < table.Count; i++)
                {
                    DeleteMail(table[i].id.Value);
                }
            }
        }
        public void BroadcastMailToAllOfflinePlayers(Mail mail)
        {
            List<Characters> table = connection.Query<Characters>("SELECT id FROM Characters WHERE online=0");
            if(table.Count > 0)
            {
                for(int i = 0; i < table.Count; i++)
                {
                    SaveMail(mail, table[i].id.Value);
                }
            }
        }
        public void DeleteMail(uint id)
        {
            connection.Execute("DELETE FROM Mails WHERE id=?", id);
            connection.Execute("DELETE FROM MailItems WHERE mailId=?", id);
        }
        public void DeleteAllMails(SyncListMail mailBox)
        {
            if(mailBox.Count > 0)
            {
                for(int i = 0; i < mailBox.Count; i++)
                {
                    if(mailBox[i].id > 0)
                    {
                        DeleteMail(mailBox[i].id);
                    }
                }
            }
        }
    #endregion
    #region Ranking
        #region Players
        public RankingBasicData[] LoadRankingPlayerBR()
        {
            RankingBasicData[] result = new RankingBasicData[]{};
            List<Characters> table = connection.Query<Characters>("SELECT id, name, br FROM Characters ORDER BY br DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new RankingBasicData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new RankingBasicData
                    {
                        id = table[i].id.Value,
                        name = table[i].name,
                        value = table[i].br
                    };
                }
            }
            return result;
        }
        public RankingBasicData[] LoadRankingPlayerLvl()
        {
            RankingBasicData[] result = new RankingBasicData[]{};
            List<Characters> table = connection.Query<Characters>("SELECT id, name, level FROM Characters ORDER BY level DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new RankingBasicData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new RankingBasicData
                    {
                        id = table[i].id.Value,
                        name = table[i].name,
                        value = table[i].level
                    };
                }
            }
            return result;
        }
        public RankingBasicData[] LoadRankingPlayerHnr()
        {
            RankingBasicData[] result = new RankingBasicData[]{};
            List<Characters> table = connection.Query<Characters>("SELECT id, name, totalHonor FROM Characters ORDER BY totalHonor DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new RankingBasicData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new RankingBasicData
                    {
                        id = table[i].id.Value,
                        name = table[i].name,
                        value = table[i].totalHonor
                    };
                }
            }
            return result;
        }
        #endregion
        #region Guilds
        public RankingBasicData[] LoadRankingGuildLvl()
        {
            RankingBasicData[] result = new RankingBasicData[]{};
            List<Guilds> table = connection.Query<Guilds>("SELECT id, name, level FROM Guilds ORDER BY level DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new RankingBasicData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new RankingBasicData
                    {
                        id = table[i].id,
                        name = table[i].name,
                        value = table[i].level
                    };
                }
            }
            return result;
        }
        public RankingBasicData[] LoadRankingGuildBR()
        {
            RankingBasicData[] result = new RankingBasicData[]{};
            List<Guilds> table = connection.Query<Guilds>("SELECT id, name, br FROM Guilds ORDER BY br DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new RankingBasicData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new RankingBasicData
                    {
                        id = table[i].id,
                        name = table[i].name,
                        value = (uint)table[i].br
                    };
                }
            }
            return result;
        }
        #endregion
        #region Tribes
        public RankingBasicData[] LoadRankingTribeBR()
        {
            RankingBasicData[] result = new RankingBasicData[]{};
            List<Tribes> table = connection.Query<Tribes>("SELECT id, totalBR FROM Tribes ORDER BY totalBR DESC");
            if(table.Count > 0)
            {
                result = new RankingBasicData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new RankingBasicData
                    {
                        id = table[i].id,
                        value = table[i].totalBR
                    };
                }
            }
            return result;
        }
        public RankingBasicData[] LoadRankingTribeWins()
        {
            RankingBasicData[] result = new RankingBasicData[]{};
            List<Tribes> table = connection.Query<Tribes>("SELECT id, wins FROM Tribes ORDER BY wins DESC");
            if(table.Count > 0)
            {
                result = new RankingBasicData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new RankingBasicData
                    {
                        id = table[i].id,
                        value = table[i].wins
                    };
                }
            }
            return result;
        }
        #endregion
        #region Pets
        public SummonableRankingData[] LoadRankingPetBR()
        {
            SummonableRankingData[] result = new SummonableRankingData[]{};
            List<Pets> table = connection.Query<Pets>("SELECT owner, id, tier, br FROM Pets ORDER BY br DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new SummonableRankingData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new SummonableRankingData
                    {
                        prefab = table[i].id,
                        tier = table[i].tier,
                        value = table[i].br,
                        ownerId = table[i].owner,
                        ownerName = connection.FindWithQuery<Characters>("SELECT name FROM Characters WHERE id=?", table[i].owner).name
                    };
                }
            }
            return result;
        }
        public SummonableRankingData[] LoadRankingPetLvl()
        {
            SummonableRankingData[] result = new SummonableRankingData[]{};
            List<Pets> table = connection.Query<Pets>("SELECT owner, id, tier, level FROM Pets ORDER BY level DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new SummonableRankingData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new SummonableRankingData
                    {
                        prefab = table[i].id,
                        tier = table[i].tier,
                        value = table[i].level,
                        ownerId = table[i].owner,
                        ownerName = connection.FindWithQuery<Characters>("SELECT name FROM Characters WHERE id=?", table[i].owner).name
                    };
                }
            }
            return result;
        }
        #endregion
        #region Mounts
        public SummonableRankingData[] LoadRankingMountBR()
        {
            SummonableRankingData[] result = new SummonableRankingData[]{};
            List<Mounts> table = connection.Query<Mounts>("SELECT owner, id, tier, br FROM Mounts ORDER BY br DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new SummonableRankingData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new SummonableRankingData
                    {
                        prefab = table[i].id,
                        tier = table[i].tier,
                        value = table[i].br,
                        ownerId = table[i].owner,
                        ownerName = connection.FindWithQuery<Characters>("SELECT name FROM Characters WHERE id=?", table[i].owner).name
                    };
                }
            }
            return result;
        }
        public SummonableRankingData[] LoadRankingMountLvl()
        {
            SummonableRankingData[] result = new SummonableRankingData[]{};
            List<Mounts> table = connection.Query<Mounts>("SELECT owner, id, tier, level FROM Mounts ORDER BY level DESC LIMIT 100");
            if(table.Count > 0)
            {
                result = new SummonableRankingData[table.Count];
                for(int i = 0; i < table.Count; i++)
                {
                    result[i] = new SummonableRankingData
                    {
                        prefab = table[i].id,
                        tier = table[i].tier,
                        value = table[i].level,
                        ownerId = table[i].owner,
                        ownerName = connection.FindWithQuery<Characters>("SELECT name FROM Characters WHERE id=?", table[i].owner).name
                    };
                }
            }
            return result;
        }
        #endregion
    #endregion
    #region Teams
        public void SetNextTeamId()
        {
            Teams maxInDB = connection.FindWithQuery<Teams>("SELECT id FROM Teams WHERE id=(SELECT MAX(id) FROM Teams)");
            //uint maxInDB = connection.ExecuteScalar<uint>("SELECT MAX(id) FROM Teams");
            if(maxInDB != null)
            {
                TeamSystem.nextTeamId = maxInDB.id;
            }
        }
        async Task LoadTeam(Player player)
        {
            if(player.teamId < 1) // guard
                return;

            if(TeamSystem.teams.ContainsKey(player.teamId))
            {
                TeamSystem.SetMemberOnline(player.teamId, player.id);
            }
            else
            {
                Teams info = connection.FindWithQuery<Teams>("SELECT * FROM Teams WHERE id=?", player.teamId);
                if(info != null)
                {
                    Team team = new Team
                    {
                        id = info.id,
                        leaderId = info.leaderId,
                        share = info.share
                    };
                    List<Characters> members = connection.Query<Characters>("SELECT id, name, level, avatar FROM Characters WHERE teamId=?", player.teamId);
                    team.members = new TeamMember[members.Count];
                    for(int i = 0; i < members.Count; i++)
                    {
                        if(Player.onlinePlayers.TryGetValue(members[i].id.Value, out Player member))
                        {
                            team.members[i].id = member.id;
                            team.members[i].name = member.name;
                            team.members[i].level = (byte)member.level;
                            team.members[i].avatar = member.avatar;
                            team.members[i].online = true;
                        }
                        else
                        {
                            team.members[i].id = members[i].id.Value;
                            team.members[i].name = members[i].name;
                            team.members[i].level = members[i].level;
                            team.members[i].avatar = members[i].avatar;
                            team.members[i].online = false;
                        }
                    }
                    TeamSystem.teams[team.id] = team;
                    TeamSystem.SetMemberOnline(team.id, player.id);
                }
                else
                {
                    player.teamId = 0;
                }
            }
        }
        public void RemoveTeam(uint teamId)
        {
            connection.Execute("DELETE FROM Teams WHERE id=?", teamId);
        }
        public void RemoveMemberFromTeam(uint playerId)
        {
            connection.Execute("UPDATE characters SET teamId=0 WHERE id=?", playerId);
        }
        public void SaveTeams()
        {
            foreach(Team team in TeamSystem.teams.Values)
            {
                connection.InsertOrReplace(new Teams
                {
                    id = team.id,
                    leaderId = team.leaderId,
                    share = team.share,
                });
            }
        }
    #endregion
    #region Achievements
        async Task LoadAchievements(Player player)
        {
            player.own.achievements = new SyncListAchievements();
            Archive archive = connection.FindWithQuery<Archive>("SELECT * FROM Archive WHERE owner=?", player.id);
            if(archive != null)
            {
                player.own.archive = new Achievements.Archive
                {
                    achievementPoints = archive.achievementPoints,
                    gainedGold = archive.gainedGold,
                    usedGold = archive.usedGold,
                    gainedDiamonds = archive.gainedDiamonds,
                    usedDiamonds = archive.usedDiamonds,
                    killStrike = archive.killStrike,
                    arena1v1Wins = archive.arena1v1Wins,
                    arena1v1Losses = archive.arena1v1Losses,
                    highestArena1v1Points = archive.highestArena1v1Points
                };
                List<DatabaseModels.Achievements> table = connection.Query<DatabaseModels.Achievements>("SELECT id, claimed FROM Achievements WHERE owner=? ORDER BY id", player.id);
                if(table.Count > 0)
                {
                    for(int i = 0; i < table.Count; i++)
                    {
                        player.own.achievements.Add(new Achievements.Achievement
                        {
                            id = table[i].id,
                            claimed = table[i].claimed
                        });
                    }
                }
            }
            else
            {
                player.own.archive = new Achievements.Archive();
            }
            player.inprogressAchievements = new Achievements.InprogressAchievements(player);
        }
        public void SaveAchievements(Player player)
        {
            connection.InsertOrReplace(new Archive
            {
                owner = player.id,
                achievementPoints = player.own.archive.achievementPoints,
                gainedGold = player.own.archive.gainedGold,
                usedGold = player.own.archive.usedGold,
                gainedDiamonds = player.own.archive.gainedDiamonds,
                usedDiamonds = player.own.archive.usedDiamonds,
                killStrike = player.own.archive.killStrike,
                arena1v1Wins = player.own.archive.arena1v1Wins,
                arena1v1Losses = player.own.archive.arena1v1Losses,
                highestArena1v1Points = player.own.archive.highestArena1v1Points
            });
            if(player.own.achievements.Count > 0)
            {
                connection.Execute("DELETE FROM Achievements WHERE owner=?", player.id);
                for(int i = 0; i < player.own.achievements.Count; i++)
                {
                    connection.Insert(new DatabaseModels.Achievements
                    {
                        owner = player.id,
                        id = player.own.achievements[i].id,
                        claimed = player.own.achievements[i].claimed
                    });
                }
            }
        }
    #endregion
    #region Marriage
        public void LoadMarriage(Player player, uint sId)
        {
            if(sId < 1)
                return;
            if(Player.onlinePlayers.ContainsKey(sId))
            {
                player.own.marriage = new Marriage
                {
                    level = Player.onlinePlayers[sId].own.marriage.level,
                    exp = Player.onlinePlayers[sId].own.marriage.exp,
                    spouse = sId,
                    spouseLevel = (byte)Player.onlinePlayers[sId].level,
                    spouseName = Player.onlinePlayers[sId].name,
                    spouseOnline = true
                };
                return;
            }

            Marriages row = connection.FindWithQuery<Marriages>($"SELECT level, exp FROM Marriages WHERE {(player.gender == Gender.Male ? "hasband" : "wife")}=?", player.id);
            Characters spouse = connection.FindWithQuery<Characters>("SELECT name, level FROM Characters WHERE id=?", sId);
            
            if(row != null && spouse != null)
            {
                player.own.marriage = new Marriage
                {
                    level = row.level,
                    exp = row.exp,
                    spouse = sId,
                    spouseLevel = spouse.level,
                    spouseName = spouse.name,
                    spouseOnline = false
                };
            }
        }
        public void SaveMarriage(Player player)
        {
            if(player.IsMarried())
            {
            // if(Player.onlinePlayers.ContainsKey(player.own.marriage.spouse) && )
                //    return;
                connection.InsertOrReplace( new Marriages
                {
                    hasband = player.gender == Gender.Male ? player.id : player.own.marriage.spouse,
                    wife = player.gender == Gender.Female ? player.id : player.own.marriage.spouse,
                    level = player.own.marriage.level,
                    exp = player.own.marriage.exp
                });
            }
        }
    #endregion
    #region General
        public ulong TryLogin(string account, string password)
        {
            if (!string.IsNullOrWhiteSpace(account) && !string.IsNullOrWhiteSpace(password))
            {
                // demo feature: create account if it doesn't exist yet.
                // note: sqlite-net has no InsertOrIgnore so we do it in two steps
                if (connection.FindWithQuery<Accounts>("SELECT * FROM Accounts WHERE username=?", account) == null)
                {
                    connection.Insert(new Accounts{ username=account, password=password, created=DateTime.UtcNow, lastlogin=DateTime.Now, banned=false});
                }

                // check account username, password, banned status
                Accounts acc = connection.FindWithQuery<Accounts>("SELECT * FROM Accounts WHERE username=? OR email=? AND password=?", account, account, password);
                if (acc != null)
                {
                    if(acc.banned)
                    {
                        return 1;
                    }
                    // save last login time and return true
                    connection.Execute("UPDATE Accounts SET lastlogin=? WHERE id=?", DateTime.UtcNow, acc.id);
                    return acc.id.Value;
                }
            }
            return 0;
        }
    #endregion
    #region Server
        public void Connect()
        {
    #if UNITY_EDITOR
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, databaseFile);
    #elif UNITY_ANDROID
            string path = Path.Combine(Application.persistentDataPath, databaseFile);
    #elif UNITY_IOS
            string path = Path.Combine(Application.persistentDataPath, databaseFile);
    #else
            string path = Path.Combine(Application.dataPath, databaseFile);
    #endif
            connection = new SQLiteConnection(path);
            CheckTables();
        }
        void CheckTables()
        {
            // general
            connection.CreateTable<Accounts>(); // TODO: remove after making to MasterServer
            connection.CreateTable<Characters>();
            connection.CreateTable<Tribes>();
            connection.CreateTable<DatabaseModels.Server>();
            // indexed
            connection.CreateTable<Inventory>();
            connection.CreateIndex(nameof(Inventory), new []{"owner", "name"});
            connection.CreateTable<Equipments>();
            connection.CreateIndex(nameof(Equipments), new []{"owner", "name"});
            connection.CreateTable<Accessories>();
            connection.CreateIndex(nameof(Accessories), new []{"owner", "name"});
            connection.CreateTable<Skills>();
            connection.CreateIndex(nameof(Skills), new []{"id", "skill"});
            connection.CreateTable<Buffs>();
            connection.CreateIndex(nameof(Buffs), new []{"id", "buff"});
            // not indexed
            connection.CreateTable<PreviewData>();
            connection.CreateTable<Guilds>();
            connection.CreateTable<GuildMembers>();
            connection.CreateTable<GuildSkills>();
            connection.CreateTable<Pets>();
            connection.CreateTable<Mounts>();
            connection.CreateTable<Archive>(); 
            connection.CreateTable<DatabaseModels.Achievements>();
            connection.CreateTable<Clothing>();
            connection.CreateTable<Wardrobes>();
            connection.CreateTable<Mails>();
            connection.CreateTable<MailItems>();
            connection.CreateTable<HotEvents>();
            connection.CreateTable<HotEventsProgress>();
            connection.CreateTable<VIPs>();
            connection.CreateTable<Titles>();
            connection.CreateTable<Quests>();
            connection.CreateTable<Teams>();
            connection.CreateTable<Friends>();
            connection.CreateTable<Marriages>();
            connection.CreateTable<First7Days>();

            Debug.Log("Connected to Database Successfully");
        }
        void OnApplicationQuit()
        {
            connection?.Close();
            Debug.Log("Database Disconnected");
        }
        public void SaveServerData()
        {
            connection.InsertOrReplace(new DatabaseModels.Server
            {
                number = Server.number,
                name = Server.name,
                port = Server.port,
                createdAt = Server.createdAt,
            });
        }
        public bool LoadServerData()
        {
            DatabaseModels.Server info = connection.FindWithQuery<DatabaseModels.Server>("SELECT * FROM Server");
            if(info != null)
            {
                Server.number = info.number;
                Server.name = info.name;
                Server.port = info.port;
                Server.createdAt = info.createdAt;
                return true;
            }
            return false;
        }
        public void CreateServer(ushort number, string name, List<byte> tribes)
        {
            Server.number = number;
            Server.name = name;
            Server.createdAt = DateTime.Now;
            connection.InsertOrReplace(new DatabaseModels.Server
            {
                number = number,
                name = name,
                createdAt = Server.createdAt
            });
            for(int i = 0; i < tribes.Count; i++)
            {
                connection.Insert(new Tribes
                {
                    id = tribes[i]
                });
            }
            // set first id in characters
            uint serverId = (uint)(Server.number * 10000000);
            connection.InsertOrReplace(new Characters
            {
                id = serverId
            });
            connection.Execute("DELETE FROM Characters WHERE id=?", serverId);
            // set first id in Accounts (only on master server)
            connection.InsertOrReplace(new Accounts
            {
                id = 100000
            });
            connection.Execute("DELETE FROM Accounts WHERE id=?", 100000);
        }
        public int GetCharactersCount()
        {
            return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Characters");
        }
    #endregion
    }
}