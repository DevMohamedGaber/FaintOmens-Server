/*using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class UINotification : MonoBehaviour {
    [SerializeField] private Button body;
    [SerializeField] private Text info;
    void Awake() {
        body.onClick.SetListener(close);
    }
    public void Show(string text) {
        info.text = text;
        StartCoroutine(ShowNotification());
    }
    IEnumerator<WaitForSeconds> ShowNotification() {
        yield return new WaitForSeconds(5);
        close();
    }
    void close() {
        // NOTE: cancel the coroutine first
        Destroy(gameObject);
    }
}*/