using System.Collections.Generic;                                                                            //use System.Collections.Generic - for lists
using UnityEngine;                                                                                           //use UnityEngine for MonoBehaviour and other Unity features
using UnityEngine.InputSystem;                                                                               //use UnityEngine.InputSystem for the new Input System
using UnityEngine.UI;                                                                                        //use UnityEngine.UI for Slider references
using UnityEngine.SceneManagement;                                                                          //use UnityEngine.SceneManagement for reloading the scene on reset
using TMPro;                                                                                                 //use TMPro for TextMeshPro references
using PartyTaxes;                                                                                            //use the PartyTaxes namespace to access the PTSoul class

[System.Serializable]
public class DeadCharacterData                                                                               //class to store information about dead characters for potential resurrection
{
    public string name;
    public string type;
    public int level;
    public int maxHP;
    public int attack;
    public int defense;
    public int dailywages;
    public int currentXp;
    public GameObject originalPrefab;

    public DeadCharacterData(PTSoul soul, GameObject prefab)                                                 //constructor to copy data from a PTSoul
    {
        name = soul.Name;
        type = soul.Type;
        level = soul.level;
        maxHP = soul.maxHP;
        attack = soul.attack;
        defense = soul.defense;
        dailywages = soul.dailywages;
        currentXp = soul.currentXp;
        originalPrefab = prefab;
    }

    public void ApplyTo(PTSoul soul, float statPenalty = 0.9f)                                               //restore character data onto a PTSoul with optional stat penalty
    {
        soul.Name = name;
        soul.Type = type;
        soul.level = level;
        soul.maxHP = Mathf.RoundToInt(maxHP * statPenalty);                                                  //apply stat penalty
        soul.currentHP = soul.maxHP;                                                                         //resurrect at full HP
        soul.attack = Mathf.RoundToInt(attack * statPenalty);                                                //apply stat penalty
        soul.defense = Mathf.RoundToInt(defense * statPenalty);                                              //apply stat penalty
        soul.dailywages = dailywages;
        soul.currentXp = currentXp;
        soul.xpToNextLevel = (soul.level + 1) * 50;
        soul.markedByDeath = true;                                                                           //mark as resurrected
        soul.isCowardly = false;                                                                             //resurrected souls are fearless
        soul.UpdateUI();
    }
}

public partial class PTManager : MonoBehaviour, SimpleControls.IPlayerActions                                //inherit from MonoBehaviour and implement the IPlayerActions interface from the SimpleControls input actions
{
#region Variables
  public bool debugMode = false;                                                                             //bool for debug toggling 

  [Header("Character & Enemy Prefabs")]                                                                      //header for character prefabs in the inspector
  [Tooltip("Base prefab used to instantiate party members. Must have a PTSoul component.")]
  public GameObject partyBasePrefab;                                                                         //base prefab for spawning party members (randomized by PTSoulGen)
  public int startingPartySize = 2;                                                                          //number of random party members to spawn at game start
  public GameObject[] enemyPrefabs;                                                                           //array of enemy prefabs to randomly spawn from
  public Transform partySpawnPoint;                                                                          //transform for party spawn location
  public Transform enemySpawnPoint;                                                                          //transform for enemy spawn location

  [Header("Party & Combat")]                                                                                 //header for party and combat settings in the inspector                           
  public List<PTSoul> partyMembers = new List<PTSoul>();                                                     //list to store the party members, using the PTSoul class for an individuals information.
  public List<PTSoul> adversaries = new List<PTSoul>();                                                      //list to store the enemies, using the PTSoul class for an individuals information.
  public int gold = 90;                                                                                      //integer # of gold
  public static event System.Action<int> OnGoldChanged;                                                     //event fired whenever gold changes, passes the new gold value
  public int commission = 50;                                                                                //integer # gold bonus from surviving combat
  public int weeklyBonusCommission = 50;                                                                     //integer # gold bonus from living for a week
  public int totalWages;                                                                                     //integer # for total calculated wages, party member count x character dailywages.
  public int daysElapsed = 0;                                                                                //integer # of days elapsed       
  public int partyCount => partyMembers.Count;                                                               //derived from list - always in sync
  public int enemyCount => adversaries.Count;                                                                //derived from list - always in sync
  public int maxPartyCount = 4;                                                                              //integer # of max party members
  public AudioClip BGMusic;                                                                                  //AudioClip for background music
  public AudioClip goldSound;                                                                                //AudioClip for gold sound effect
  public GameObject Sun;                                                                                     //Reference to the sun onject for changing instensity when sleeping
  public string partyName = "The Adventurers";                                                               //variable string for the party name
#endregion

#region UI References and Messages
  [Header("UI References")]                                                                                 //header for UI references in the inspector
  public TextMeshProUGUI battleText;                                                                        //reference to TextMeshPro level text obj
  public TextMeshProUGUI goldText;                                                                          //reference to TextMeshPro gold text obj
  public TextMeshProUGUI dayText;                                                                           //reference to TextMeshPro day text obj
  public TextMeshProUGUI PartyNameText;                                                                     //reference to TextMeshPro party name text obj
  public TextMeshProUGUI controlsText;                                                                    //reference to TextMeshPro combat log text obj
  public string dailyMsg;                                                                                    //variable string for the daily message
  public string enemyMsg;                                                                                    //variable string for the enemy message
  public string[] sleepMsgs = {                                                                              //string array for possible messages
    "You sleep roughly through the night and wake up feeling restless.",
    "You sleep soundly through this night and wake up feeling refreshed.",
    "You don't sleep at all this night, you feel lethargic and worn out."
  };                                                                                                         // possible sleep messages

  public bool canSleep = true;                                                                               //bool to check if sleeping is allowed  
  public bool hasFought = false;                                                                             //bool to check if the player has fought
  public bool hasHealed = false;                                                                             //bool to check if the player has healed
  public bool hasBeenBlessed = false;
  public bool gameOver = false;                                                                              //bool to check if the game is over  
  public int daysWithoutGold = 0;                                                                            //integer tracking consecutive days the party has had no gold, game over after 3
  

  private SimpleControls controls;                                                                           //reference to the input controls
  private AudioSource audioSource;                                                                           //reference to the AudioSource component
  private HashSet<string> deadCharacterNames = new HashSet<string>();                                       //tracks names of deceased characters to prevent respawning
  private List<DeadCharacterData> deadCharacters = new List<DeadCharacterData>();                           //stores full data of dead characters for resurrection
  private PTSoulGen soulGen;                                                                                 //reference to the soul generator for spawning recruits and enemies

//constants for game balance and tuning
  private const int DAYS_UNTIL_MUTINY = 3;                                                                   //days without gold before game over
  private const int BASE_HEAL_AMOUNT = 50;                                                                   //base HP restored per party member
  private const int HEAL_COST_PER_MEMBER = 10;                                                               //gold cost per party member for healing
  private const int BLESS_COST_PER_MEMBER = 100;                                                             //gold cost per party member for blessing
  private const int BASE_RESURRECTION_COST = 100;                                                            //base gold cost for resurrection
  private const float THEFT_CHANCE_BROKE = 0.25f;                                                            //25% theft chance when broke
  private const float THEFT_CHANCE_NORMAL = 0.1f;                                                           //10% theft chance normally
  private const float THEFT_PERCENT_BROKE = 0.1f;                                                           //10% of gold stolen when broke
  private const float THEFT_PERCENT_NORMAL = 0.2f;                                                          //20% of gold stolen normally
  private const float FLEE_COST_PERCENT = 0.1f;                                                              //10% of gold to flee
  private const int RESURRECTION_UNLOCK_DAY = 7;                                                             //day when resurrection becomes available
  private const int WEEKLY_BONUS_INTERVAL = 7;                                                               //days between weekly bonuses
#endregion

#region Initilization and Update
    void Awake()                                                                                             //On Awake, called once before start.   
    {
        controls = new SimpleControls();                                                                     //initialize controls in Awake (ScriptableObject creation is not allowed in field initializers)
        controls.Player.SetCallbacks(this);                                                                  //register input callbacks
    }

    void Start()                                                                                             //On Game Start, called once before update
    {   
        // Set up background music
        audioSource = GetComponent<AudioSource>();                                                           //get the AudioSource component
        if (BGMusic != null)                                                                                 //if BGMusic clip is assigned
        {
            audioSource.clip = BGMusic;                                                                      //set the audio clip
            audioSource.loop = true;                                                                         //enable looping
            audioSource.Play();                                                                              //play the music
        }

        soulGen = GetComponent<PTSoulGen>();                                                                  //get the soul generator component
        SetSunIntensity(2f);                                                                                 //set initial sun intensity

        for (int i = 0; i < startingPartySize; i++)                                                          // Spawn random starting party members
        {
            SpawnRandomPartyMember();                                                                        //spawn a random soul using the base prefab and soul generator
        }

        dailyMsg = "Heil, <partyname>" + partyName + "</partyname>" +                                        //greet the party,
                   "! You have " + gold +                                                                   //display the party gold
                   " gold and " + partyMembers.Count +                                                      //display the number of party members
                   " party members. Daily Wages are " + CalculateTotalDailyWages() + ".";                   //display the total daily wages 
        daysElapsed++;                                                                                      //increment the day
        PTAdventureLog.Log(dailyMsg);                                                                       //print the daily message
        UpdateUI();                                                                                         //update UI with initial values

        if (debugMode)                                                                                      //if debug mode is enabled, print debug message
        {
            Debug.Log("Debug Mode Enabled");
        }
    }

    void Update()                                                                                           //On Update
    {
        if (gameOver) return;                                                                               //skip if game is already over
    }

    void OnEnable()                                                                                         //Method for enabling the input controls
    {
        controls?.Player.Enable();    //NOTE TO SELF: null conditional to avoid errors if controls is not assigned for some reason
    }

    void OnDisable()                                                                                        //Method for disabling the input controls
    {
        controls?.Player.Disable();
    }
#endregion

#region Shared Helpers

    int CalculateTotalDailyWages()                                                                          //Integer Method to calculate total daily wages from all party members
    {
        totalWages = 0;                                                                                     //initialize total wages
        foreach (PTSoul member in partyMembers)                                                             //loop through all party members
        {
            totalWages += member.dailywages;                                                                //add each member's daily wages to total
        }
        return totalWages;                                                                                  //return total wages
    }

    void UpdateUI()                                                                                         //Method to update UI elements
    {
        if (goldText != null)
        {
            goldText.text = "Gold: " + gold;                                                                //display current gold
            goldText.color = (gold <= 0 || daysWithoutGold > 0) ? Color.red : new Color(1f, 0.84f, 0f);   //tint red when broke or broke counter is active, gold colour otherwise
        }
        
        if (battleText != null)
        {
            if (adversaries.Count > 0)                                                                      //if in battle
            {
                battleText.text = "In battle with " + adversaries.Count + " enemies";           //display enemy count
            }
            else                                                                                            //if not in battle
            {
                battleText.text = "In Town";                                                   //display in town message
            }
        }

        if (dayText != null)
        {
            dayText.text = "Day's passed: " + daysElapsed;                                                            //display current day
        }

        if (PartyNameText != null)
        {
            PartyNameText.text = partyName;                                                                   //display party name
        }

        UpdateControlsDisplay();                                                                            //update contextual controls display
    }

    void UpdateControlsDisplay()                                                                            //Method to update controls display based on context
    {
        if (controlsText == null) return;                                                                   //exit if controls text reference not assigned

        string controls = "";

        if (adversaries.Count > 0)                                                                          //in battle - show combat controls
        {
            controls = "<b>COMBAT</b>\n";
            controls += "Left Click - Attack\n";
            controls += "R - Run Away\n";
        }
        else                                                                                                //in town - show town controls
        {
            controls = "<b>TOWN</b>\n";
            controls += "E - Encounter Enemy\n";
            controls += "Spacebar - Sleep\n";
            controls += "F - Recruit Member\n";
            if (!hasHealed)                                                                                 //only show heal option if the player has not healed yet today
            {
                controls += "Q - Heal Party\n";
            }
             if (!hasBeenBlessed)                                                                             //only show bless option if the player has not blessed yet today
            {
                controls += "1 - Bless Party\n";
            }

            if (daysElapsed >= RESURRECTION_UNLOCK_DAY)                                                      //only show resurrect after it unlocks on day 7
            {
                controls += "3 - Resurrect\n";
            }
        }

        controlsText.text = controls;                                                                       //update the controls text
    }

    void ChangeGold(int amount)                                                                             //Method to change gold amount, takes an integer amount as a parameter (positive or negative)
    {
        if (amount == 0)
        {
            return;
        }

        gold += amount;
        gold = Mathf.Max(gold, 0);                                                                         //clamp gold so it never goes negative
        OnGoldChanged?.Invoke(gold);                                                                       //notify all listeners of the new gold value

        if (goldSound != null)
        {
            audioSource.PlayOneShot(goldSound);                                                             //play gold sound effect when gold changes
        }

        UpdateUI();                                                                                         //automatically update UI whenever gold changes
    }

    void TriggerGameOver(string message)                                                                    //Centralized method to trigger game over state
    {
        PTAdventureLog.Log(message + " Game Over.");
        gameOver = true;
    }

    void SetSunIntensity(float intensity)                                                                   //Method to change the intensity of the sun on sleep 
    {
        if (Sun == null)
        {
            return;
        }

        Light sunLight = Sun.GetComponent<Light>();                                                         //get sun's light component

        if (sunLight != null)
        {
            sunLight.intensity = intensity;                                                                 //set sun light intensity                   
        }
    }
#endregion

    public void On_2(InputAction.CallbackContext context)
    {
        return;
    }
    public void On_4(InputAction.CallbackContext context)
    {
        return;
    }

    public void On_5(InputAction.CallbackContext context)
    {
        return;
    }
}