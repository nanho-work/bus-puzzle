using System;
using System.Collections;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BusPuzzle
{
    internal sealed class IosTrackingAuthorization : MonoBehaviour
    {
        private const string GameObjectName = "BusPuzzleIosTrackingAuthorization";

        private static IosTrackingAuthorization instance;
        private static Action pendingCompletion;
        private static bool isRequesting;
        private static bool isComplete;

        public static void RequestIfNeeded(Action onComplete)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (isComplete)
            {
                onComplete?.Invoke();
                return;
            }

            pendingCompletion += onComplete;
            if (isRequesting)
            {
                return;
            }

            isRequesting = true;
            var authorization = EnsureInstance();
            authorization.StartCoroutine(authorization.RequestNextFrame());
#else
            onComplete?.Invoke();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void BusPuzzle_RequestTrackingAuthorization(string gameObjectName);

        private IEnumerator RequestNextFrame()
        {
            yield return null;
            BusPuzzle_RequestTrackingAuthorization(GameObjectName);
        }

        public void HandleTrackingAuthorizationCompleted(string status)
        {
            CompletePendingRequests();
        }

        private static IosTrackingAuthorization EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            var gameObject = GameObject.Find(GameObjectName);
            if (gameObject == null)
            {
                gameObject = new GameObject(GameObjectName);
                DontDestroyOnLoad(gameObject);
            }

            instance = gameObject.GetComponent<IosTrackingAuthorization>();
            if (instance == null)
            {
                instance = gameObject.AddComponent<IosTrackingAuthorization>();
            }

            return instance;
        }

        private static void CompletePendingRequests()
        {
            isComplete = true;
            isRequesting = false;
            var callback = pendingCompletion;
            pendingCompletion = null;
            callback?.Invoke();
        }
#endif
    }
}
