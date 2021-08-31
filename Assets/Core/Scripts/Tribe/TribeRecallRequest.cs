using UnityEngine;
namespace Game
{
    [System.Serializable]
    public struct TribeRecallRequest
    {
        public static readonly TribeRecallRequest Empty = new TribeRecallRequest();
        public string name;
        public int city;
        public Vector3 position;

        public TribeRecallRequest(string name, int city, Vector3 position)
        {
            this.name = name;
            this.city = city;
            this.position = position;
        }
    }
}