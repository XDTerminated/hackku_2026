using System.Collections;
using UnityEngine;

namespace HackKU.Core
{
    // A dead-simple global coroutine host. The FoodOrderController's detached delivery
    // coroutine runs here, not on the controller itself — so nothing on the controller
    // (EndOrder, StopCoroutine, OnDisable on scene unload) can possibly kill the box spawn.
    [DefaultExecutionOrder(-5000)]
    public class DeliveryRunner : MonoBehaviour
    {
        static DeliveryRunner _instance;

        public static DeliveryRunner Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[DeliveryRunner]");
                Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<DeliveryRunner>();
                return _instance;
            }
        }

        public static Coroutine Run(IEnumerator routine)
        {
            Debug.Log("[DeliveryRunner] starting coroutine");
            return Instance.StartCoroutine(routine);
        }
    }
}
