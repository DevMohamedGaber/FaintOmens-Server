using UnityEngine;
public abstract class ScriptableObjectNonAlloc : ScriptableObject
{
    // .name allocates and we call it a lot. let's cache it to avoid GC.
    // (4.1KB/frame for skillbar items before, 0KB now)
    int cachedName;
    public new int name
    {
        get
        {
            if (cachedName == 0)
                cachedName = base.name.ToInt();
            return cachedName;
        }
        // set: not needed, we don't change ScriptableObject names at runtime.
    }
}
