using Steamworks;
using UnityEngine;

public class SteamTest : MonoBehaviour {
    void Start() {
        if (!Packsize.Test()) {
            Debug.LogError("[Steamworks.NET] Packsize test failed.");
            return;
        }
        if (!DllCheck.Test()) {
            Debug.LogError("[Steamworks.NET] DllCheck test failed.");
            return;
        }

        try {
            if (SteamAPI.Init()) {
                Debug.Log("Steam connected as: " + SteamFriends.GetPersonaName());
                Debug.Log("Steam ID: " + SteamUser.GetSteamID());
            } else {
                Debug.LogError("SteamAPI.Init() failed. Is Steam running?");
            }
        } catch (System.DllNotFoundException e) {
            Debug.LogError("Steamworks DLL not found: " + e.Message);
        }
    }

    void Update() {
        SteamAPI.RunCallbacks();
    }

    void OnApplicationQuit() {
        SteamAPI.Shutdown();
    }
}