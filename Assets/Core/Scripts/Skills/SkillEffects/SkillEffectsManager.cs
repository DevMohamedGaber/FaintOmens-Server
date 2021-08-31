/*using UnityEngine;
public class SkillEffectsManager : MonoBehaviour {
    public float destroyAfter;
    Entity caster;
    Entity target;
    public SkillEffectStepInfo[] effects;
    int level;

    //System.DateTime starting;
    public void Set(Entity skillCaster, int skillLevel) {
        caster = skillCaster;
        target = skillCaster.target;
        level = skillLevel;
        //starting = System.DateTime.Now;
    }
    public void InstantiateEffect(int index) {
        GameObject instance = Instantiate(effects[index].Effect, effects[index].StartPositionRotation.position, effects[index].StartPositionRotation.rotation);
        SkillEffect effect = instance.GetComponent<SkillEffect>();
        effect.caster = caster;
        effect.target = target;
        if(effect is CustomSkillEffect customEffect)
            customEffect.level = level;
        Mirror.NetworkServer.Spawn(instance);
        if (effects[index].UseLocalPosition) {
            instance.transform.parent = effects[index].StartPositionRotation.transform;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = new Quaternion();
        }
        
        Destroy(instance, effects[index].DestroyAfter);
    }
    void Start() {
        Destroy(gameObject, destroyAfter);
    }
}
[System.Serializable]

public struct SkillEffectStepInfo {
    public GameObject Effect;
    public Transform StartPositionRotation;
    public float DestroyAfter;
    public bool UseLocalPosition;
}*/