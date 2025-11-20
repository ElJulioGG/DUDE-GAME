using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    public static GameManager instance;
    [Header("GameStatus")]
    [SerializeField] public bool gamePaused = false;
    [SerializeField] public bool assignController = false;

    [Header("PlayersStatus")]
    [SerializeField] public int playersActive = 0;
    [SerializeField] public bool playersCanMove = true;
    [SerializeField] public bool playersCanShoot = true;
    [SerializeField] public bool playersCanPickDrop = true;
    [SerializeField] public bool playersCanReload = true;
    [SerializeField] public bool playersCanAim = true;
    [SerializeField] public bool playersCanPowerUp = true;
    [SerializeField] public bool unlimitedBullets = false;

    [Header("Player Stats")]
    [SerializeField] public bool player1Playable = true;
    [SerializeField] public bool player2Playable = true;
    [SerializeField] public bool player3Playable = true;
    [SerializeField] public bool player4Playable = true;
    [SerializeField] public int player1Score = 0;
    [SerializeField] public int player2Score = 0;
    [SerializeField] public int player3Score = 0;
    [SerializeField] public int player4Score = 0;

    [SerializeField] public Sprite player1Icon;
    [SerializeField] public Sprite player2Icon;
    [SerializeField] public Sprite player3Icon;
    [SerializeField] public Sprite player4Icon;

    // 0: Xbox, 1: Switch, 2: PS5
    [SerializeField] public int player1ControllerType = 0;
    [SerializeField] public int player2ControllerType = 0;
    [SerializeField] public int player3ControllerType = 0;
    [SerializeField] public int player4ControllerType = 0;

    //0 = no power, 1 = instakill, 2 = doublePoints, 3 = OpenFire, 4 = MaxAmmo, *5 = fireSale*, 6 = kaboom, *7 = carpinter*, 8 = death machine
    [SerializeField] public Sprite[] powerUpIcons; 
    [SerializeField] public int player1PowerUp = 0;
    [SerializeField] public int player2PowerUp = 0;
    [SerializeField] public int player3PowerUp = 0;
    [SerializeField] public int player4PowerUp = 0;

    [Header("Camera")]
    [SerializeField] public int activeCamera = 0;
    [Header("LevelStatus")]
    [SerializeField] public bool destroyProyectiles = false;

    

   

    


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