using System.Collections.Generic;                                                                            //use System.Collections.Generic - for lists
using UnityEngine;                                                                                           //use UnityEngine for MonoBehaviour and other Unity features
using UnityEngine.InputSystem;                                                                               //use UnityEngine.InputSystem for the new Input System
using UnityEngine.UI;                                                                                        //use UnityEngine.UI for Slider references
using UnityEngine.SceneManagement;                                                                          //use UnityEngine.SceneManagement for reloading the scene on reset
using TMPro;                                                                                                 //use TMPro for TextMeshPro references
using PartyTaxes;                                                                                            //use the PartyTaxes namespace to access the PTSoul class

public class PTManager : MonoBehaviour, SimpleControls.IPlayerActions                                        //inherit from MonoBehaviour and implement the IPlayerActions interface from the SimpleControls input actions
{
#region Variables
  public bool debugMode = false;                                                                             //bool for debug toggling 

  [Header("Character & Enemy Prefabs")]                                                                      //header for character prefabs in theinspector
  public GameObject[] characterPrefabs;                                                                      //array of character prefabs to instantiate for starting party
  public GameObject[] enemyPrefabs;                                                                           //array of enemy prefabs to randomly spawn from
  public Transform partySpawnPoint;                                                                          //transform for party spawn location
  public Transform enemySpawnPoint;                                                                          //transform for enemy spawn location

  [Header("Party & Combat")]                                                                                 //header for party and combat settings in the inspector                           
  public List<PTSoul> partyMembers = new List<PTSoul>();                                                     //list to store the party members, using the PTSoul class for an individuals information.
  public List<PTSoul> adversaries = new List<PTSoul>();                                                      //list to store the enemies, using the PTSoul class for an individuals information.
  public int enemyCount = 0;                                                                                 //integer # of enemies to fight
  public int gold = 90;                                                                                      //integer # of gold     
  public int commission = 50;                                                                                //integer # gold bonus from surviving combat
  public int weeklyBonusCommission = 50;                                                                     //integer # gold bonus from living for a week
  public int totalWages;                                                                                     //integer # for total calculated wages, party member count x character dailywages.
  public int daysElapsed = 0;                                                                                //integer # of days elapsed       
  public int partyCount;                                                                                     //integer # of party members
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

        SetSunIntensity(2f);                                                                                 //set initial sun intensity

        
        if (characterPrefabs != null && characterPrefabs.Length > 0)                                         // Create starting party members from prefabs
        {
            foreach (GameObject prefab in characterPrefabs)
            {
                AddPartyMember(prefab);                                                                     //add to party using the AddPartyMember function (which instantiates the prefab)
            }
        }

        partyCount = partyMembers.Count;                                                                    //set the party count to the number of party members in the list
        dailyMsg = "Heil, " + partyName +                                                                   //greet the party,
                   "! You have " + gold +                                                                   //display the party gold
                   " gold and " + partyMembers.Count +                                                      //display the number of party members
                   " party members. Daily Wages are " + CalculateTotalDailyWages() +                        //display the total daily wages 
                   ". Press E to encounter an enemy, Left Click to Attack it or" +                          //display instructions for fighting enemies
                   " Spacebar to sleep through the night.";                                                 //display instructions for sleeping
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

#region Party Taxes Systems
    void AddPartyMember(GameObject prefab)                                                                  //Method to add a new party member to the party, takes a GameObject prefab as a parameter
    {
        if (partyMembers.Count < maxPartyCount)                                                             //check if the party is not full
        {
            if (prefab != null)
            {
                Vector3 baseSpawnPosition = partySpawnPoint != null ?
                                            partySpawnPoint.position : 
                                            transform.position;                                             //use spawn point if assigned, otherwise use PTManager position
                float spacing = 1.2f;                                                                       //spacing between party members along x-axis

                int newCount = partyMembers.Count + 1;                                                      //total party size after this member is added
                float totalWidth = (newCount - 1) * spacing;                                                //total width of the party formation
                float startX = -(totalWidth / 2f);                                                          //leftmost position, so the group stays centered on the spawn point

                for (int j = 0; j < partyMembers.Count; j++)                                               //reposition existing members to keep the group centered
                {
                    partyMembers[j].transform.position = baseSpawnPosition + new Vector3(startX + j * spacing, 0, 0);
                }

                Vector3 spawnPosition = baseSpawnPosition + new Vector3(startX + partyMembers.Count * spacing, 0, 0); //new member takes the last slot
                
                GameObject characterObj = Instantiate(prefab,                                               //create an instance of the character at the designated spawn position as child of PTManager
                                                      spawnPosition, 
                                                      Quaternion.identity, 
                                                      transform);                                           
                PTSoul soul = characterObj.GetComponent<PTSoul>();                                          //get the PTSoul component from the instance
                if (soul != null)                                                                           //if the soul exists on the prefab
                {
                    partyMembers.Add(soul);                                                                 //add the new member to the party
                    partyCount = partyMembers.Count;                                                        //update the party count

                    if (debugMode)                                                                          //if debug mode is enabled, print debug message
                    {
                        Debug.Log("Added " + soul.Name + " to " + partyName + "!");
                    }
                    PTAdventureLog.Log("Added " + soul.Name + " to " + partyName + "!");                    //log message that a new member has joined
                }
            }
        }
        else                                                                                                //if the party is full
        {
            if (debugMode)                                                                                  //if debug mode is enabled, 
            {
                Debug.Log("The party is full! You cannot add more members to the party.");                  //print debug message
            }
            
            PTAdventureLog.Log("The party is full! You cannot add more members to the party.");             //log message that the party is full
        }
    }

    void RemovePartyMember(PTSoul member)                                                                   //Method to remove a party member and destroy their GameObject
    {
        if (partyMembers.Contains(member))
        {
            partyMembers.Remove(member);                                                                    //remove from list
            partyCount = partyMembers.Count;                                                                //update party count
            Destroy(member.gameObject);                                                                     //destroy the GameObject

            PTAdventureLog.Log(member.Name + " has left the party!");                                       //log message that a member has left

            if (debugMode)                                                                                  //if debug mode is enabled, print debug message
            {
                Debug.Log(member.Name + " has left the party!");
            }
        }
    }

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
            goldText.color = gold <= 0 ? Color.red : new Color(1f, 0.84f, 0f);                             //tint red when broke, gold colour otherwise
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
    }

    void ChangeGold(int amount)                                                                             //Method to change gold amount, takes an integer amount as a parameter (positive or negative)
    {
        if (amount == 0)
        {
            return;
        }

        gold += amount;
        gold = Mathf.Max(gold, 0);                                                                         //clamp gold so it never goes negative

        if (goldSound != null)
        {
            audioSource.PlayOneShot(goldSound);                                                             //play gold sound effect when gold changes
        }
    }

    int GetAveragePartyLevel()
    {
        if (partyMembers.Count == 0) return 0;
        int total = 0;
        foreach (PTSoul member in partyMembers)
        {
            total += member.level;
        }
        return total / partyMembers.Count;
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

#region Input Actions
    public void OnSleep(InputAction.CallbackContext context)                                                //Method to handle Spacebar press, using new InputActions system     
    {
        if (gameOver) return;                                                                               //block input after game over
        if (context.performed)
        {
            if (canSleep)                                                                                   //check if sleeping is allowed    
            {
                
                daysElapsed++;                                                                              //increment the day
                PTAdventureLog.Log(sleepMsgs[Random.Range(0, sleepMsgs.Length)] + " Day: " + daysElapsed);  //log sleeping message
                totalWages = CalculateTotalDailyWages();                                                    //calculate total daily wages from all party members
                
                if (gold >= totalWages)                                                                     //check if the party can afford to pay wages
                {
                    ChangeGold(-totalWages);                                                                //subtract total daily wages from gold
                    daysWithoutGold = 0;                                                                    //reset broke counter since wages were paid
                }
                else                                                                                        //if the party cannot afford wages
                {
                    daysWithoutGold++;                                                                      //increment the broke day counter
                    int daysRemaining = 3 - daysWithoutGold;                                               //calculate days remaining before game over
                    if (daysWithoutGold >= 3)                                                               //game over after 3 consecutive days without gold
                    {
                        PTAdventureLog.Log("The party has gone unpaid for 3 days and murdered you in your sleep! Game Over."); //log game over message
                        gameOver = true;                                                                    //set game over flag
                        return;                                                                             //exit early
                    }
                    PTAdventureLog.Log("WARNING: The party cannot be paid! They are very unhappy about this. " + daysRemaining + " day(s) until mutiny!"); //log warning message
                }

                                                                                                            // Theft chance — higher odds and larger cut during the broke period
                float theftChance  = daysWithoutGold > 0 ? 0.25f : 0.05f;                                   //25% chance for party theft when broke, 5% otherwise
                float theftPercent = daysWithoutGold > 0 ? 0.05f : 0.02f;                                   //they steal 5% when you are broke, 2% otherwise
                if (Random.value < theftChance && gold > 0)                                                 //roll for theft, only if there is gold to steal
                {
                    int stolen = Mathf.Max(1, Mathf.FloorToInt(gold * theftPercent));                       //calculate amount stolen, minimum 1
                    ChangeGold(-stolen);                                                                    //deduct stolen gold
                    string theftMsg = daysWithoutGold > 0
                        ? "The party is getting desperate... " + stolen + " gold has gone missing!"        //broke period message
                        : "The party has stolen from you. " + stolen + " gold is missing!";                //normal theft message
                    PTAdventureLog.Log(theftMsg);                                                          //log theft message
                }

                if (daysElapsed % 7 == 0)                                                                       //every seventh day, award the weekly bonus commission
                {
                    ChangeGold(weeklyBonusCommission);                                                      //add weekly bonus commission
                    PTAdventureLog.Log("A week has passed! The party earns a weekly bonus of " +
                                       weeklyBonusCommission + " gold!");                                   //log weekly bonus message
                }

                SetSunIntensity(2f);
                hasFought = false;                                                                          //reset hasFought for the new day
                hasHealed = false;                                                                          //reset hasHealed for the new day
                enemyCount = 0;                                                                              //reset enemy count for the new day
                
                dailyMsg = "Heil, " + partyName +                                                           //greet the party,
                          "! You have " + gold +                                                            //display the party gold
                          " gold and " + partyMembers.Count +                                               //display the number of party members
                          " party members. Daily Wages are " + totalWages +                                 //display the total daily wages 
                          ". Your party may Heal if the cleric is available, look for new members or head out on a hunt." +                   //display instructions for fighting enemies
                          " You may also sleep through this night.";                                          //display instructions for sleeping
                PTAdventureLog.Log(dailyMsg);                                                               //log the daily message
                UpdateUI();                                                                                 //update UI after sleeping

                if (debugMode)
                {
                    Debug.Log("party slept");
                }                                                                      
                
            }
            else
            {
                PTAdventureLog.Log("You cannot sleep!");                                                    //log cannot sleep message
            }
        }
    }

    public void OnEncounter(InputAction.CallbackContext context)                                                    //Method to handle E key press, using new InputActions system
    {
        if (gameOver) return;                                                                               //block input after game over
        if (context.performed)
        {
            if (!hasFought)                                                                                 // Only allow one encounter per day
            {
              float weekMultiplier = Mathf.Pow(1.5f, daysElapsed / 7);                                     //multiply enemy spawn by 1.5 for each week that has passed
              int baseMin = Mathf.RoundToInt(partyCount * weekMultiplier);                                 //minimum spawn count scaled by week
              int baseMax = Mathf.RoundToInt(partyCount * 3 * weekMultiplier);                             //maximum spawn count scaled by week
              int spawnCount = Random.Range(baseMin, baseMax);                                             //random number of enemies to spawn, scaled by weekly multiplier
              enemyCount += spawnCount;                                                                        
              
              if (enemyPrefabs != null && enemyPrefabs.Length > 0)                                          //Create enemy GameObjects from prefab array after a null check
              {
                  Vector3 baseSpawnPosition = enemySpawnPoint != null ? 
                                              enemySpawnPoint.position : 
                                              transform.position;                                           //use spawn point if assigned, otherwise use PTManager position
                  float minDistance = 1.5f;                                                                 //minimum distance between enemies
                  int maxAttempts = 30;                                                                     //maximum attempts to find valid position
                  
                  for (int i = 0; i < spawnCount; i++)                                                      //loop to add new enemies to the adversaries list based on the spawn count
                  {
                      Vector3 spawnPosition = baseSpawnPosition;
                      bool validPosition = false;
                      
                      
                      for (int attempt = 0; attempt < maxAttempts; attempt++)                               //for each, Try to find a non-overlapping position
                      {
                          Vector3 randomOffset = Random.insideUnitSphere * 3f;                              //generate random position within 3 unit radius
                          randomOffset.y = 0;                                                               //set y to 0 to keep enemies on the same horizontal plane
                          spawnPosition = baseSpawnPosition + randomOffset;                                 //calculate spawn position with offset
                          
                          
                          validPosition = true;                                                             
                          foreach (PTSoul existingEnemy in adversaries)                                     //for each existing enemy, check distance from the potential spawn position
                          {
                              if (Vector3.Distance(spawnPosition, existingEnemy.transform.position) < minDistance)
                              {
                                  validPosition = false;
                                  break;
                              }
                          }
                          
                          if (validPosition)
                              break;
                      }

                      GameObject randomEnemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];    //randomly select an enemy prefab from the array
                      int avgLevel = GetAveragePartyLevel();                                                //get the current average party level

                      
                      if (randomEnemyPrefab.GetComponent<PTSoul>().level > avgLevel)                        // If the selected prefab is too strong, find a suitable one
                      {
                          List<GameObject> suitableEnemies = new List<GameObject>();                         //build a list of prefabs at or below average level
                          foreach (GameObject prefab in enemyPrefabs)
                          {
                              if (prefab.GetComponent<PTSoul>().level <= avgLevel)
                                  suitableEnemies.Add(prefab);
                          }
                          if (suitableEnemies.Count > 0)                                                    //pick a random suitable prefab if any exist
                              randomEnemyPrefab = suitableEnemies[Random.Range(0, suitableEnemies.Count)];
                      }

                      GameObject enemyObj = Instantiate(randomEnemyPrefab, 
                                                        spawnPosition, 
                                                        Quaternion.identity, 
                                                        transform);                                         //instantiate enemy at random position as child of PTManager
                      enemyObj.name = enemyObj.GetComponent<PTSoul>().Type + (i + 1);                       //set GameObject name
                      PTSoul enemySoul = enemyObj.GetComponent<PTSoul>();                                   //get the PTSoul component
                      if (enemySoul != null)
                      {
                          adversaries.Add(enemySoul);                                                       //add to adversaries list
                      }
                  }
              }
              
              hasFought = true;                                                                             //set hasFought to true for the day
              
              enemyMsg = "An enemy has appeared! There are now " + enemyCount + 
                                  " " + (adversaries.Count > 0 ? adversaries[0].Type : "enemy") + "s to fight!";                                //sets the enemy message
                if (enemyCount > 0)                                                                         //check if there are enemies to fight            
                {
                  canSleep = false;                                                                         //disable sleeping    
                }

                PTAdventureLog.Log(enemyMsg);                                                               //print enemy message

                UpdateUI();                                                                                 //update UI when enemies spawn
            }
            if (hasFought && enemyCount > 0)                                                           //check if the player has already fought for the day
            {
                PTAdventureLog.Log("You are already fighting an enemy!");                                   //log already fighting message
            }
            else if (hasFought && enemyCount <= 0)                                                               //check if the player has already fought and there are no enemies to fight
            {
                PTAdventureLog.Log("You have already fought for the day, " + 
                                   "there are no enemies to fight!");                                   //log already fought message
            }
      } 
    }

    public void OnAttack(InputAction.CallbackContext context)                                               //Method to handle Left Click
    { 
      if (gameOver) return;                                                                               //block input after game over
      if (context.performed && adversaries.Count > 0)                                                       //check if attack key is pressed and there are enemies to fight
      {
        if (partyMembers.Count > 0)                                                                         //check if there are party members alive to attack with
        {
          PTAdventureLog.Log("=== PARTY ATTACKS ===");

          Dictionary<PTSoul, PTSoul> killCredit = new Dictionary<PTSoul, PTSoul>();                         //dictionairy for killed enemies being mapped to the party member who landed the killing blow

          foreach (PTSoul attacker in partyMembers)                                                         //each party member attacks a random enemy
          {
              if (adversaries.Count > 0)                                                                    //check if there are still enemies to attack
              {
                  PTSoul target = adversaries[Random.Range(0, adversaries.Count)];                          //select random enemy to target
                  
                  int damage = attacker.DealDamage();                                                       //calculate damage from attacker
                  target.TakeDamage(damage);                                                                //apply damage to target

                  if (!target.isAlive && !killCredit.ContainsKey(target))                                   //if this attack killed the enemy and no kill credit exists yet
                  {
                      killCredit[target] = attacker;                                                        //record the attacker as the killer
                  }

                  PTAdventureLog.Log(attacker.Name + " attacks " +                                          //log attack message
                                     target.Type + " for " + 
                                     damage + " damage! " + 
                                     target.Type + " has " + 
                                     target.currentHP + "/" + 
                                     target.maxHP + " HP remaining.");
              }
          }
          
                                                                                                            // Remove dead enemies after all party attacks
          for (int i = adversaries.Count - 1; i >= 0; i--)                                                  //loop backwards through adversaries to safely remove
          {
              if (!adversaries[i].isAlive)                                                                  //check if enemy is dead
              {
                  PTSoul deadEnemy = adversaries[i];
                  ChangeGold(deadEnemy.goldReward);                                                         //add gold reward

                  if (killCredit.TryGetValue(deadEnemy, out PTSoul killer))                                 //check if a killer was recorded for this enemy
                  {
                      killer.GainXP(deadEnemy.accumulatedLifeXp);                                          //award XP only to the killer
                      PTAdventureLog.Log(killer.Name + " gained " + deadEnemy.accumulatedLifeXp + " XP for slaying " + deadEnemy.Type + "!"); //log XP gained message
                  }

                  Destroy(deadEnemy.gameObject);                                                            //destroy enemy GameObject
                  adversaries.RemoveAt(i);                                                                  //remove from list

                  PTAdventureLog.Log(deadEnemy.Type + " has been defeated! You found " +
                            deadEnemy.goldReward + " gold on the corpse.");                                 //log enemy defeated message with gold reward
              }
          }
          
          UpdateUI();                                                                                       //update UI after enemies are defeated
          
          // Check if all enemies are defeated
          if (adversaries.Count == 0)
          {
            ChangeGold(commission);                                                                         //add bonus commission for winning
            SetSunIntensity(0.1f);
            canSleep = true;                                                                                //enable sleeping
            enemyCount = 0;                                                                                 //reset enemy count
            
            PTAdventureLog.Log("You have defeated all the enemies! You can now sleep through the night.");  //log victory message
            PTAdventureLog.Log("You earned a commission of " + commission + 
                               " gold! You have " + gold + " gold in total.");                              //log gold earned message


            UpdateUI();                                                                                     //update UI after victory
            return;                                                                                         //exit function early since battle is over
          }
          
          if (adversaries.Count > 0 && partyMembers.Count > 0)                                              //check if there are still enemies and party members alive
          {
              PTAdventureLog.Log("=== ENEMIES COUNTER-ATTACK ===");

              Dictionary<PTSoul, PTSoul> enemyKillCredit = new Dictionary<PTSoul, PTSoul>();                //maps each killed party member to the enemy who landed the killing blow

              foreach (PTSoul enemyAttacker in adversaries)                                                 //for each enemy in the adversaries list, have them attack a random party member
              {
                  if (partyMembers.Count > 0)                                                               //check if there are still party members to attack
                  {
                      PTSoul partyTarget = partyMembers[Random.Range(0, partyMembers.Count)];               //select random party member to target
                      
                      int enemyDamage = enemyAttacker.DealDamage();                                         //calculate damage from enemy
                      partyTarget.TakeDamage(enemyDamage);                                                  //apply damage to party member

                      if (!partyTarget.isAlive && !enemyKillCredit.ContainsKey(partyTarget))                //if this attack killed the party member and no kill credit exists yet
                      {
                          enemyKillCredit[partyTarget] = enemyAttacker;                                     //record the enemy as the killer
                      }

                      PTAdventureLog.Log(enemyAttacker.Type + " attacks " + 
                                         partyTarget.Name + " for " + 
                                         enemyDamage + " damage! " + 
                                         partyTarget.Name + " has " + 
                                         partyTarget.currentHP + "/" + 
                                         partyTarget.maxHP + " HP remaining.");
                  }
              }
              
                                                                                                            // Remove dead party members after all enemy attacks
              for (int i = partyMembers.Count - 1; i >= 0; i--)                                             //loop backwards through party members to safely remove
              {
                  if (!partyMembers[i].isAlive)                                                             //check if party member is dead
                  {
                      PTSoul deadMember = partyMembers[i];

                      if (enemyKillCredit.TryGetValue(deadMember, out PTSoul enemyKiller))                  //check if a killer was recorded for this party member
                      {
                          enemyKiller.GainXP(deadMember.accumulatedLifeXp);                                 //award XP to the enemy who landed the killing blow
                          PTAdventureLog.Log(enemyKiller.Type + " gained " + 
                                             deadMember.accumulatedLifeXp + " XP for slaying " + 
                                             deadMember.Name + "!");                                        //log that the enemy gained xp for killing the party member
                      }

                      RemovePartyMember(deadMember);                                                        //remove dead party member
                      PTAdventureLog.Log(deadMember.Name + " has been slain!");                             //log death message
                  }
              }
              
              if (partyMembers.Count == 0)                                                                  //if all party members are dead
              {
                  PTAdventureLog.Log("You've been slaughtered by the enemy! Game Over.");                   //log game over message

                  gameOver = true;                                                                          //set game over flag — input stays active so 4 can reset
                  return;                                                                                   //exit the method
              }
              
              if (debugMode)                                                                                //if debug mode is enabled, print debug message
              {
              Debug.Log(adversaries.Count + " " + adversaries[0].Type + "s remaining.");                    //log remaining enemies
              }
          }
        }
      }
      else if (context.performed && adversaries.Count == 0)                                                 //check if attack key is pressed and there are no enemies to fight
      {
        PTAdventureLog.Log("There are no enemies to attack!");                                              //log no enemies message
      }
    }

    public void OnHeal(InputAction.CallbackContext context)                                                   //method to handle Q key press aka Healing
    {
      if (gameOver) return;                                                                               //block input after game over
      if (context.performed)
      {
        if (adversaries.Count > 0)                                                                          //if enemies are present, prevent healing
        {
          PTAdventureLog.Log("You cannot heal during battle!");
          return;
        }

        
        if (hasHealed)                                                                                      //if already healed today
        {
          PTAdventureLog.Log("You have already exhausted the local cleric, try again tomorrow.");
          return;
        }

        int healCost = partyMembers.Count * 10;                                                             // Calculate cost (10 gold per party member)

        if (gold < healCost)                                                                                //if the player cant afford heals
        {
          PTAdventureLog.Log("Not enough gold! You need " + healCost + 
                             " gold to heal the party, but you only have " + gold + " gold.");
          return;
        }

        ChangeGold(-healCost);
        int totalHealed = 0;
        foreach (PTSoul member in partyMembers)
        {
          int healed = member.Heal(50);
          totalHealed += healed;

          PTAdventureLog.Log(member.Name + " was healed for " + healed + " HP! (" + member.currentHP + "/" + member.maxHP + " HP)");
        }

        hasHealed = true;                                                                                   //set healing as used for the day


        PTAdventureLog.Log("The Party has been Healed! Healing recieved: " + totalHealed + " HP. Cost: " + healCost + " gold. Remaining gold: " + gold);


        UpdateUI();                                                                                         //call update UI after healing (gold changed)
      }
    }

    public void OnRecruit(InputAction.CallbackContext context)                                                   //Method to handle F key to recruit a new party member
    {
      if (gameOver) return;                                                                               //block input after game over
      if (context.performed)
      {
        if (adversaries.Count > 0)                                                                          //if adversaries list contains an adversary, prevent recruitment.
        {
          PTAdventureLog.Log("You are in Combat! Win the battle before recruiting new members!");           //log message to win battle before recruiting
          return;
        }

        if (partyMembers.Count >= maxPartyCount)                                                            //if the party is full, prevent recruitment
        {
          PTAdventureLog.Log("The party is full! You cannot recruit more members right now.");              //log party full message
          return;
        }

        if (characterPrefabs == null || characterPrefabs.Length == 0)                                       //if there are no prefabs or prefab list is empty or 0
        {
          PTAdventureLog.Log("No one is available to join the party!");                                     //log messgae, no one to recruit
          return;
        }

        int recruitCost = CalculateTotalDailyWages() / 2;                                                  //recruitment cost is half of total wages

        bool hasCoward = false;                                                                              //check if any current party member is cowardly
        foreach (PTSoul member in partyMembers)
        {
          if (member.isCowardly)
          {
            hasCoward = true;
            break;
          }
        }
        if (hasCoward)                                                                                      //if a cowardly soul exists, multiply cost by 1.5
        {
          recruitCost = Mathf.RoundToInt(recruitCost * 1.5f);
          PTAdventureLog.Log("Cowardly members in the party have increased recruitment costs!");            //log cowardly surcharge
        }

        if (gold < recruitCost)                                                                              //check if party can afford recruitment
        {
          PTAdventureLog.Log("Not enough gold to recruit! You need " + recruitCost +
                             " gold but only have " + gold + " gold.");                                    //log insufficient gold
          return;
        }

        ChangeGold(-recruitCost);                                                                            //deduct recruitment cost
        PTAdventureLog.Log("Recruitment cost: " + recruitCost + " gold. Remaining: " + gold + " gold."); //log recruitment cost

        GameObject randomPrefab = characterPrefabs[Random.Range(0, characterPrefabs.Length)];               // Select a random character prefab
        
        AddPartyMember(randomPrefab);                                                                       //call method, AddPartyMember
      }
    }

    public void OnRunAway(InputAction.CallbackContext context)                                                   //Method to handle R key press - Run from battle
    {
      if (gameOver) return;                                                                               //block input after game over
      if (context.performed)
      {
        if (adversaries.Count == 0)                                                                       //if not in combat, can't flee
        {
          PTAdventureLog.Log("There is no battle to run from!");                                          //log no battle message
          return;
        }

        int fleeCost = Mathf.Max(1, Mathf.FloorToInt(gold * 0.1f));                                       //10% of current gold, minimum 1
        ChangeGold(-fleeCost);                                                                             //deduct flee cost
        PTAdventureLog.Log(partyName + " fled from battle! It cost " + fleeCost + " gold to escape.");    //log flee cost

        foreach (PTSoul member in partyMembers)                                                           //mark all living party members as cowardly
        {
          if (member.isAlive)
          {
            member.isCowardly = true;                                                                     //mark as cowardly for fleeing
          }
        }
        PTAdventureLog.Log("The party has been marked as Cowardly for running from battle!");             //log cowardly mark

        for (int i = adversaries.Count - 1; i >= 0; i--)                                                  //destroy all enemies without gold reward
        {
          Destroy(adversaries[i].gameObject);                                                              //destroy enemy GameObject
        }
        adversaries.Clear();                                                                               //clear the adversaries list

        canSleep = true;                                                                                   //enable sleeping
        enemyCount = 0;                                                                                    //reset enemy count
        SetSunIntensity(0.1f);                                                                             //dim the sun

        PTAdventureLog.Log("You escaped the battle. No gold was looted and no commission was earned. You may now sleep but people in town are now wary of your cowardice!!"); //log post-flee summary
        UpdateUI();                                                                                        //update UI after fleeing
      }
    }

    public void OnReset(InputAction.CallbackContext context)                                                   //Method to handle Escape key press - reloads the active scene
    {
      if (context.performed)
        {
            PTAdventureLog.ClearLog();
            PTAdventureLog.Log("Restarting...");                                                            //log reset message
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);                               //reload the current scene, resetting all state
        }
    }

    public void On_1(InputAction.CallbackContext context)   //for blessing the Party
    {
        if (gameOver) return;                                                                               //block input after game over
        if (context.performed)
        {
            if (adversaries.Count > 0)                                                                          //if enemies are present, prevent healing
            {
                PTAdventureLog.Log("You are not near the cleric, Go back to Town!");
                return;
            }

        
            if (hasBeenBlessed)                                                                                      //if already healed today
            {
                PTAdventureLog.Log("You have already recieved the blessings of the cleric this day!");
                return;
            }

            int blessCost = partyMembers.Count * 100;                                                             // Calculate cost (100 gold per party member)

            if (gold < blessCost)                                                                                //if the player cant afford blessings
            {
                PTAdventureLog.Log("The Cleric scoffs at your wealth! Come back with " + blessCost + 
                             " gold to bless the party!!");
                return;
            }

            ChangeGold(-blessCost);
            foreach (PTSoul member in partyMembers)
            {
            member.Bless();
            }

            hasHealed = true;                                                                                   //set healing as used for the day


            PTAdventureLog.Log("The Party has been Blessed!" + blessCost + " gold. Remaining gold: " + gold);


            UpdateUI();                                                                                         //call update UI after healing (gold changed)
        }
    }

    public void On_2(InputAction.CallbackContext context)
    {
        return;
    }

    public void On_3(InputAction.CallbackContext context)
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
    #endregion
}