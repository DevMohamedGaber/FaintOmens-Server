using System;
using System.Collections.Generic;
namespace Game
{
    public class PreviewSystem
    {
        public static Dictionary<uint, PreviewPlayerData> cashe = new Dictionary<uint, PreviewPlayerData>();
        public static PreviewPlayerData GetPlayerInfo(uint playerId)
        {
            if(Player.onlinePlayers.ContainsKey(playerId))
            {
                return Player_To_PreviewPlayerData(Player.onlinePlayers[playerId]);
            }
            else if(cashe.TryGetValue(playerId, out PreviewPlayerData playerData))
            {
                return playerData;
            }
            else
            {
                PreviewPlayerData playerDataFromDB = Database.singleton.CharacterPreview(playerId);
                if(playerDataFromDB.status)
                    cashe[playerId] = playerDataFromDB;
                return playerDataFromDB;
            }
        }
        static PreviewPlayerData Player_To_PreviewPlayerData(Player player)
        {
            PreviewPlayerData result = new PreviewPlayerData
            {
                status = true,
                id = player.id,
                name = player.name,
                level = (byte)player.level,
                gender = player.gender,
                classInfo = player.classInfo,
                tribeId = player.tribeId,
                guildName = player.InGuild() ? player.guild.name : "",
                vipLevel = player.own.vip.level,
                militaryRank = player.own.militaryRank,
                equipments = new Item[Storage.data.player.equipmentCount],
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
                speed = player.speed
            };
            for(int i = 0; i < player.equipment.Count; i++)
            {
                if(player.equipment[i].amount > 0)
                    result.equipments[i] = player.equipment[i].item;
            }
            return result;
        }
    }
}