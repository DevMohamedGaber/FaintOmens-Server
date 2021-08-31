namespace Game
{
    [System.Serializable]
    public enum EntityState : byte
    {
        Idle,
        Moving,
        Casting,
        Stunned,
        Dead
    }
}