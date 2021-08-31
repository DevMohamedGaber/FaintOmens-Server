using UnityEngine;
using System.Collections.Generic;
using Mirror;
namespace Game
{
    public class Meteor : Entity {
        [Header("Meteor Synced")]
        [SyncVar, SerializeField] Tier tier = Tier.F;
        [Header("Meteor Static")]
        [SerializeField] Tier maxTier = Tier.S;
        [SerializeField] float respawnTimeInMins = 120;
        [SerializeField] MeteorSpawnData spawn;
        [SerializeField] byte currentWave = 0;
        public uint rewardExp;
        public uint rewardSkillExp;
        public uint rewardGold;
        int hpPerWave;
        int nextWaveHP;
        public override int healthMax => base.healthMax + _healthMax.Get(level);
        [Server] protected override EntityState UpdateServer() => EntityState.Idle;
        public void CheckWaves() {
            if(health <= nextWaveHP) {
                // Spawn Mobs in circle
                float angle = 360f / (float)spawn.spawns;
                Vector3 centerPos = transform.position;
                float radius = 5f;
                for(int i = 0; i < spawn.spawns; i++) {
                    Quaternion rotation = Quaternion.AngleAxis(i * angle, Vector3.up);
                    Vector3 direction = rotation * Vector3.forward;
                    Vector3 position = centerPos + (direction * radius);
                    GameObject go = Instantiate(spawn.prefab, position, rotation);
                    if(target != null) {
                        Monster monster = go.GetComponent<Monster>();
                        monster.respawn = false;
                        monster.OnAggro(target);
                    }
                    NetworkServer.Spawn(go);
                }
                currentWave++;
                nextWaveHP = (int)Mathf.Ceil(hpPerWave * currentWave);
            }
        }
        public void OnDestroyed(Player player) {
            if(player.InTeam()) {
                List<Player> members = player.GetTeamMembersInProximity();
                for(int i = 0; i < members.Count; i++) {
                    members[i].AddExp(Player.CalculateTeamExperienceShare(rewardExp, members.Count, members[i].level, level));
                    members[i].AddGold(rewardGold / (uint)members.Count);
                }
            }
            else {
                player.AddExp(Player.BalanceExpReward(rewardExp, player.level, level));
                player.AddGold(rewardGold);
            }
            // add to player achievements
            Hide();
            Invoke("ResetToOriginal", respawnTimeInMins * 60);
        }
        public void ResetDataToOriginal() {
            health = healthMax;
            nextWaveHP = healthMax;
            currentWave = 0;
            tier = (Tier)Random.Range(0, (int)maxTier);
            int tierFac = (int)tier + 1;
            rewardExp = (uint)(Storage.data.meteor.baseExp * level * tierFac);
            rewardGold = (uint)(Storage.data.meteor.baseGold * level * tierFac);
            //Debug.Log(health);
        }
        public void ResetToOriginal() {
            ResetDataToOriginal();
            Show();
        }
        public override void OnStartServer() {
            hpPerWave = healthMax / spawn.waves;
            ResetDataToOriginal();
        }
        protected override void OnTriggerExit(Collider col) {
            Player player = col.GetComponentInParent<Player>();
            if(player) {
                if(netIdentity.observers.Count == 0) {
                    health = healthMax;
                    currentWave = 0;
                }
            }
        }
        public override void Warp(Vector3 destination) {}
        public override void ResetMovement() {}
    }
}