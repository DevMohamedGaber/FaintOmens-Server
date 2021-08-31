/*
    - out [Meteor]
        - put logic on city area
        - ray cast
        - rand place on map
*/
using UnityEngine;
using System.Collections.Generic;
using Mirror;
namespace Game.Components
{
    [AddComponentMenu("Custom/MonstersSpawnArea")]
    [RequireComponent(typeof(BoxCollider))]
    public class MonstersSpawnArea : MonoBehaviour
    {
    #region Data
        [Header("Mobs")]
        [SerializeField] GameObject prefab;
        [SerializeField] ushort count = 10;
        [SerializeField] float mobRadius = 1f;
        [SerializeField] int level = 1;
        [SerializeField] ItemDropChance[] mobsDrop = new ItemDropChance[]{};
        [Header("Elites")]
        [SerializeField] GameObject elitePrefab;
        [SerializeField] int maxElites = 5;
        [SerializeField] float eliteRadius = 1f;
        [SerializeField] float eliteBonus = 1.5f;
        [SerializeField] double eliteRarity = .5d;
        [SerializeField] ItemDropChance[] elitesDrop = new ItemDropChance[]{};
        //cache
        List<GameObject> mobs = new List<GameObject>();
        List<GameObject> elites = new List<GameObject>();
        WaitForSeconds eliteCheckTimer;
        System.Random rand => Utils.random;
        int minX, maxX, minY, minZ, maxZ;
    #endregion
        // initiate method
        public void Init() {
            // guards
            if(prefab == null) {
                Debug.Log("prefab isn't found");
                return;
            }
            if(count < 1) {
                Debug.Log("count is less than 1");
                return;
            }
            if(level < 1) {
                Debug.Log("count is less than 1");
                return;
            }
            SpawnMobs();
            StartSpawnElitesCicle();
        }
        // spawners
        void SpawnMobs() {
            for(int i = 0; i < count; i++) {
                GameObject go = Instantiate(prefab, GetValidRandomPosition(ref mobRadius), Quaternion.identity);
                go.name = prefab.name;
                Monster monster = go.GetComponent<Monster>();
                monster.level = level;
                monster.dropChances = mobsDrop;
                NetworkServer.Spawn(go);
                mobs.Add(go);
            }
        }
        void StartSpawnElitesCicle() {
            if(elitePrefab != null && maxElites > 0) {
                eliteCheckTimer = new WaitForSeconds(Storage.data.monsters.eliteSpawnCheckInMins);
                StartCoroutine(SpawnElites());
            }
        }
        IEnumerator<WaitForSeconds> SpawnElites() {
            if(elites.Count < maxElites) {
                if(rand.NextDouble() >= eliteRarity) {
                    GameObject go = Instantiate(elitePrefab, GetValidRandomPosition(ref eliteRadius), Quaternion.identity);
                    go.name = elitePrefab.name;
                    Monster elite = go.GetComponent<Monster>();
                    elite.level = level;
                    elite.dropChances = elitesDrop;
                    NetworkServer.Spawn(go);
                    elites.Add(go);
                }
            }
            yield return eliteCheckTimer;
        }
        // helpers
        Vector3 GetValidRandomPosition(ref float radius) {
            Vector3 result = GetRandomPoisition();
            byte tries = 0;
            while(IsOverlaping(result, ref radius) && tries <= Storage.data.monsters.maxRandomSpawnIteration) {
                result = GetRandomPoisition();
                tries++;
            }
            return result;
        }
        Vector3 GetRandomPoisition() => new Vector3 (
            (float)rand.Next(minX, maxX), // random x value
            minY, // the same level on y
            (float)rand.Next(minZ, maxZ) // random z value
        );
        bool IsOverlaping(Vector3 pos, ref float radius) {
            Collider[] cols = Physics.OverlapSphere(pos, radius);
            foreach(Collider col in cols) {
                if(col.GetComponentInParent<Entity>())
                    return true;
            }
            return false;
        }
        // lc
        void Awake() {
            Collider col = gameObject.GetComponent<BoxCollider>();
            if(col != null) {
                minX = (int)col.bounds.min.x;
                maxX = (int)col.bounds.max.x + 1;
                minY = (int)col.bounds.min.y;
                minZ = (int)col.bounds.min.z;
                maxZ = (int)col.bounds.max.z + 1;
            }
            else {
                Debug.Log("Collider not found for this monster area");
            }
        }
    }
}