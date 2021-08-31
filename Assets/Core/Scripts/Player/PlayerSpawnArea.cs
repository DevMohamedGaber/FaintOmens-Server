using UnityEngine;
using UnityEngine.AI;
namespace Game.Components
{
    [RequireComponent(typeof(BoxCollider))] 
    public class PlayerSpawnArea : MonoBehaviour
    {
        int minX, maxX, minY, minZ, maxZ;
        System.Random rand => Utils.random;
        public Vector3 GetValidRandomPosition()
        {
            Vector3 result = GetRandomPoisition();
            byte tries = 0;
            while(IsOverlaping(result) && tries <= Storage.data.player.maxRandomSpawnIteration)
            {
                result = GetRandomPoisition();
                tries++;
            }
            return result;
        }
        Vector3 GetRandomPoisition()
        {
            return new Vector3 (
                (float)rand.Next(minX, maxX), // random x value
                minY, // the same level on y
                (float)rand.Next(minZ, maxZ) // random z value
            );
        }
        bool IsOverlaping(Vector3 pos)
        {
            Collider[] cols = Physics.OverlapSphere(pos, 1f);
            foreach(Collider col in cols)
            {
                if(col.GetComponentInParent<Entity>())
                    return true;
            }
            return false;
        }
        
        void Awake()
        {
            Collider col = gameObject.GetComponent<BoxCollider>();
            if(col != null)
            {
                minX = (int)col.bounds.min.x;
                maxX = (int)col.bounds.max.x + 1;
                minY = (int)col.bounds.min.y;
                minZ = (int)col.bounds.min.z;
                maxZ = (int)col.bounds.max.z + 1;
            }
            else
            {
                Debug.Log("Collider not found for this monster area");
            }
        }
    }
}