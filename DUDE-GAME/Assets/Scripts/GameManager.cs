using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [Header("PlayersStatus")]
    [SerializeField] public int playersActive = 0;
    [SerializeField] public bool playersCanMove = true;
    [SerializeField] public bool unlimitedBullets = false;

    [Header("Player Stats")]
    [SerializeField] public int player1Score = 0;
    [SerializeField] public int player2Score = 0;
    [SerializeField] public int player3Score = 0;
    [SerializeField] public int player4Score = 0;

    [SerializeField] public Sprite player1Icon;
    [SerializeField] public Sprite player2Icon;
    [SerializeField] public Sprite player3Icon;
    [SerializeField] public Sprite player4Icon;

    //0 = no power, 1 = instakill, 2 = doublePoints, 3 = OpenFire, 4 = MaxAmmo, 5 = fireSale, 6 = kaboom, 7 = carpinter, 8 = death machine
    [SerializeField] public Sprite[] powerUpIcons; 
    [SerializeField] public int player1PowerUp = 0;
    [SerializeField] public int player2PowerUp = 0;
    [SerializeField] public int player3PowerUp = 0;
    [SerializeField] public int player4PowerUp = 0;

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