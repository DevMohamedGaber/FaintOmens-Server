/*using UnityEngine;
using UnityEngine.UI;
public class UINotifications : MonoBehaviour {
    public static UINotifications list;
    [SerializeField] private UINotification prefab;
    void Awake() {
        if(list == null) list = this;
    }
    public void Add(string info) {
        GameObject go = Instantiate(prefab.gameObject);
        go.transform.SetParent(transform, false);
        go.GetComponent<UINotification>().Show(info);
    }
}*/