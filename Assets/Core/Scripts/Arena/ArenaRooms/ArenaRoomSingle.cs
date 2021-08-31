using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Game.Arena;
namespace Game.Components
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class ArenaRoomSingle : MonoBehaviour {
        [SerializeField]
        GameObject _entry1, _entry2;
        public short id;
        public bool isFree = true;
        public Vector3 entry1 => _entry1.transform.position;
        public Vector3 entry2 => _entry2.transform.position;
        ArenaMatch1v1 match;
        WaitForSeconds cancelIfNotReadyTime, endIfNotReadyTime, startTime, matchDuration, wrapUpTime;
        public void SetUp(ArenaMatch1v1 match)
        {
            this.match = match;
            isFree = false;
        }
        public void WaitForAcceptanceOrCancelMatch()
        {
            Debug.Log("WaitForAcceptanceOrCancelMatch");
            StartCoroutine(CancelIfNotReady());
        }
        public void WaitForPlayersToTransport()
        {
            Debug.Log("WaitForPlayersToTransport");
            StopCoroutine(CancelIfNotReady());
            StartCoroutine(EndIfNotReady());
        }
        public void StartCountDown()
        {
            Debug.Log("StartCountDown");
            StopCoroutine(EndIfNotReady());
            StartCoroutine(StartMatch());
        }
        public void WrapUp()
        {
            Debug.Log("WrapUp");
            StopCoroutine(TimeIsUp());
            StartCoroutine(WrapUpMatch());
        }
        IEnumerator<WaitForSeconds> CancelIfNotReady()
        {
            yield return cancelIfNotReadyTime;
            Debug.Log("CancelIfNotReady");
            match.Cancel();
        }
        IEnumerator<WaitForSeconds> EndIfNotReady()
        {
            yield return endIfNotReadyTime;
            Debug.Log("EndIfNotReady");
            match.EndIfNotReady();
        }
        IEnumerator<WaitForSeconds> StartMatch()
        {
            yield return startTime;
            Debug.Log("StartMatch");
            match.Start();
            StartCoroutine(TimeIsUp());
        }
        IEnumerator<WaitForSeconds> TimeIsUp()
        {
            yield return matchDuration;
            Debug.Log("TimeIsUp");
            match.TimeIsUp();
        }
        IEnumerator<WaitForSeconds> WrapUpMatch()
        {
            yield return wrapUpTime;
            Debug.Log("WrapUpMatch");
            match.WrapUp();
            isFree = true;
        }
        void Start()
        {
            cancelIfNotReadyTime = new WaitForSeconds(Storage.data.arena.cancelIfNotReadyTime);
            endIfNotReadyTime = new WaitForSeconds(Storage.data.arena.endIfNotReadyTime);
            startTime = new WaitForSeconds(Storage.data.arena.startTime);
            matchDuration = new WaitForSeconds(Storage.data.arena.matchDurationInMins * 60);
            wrapUpTime = new WaitForSeconds(Storage.data.arena.wrapUpTime);
        }
    }
}