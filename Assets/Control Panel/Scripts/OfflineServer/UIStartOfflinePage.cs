using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
namespace Game.ControlPanel
{
    public class UIStartOfflinePage : MonoBehaviour
    {
        [SerializeField] kcp2k.KcpTransport transport;
        [Header("Start")]
        [SerializeField] GameObject OfflineServerInfoPanal;
        [SerializeField] Text OfflineServerNumber;
        [SerializeField] Text OfflineServerName;
        [SerializeField] Text OfflineServerCreatedAt;
        [SerializeField] Transform OfflineServerTribesContent;
        [SerializeField] InputField OfflineServerPort;
        [SerializeField] UIStartServerTribe OfflineServerTribePrefab;
        [Header("Create")]
        [SerializeField] GameObject CreateServerInfoPanal;
        [SerializeField] InputField CreateServerNumber;
        [SerializeField] InputField CreateServerName;
        [SerializeField] InputField CreateServerTribesCount;
        [SerializeField] Transform CreateServerTribes;
        [SerializeField] Dropdown CreateServerTribeMenuPrefab;

        public void StartServer() {
            if(TribeSystem.tribes.Count >= 2) {
                SetPort(Convert.ToUInt16(OfflineServerPort.text));
                UIManager.data.initializer.StartServer();
                Invoke(nameof(ShowHomePage), 2f);
                OfflineServerInfoPanal.SetActive(false);
            }
        }
        public void CreateServer() {
            if(CreateServerNumber.text == "" || CreateServerName.text.Length < 3)
                return;
            ushort number = Convert.ToUInt16(CreateServerNumber.text);

            List<byte> ids = new List<byte>();
            for(int i = 0; i < Convert.ToInt32(CreateServerTribesCount.text); i++)
                ids.Add(Convert.ToByte(CreateServerTribes.GetChild(i).GetComponent<Dropdown>().value + 1));
            List<byte> duplicates = ids.FindDuplicates(tribe => tribe);
            if(duplicates.Count < 1) {
                if(number < 1 || CreateServerName.text.Length < 3 || ids.Count < 2) {
                    Debug.Log("Some thing wronge with the inputs");
                    return;
                }
                CreateServerInfoPanal.SetActive(false);
                Database.singleton.CreateServer(number, CreateServerName.text, ids);
                TribeSystem.LoadTribes();
                Invoke(nameof(UpdateData), 2f);
            }
            else Debug.Log(duplicates.Count + " duplicates found in tribes selection.");
        }
        public void UpdateCreateTribeFieldCount() {
            int count = CreateServerTribesCount.text != "" ? Convert.ToInt32(CreateServerTribesCount.text) : 2;
            if(count >= 2)
                UIUtils.BalancePrefabs(CreateServerTribeMenuPrefab.gameObject, count, CreateServerTribes);
        }
        void ShowHomePage() {
            UIManager.data.homePage.Show();
            Hide();
        }
        void UpdateData() {
            OfflineServerNumber.text = Server.number.ToString();
            OfflineServerName.text = Server.name;
            OfflineServerCreatedAt.text = Server.createdAt.ToString();
            OfflineServerPort.text = Server.port.ToString();
            UIUtils.BalancePrefabs(OfflineServerTribePrefab.gameObject, TribeSystem.tribes.Count, OfflineServerTribesContent);
            int i = 0;
            foreach (byte tribe in TribeSystem.tribes.Keys) {
                UIStartServerTribe row = OfflineServerTribesContent.GetChild(i).GetComponent<UIStartServerTribe>();
                row.flag.sprite = ScriptableTribe.dict[tribe].flag;
                row.Name.text = ScriptableTribe.dict[tribe].Name;
                i++;
            }
            CreateServerInfoPanal.SetActive(false);
            OfflineServerInfoPanal.SetActive(true);
        }
        void ShowCreatePanel() {
            OfflineServerInfoPanal.SetActive(false);
            CreateServerInfoPanal.SetActive(true);
        }
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
        void SetPort(ushort port) {
            transport.Port = port;
            Server.port = port;
        }
        void Start() {
            Database.singleton.Connect();
            if(Database.singleton.LoadServerData()) {
                TribeSystem.LoadTribes();
                SetPort(Server.port);
                Invoke(nameof(UpdateData), 2f);
            }
            else ShowCreatePanel();
        }
    }
}