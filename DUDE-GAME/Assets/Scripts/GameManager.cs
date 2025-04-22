using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [Header("PlayersStatus")]
    [SerializeField] private int playersActive = 0;
    [Header("Player Conditions")]
    [SerializeField] public bool playerSyncAtack = false;
    [SerializeField] public bool playerIsHit;
    [SerializeField] public bool playerInvinsibility;
    [SerializeField] public int currentPlayerWeapon = 0;
    [SerializeField] public bool playerDied;
    [SerializeField] public bool playerCanInput = true;
    [SerializeField] public bool playerCanMove = true;
    [SerializeField] public bool weaponChange = true;
    [SerializeField] public bool playerCanAtack = true;
    [SerializeField] public bool playerIsShadow = false;
    [SerializeField] public bool playerIsInDialog = false;
    [SerializeField] public bool playerCanDialog = false;
    [SerializeField] public bool playerCanAlchemy = false;
    [SerializeField] public bool playerPiercingShot = false;


    [SerializeField] public bool defendingBase = false;
    [SerializeField] public bool exploringAndFighting = false;
    [SerializeField] public bool brewingPotions = false;
    [SerializeField] public bool figthingBoss = false;


    [Header("Camera")]
    [SerializeField] public int activeCamera = 0;

    [Header("Materials")]
    [SerializeField] public int material1 = 0;
    [SerializeField] public int material2 = 0;
    [SerializeField] public int material3 = 0;
    [SerializeField] public int material4 = 0;

    [Header("Bases Conditions")]
    [SerializeField] public int basesDefended = 0;

    [Header("Area Manager")]
    [SerializeField] public bool blockPeCausa = true;
    [SerializeField] public bool blockTower1 = true;
    [SerializeField] public bool blockTower2 = true;
    [SerializeField] public bool BlockOutsideBase1A = true;
    [SerializeField] public bool BlockOutsideBase2A = true;
    [SerializeField] public bool BlockOutsideBase1B = true;
    [SerializeField] public bool BlockOutsideBase2B = true;
    [SerializeField] public bool BlockOutsideBase3B = true;
    [SerializeField] public bool BlockOutsideBase1C = true;
    [SerializeField] public bool BlockOutsideBase2C = true;
    [SerializeField] public bool BlockOutsideBase3C = true;
    [SerializeField] public bool BlockOutsideBase1D = true;
    [SerializeField] public bool BlockOutsideBase2D = true;
    [SerializeField] public bool BlockOutsideBase3D = true;
    [SerializeField] public bool BlockOutsideBase1E = true;
    [SerializeField] public bool BlockOutsideBase2E = true;
    [SerializeField] public bool TeleportBoss = false;
    [SerializeField] public int LastTeleportedFrom = 0;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


}