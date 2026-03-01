using System.Collections.Generic;                                                                            //System.Collections.Generic - for using lists
using UnityEngine;                                                                                           //using UnityEngine for MonoBehaviour and other Unity features
using UnityEngine.InputSystem;                                                                               //using UnityEngine.InputSystem for the new Input System
using TMPro;                                                                                                 //using TMPro for TextMeshPro references
using UnityEngine.UI;                                                                                        //using UnityEngine.UI for Slider references
using PartyTaxes;                                                                                            //using the PartyTaxes namespace to access the PTSoul class

public class PTManager : MonoBehaviour, SimpleControls.IPlayerActions                                        //inherit from MonoBehaviour and implement the IPlayerActions interface from the SimpleControls input actions
{
#region Variables
  public bool debugMode = false;                                                                             //bool for debug toggling 

  [Header("Character & Enemy Prefabs")]                                                                      //header for character prefabs in theinspector
  public GameObject[] characterPrefabs;                                                                      //array of character prefabs to instantiate for starting party
  public GameObject enemyPrefab;                                                                             //varaible for enemy prefabs to spawn
  public Transform partySpawnPoint;                                                                          //transform for party spawn location
  public Transform enemySpawnPoint;                                                                          //transform for enemy spawn location

  [Header("Party & Combat")]                                                                                 //header for party and combat settings in the inspector                           
  public List<PTSoul> partyMembers = new List<PTSoul>();                                                     //list to store the party members, using the PTSoul class for an individuals information.
  public List<PTSoul> adversaries = new List<PTSoul>();                                                      //list to store the enemies, using the PTSoul class for an individuals information.
  public int enemyCount = 0;                                                                                 //integer # of enemies to fight
  public int totalEnemies = 0;                                                                               //integer to store the total number of enemies for victory/gold calculation
  public int gold = 90;                                                                                      //integer # of gold     
  public int commision = 50;                                                                                 //integer # gold bonus from surviving combat
  public int totalWages;                                                                                     //integer # for total calculated wages, party member count x character dailywages.
  public int daysElapsed = 0;                                                                                //integer # of days elapsed       
  public int partyCount;                                                                                     //integer # of party members
  public int maxPartyCount = 4;                                                                              //integer # of max party members
  public AudioClip BGMusic;                                                                                  //AudioClip for background music
  public AudioClip goldSound;                                                                                //AudioClip for gold sound effect
  public GameObject Sun;                                                                                     //Reference to the sun onject for changing instensity when sleeping
  [SerializeField] public TextMeshProUGUI battleText;                                                        //reference to TextMeshPro level text obj
  [SerializeField] public TextMeshProUGUI goldText;                                                          //reference to TextMeshPro gold text obj


  public string partyName = "The Adventurers";                                                               //variable string for the party name

  public string dailyMsg;                                                                                    //variable string for the daily message
  public string enemyMsg;                                                                                    //variable string for the enemy message
  public string[] sleepMsgs = {
    "You sleep roughly through the night and wake up feeling restless.",
    "You sleep soundly through this night and wake up feeling refreshed.",
    "You don't sleep at all this night, you feel lethargic and worn out."
  };                                                                                                         // possible sleep messages

  public bool canSleep = true;                                                                               //bool to check if sleeping is allowed  
  public bool hasFought = false;                                                                             //bool to check if the player has fought
  public bool hasHealed = false;   
  

  private SimpleControls controls;                                                                           //reference to the input controls
  private AudioSource audioSource;                                                                           //reference to the AudioSource component
#endregion
#region Initilization and Update
    void Awake()                                                                                             //On Awake   
    {
        controls = new SimpleControls();                                                                     //initialize controls 
        controls.Player.SetCallbacks(this);
    }

    void Start()                                                                                             // Start, called once before the first update after the script is loaded
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
            Debug.Log("Debug Mode Enabled: Starting with extra gold and party members.");
        }
    }

    void Update()                                                                                           //On Update
    {
        if (gold <= 0)                                                                                      //check if the party is broke and trying to sleep
        {
            if (debugMode)                                                                                  //if debug mode is enabled, print debug message
            {
                Debug.Log("You have run out of gold to pay your bills" +
                          " and so your party turned on you! Game Over.");
            }

            PTAdventureLog.Log("You have run out of gold to pay your bills" +
                               " and so your party turned on you! Game Over.");
            OnDisable();                                                                                    //disable the script
        }
    }

    void OnEnable()                                                                                         //method for enabling the input controls
    {
        controls.Player.Enable();
    }

    void OnDisable()                                                                                        //method for disabling the input controls
    {
        controls.Player.Disable();
    }
#endregion

#region Party Taxes Systems
    void AddPartyMember(GameObject prefab)                                                                  //function to add a new party member to the party, takes a GameObject prefab as a parameter
    {
        if (partyMembers.Count < maxPartyCount)                                                             //check if the party is not full
        {
            if (prefab != null)
            {
                Vector3 baseSpawnPosition = partySpawnPoint != null ?
                                            partySpawnPoint.position : 
                                            transform.position;                                             //use spawn point if assigned, otherwise use PTManager position
                float spacing = 1.2f;                                                                       //spacing between party members along x-axis
                float minDistance = 1f;                                                                     //minimum distance between party members
                int maxAttempts = 30;                                                                       //maximum attempts to find valid position
                
                Vector3 spawnPosition = baseSpawnPosition;
                bool validPosition = false;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++)                                     // Try to find a non-overlapping position
                {
                    Vector3 offset = new Vector3(partyMembers.Count * spacing, 0, 0);                       //calculate offset based on current party size
                    spawnPosition = baseSpawnPosition + offset;                                             //calculate spawn position with offset
                    
                                                                                                            
                    validPosition = true;                                                                      
                    foreach (PTSoul existingMember in partyMembers)                                         // Check distance from all existing party members with a for each loop
                    {
                        if (Vector3.Distance(spawnPosition, existingMember.transform.position) < minDistance)
                        {
                            validPosition = false;
                            spacing += 0.5f;                                                                //increase spacing if overlap detected
                            break;
                        }
                    }
                    
                    if (validPosition || partyMembers.Count == 0)                                           //valid position found or first member
                        break;
                }
                
                GameObject characterObj = Instantiate(prefab, 
                                                      spawnPosition, 
                                                      Quaternion.identity, 
                                                      transform);                                           //create an instance of the character at the designated spawn position as child of PTManager
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
        else                                                                                                //if there is no PTsoul component
        {
            if (debugMode)                                                                                  //if debug mode is enabled, print debug message
            {
                Debug.Log("The party is full! You cannot add more members to the party.");                    
            }
            
            PTAdventureLog.Log("The party is full! You cannot add more members to the party.");             //log message that the party is full
        }
    }

    void RemovePartyMember(PTSoul member)                                                                   //function to remove a party member and destroy their GameObject
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

    int CalculateTotalDailyWages()                                                                          //method to calculate total daily wages from all party members
    {
        totalWages = 0;                                                                                     //initialize total wages
        foreach (PTSoul member in partyMembers)                                                             //loop through all party members
        {
            totalWages += member.dailywages;                                                                //add each member's daily wages to total
        }
        return totalWages;                                                                                  //return total wages
    }

    void UpdateUI()                                                                                         //method to update UI elements
    {
        // Update gold text
        if (goldText != null)
        {
            goldText.text = "Gold: " + gold;                                                                //display current gold
        }
        
        // Update battle text
        if (battleText != null)
        {
            if (adversaries.Count > 0)                                                                      //if in battle
            {
                battleText.text = partyName + " is in battle with " + adversaries.Count + " enemies";           //display enemy count
            }
            else                                                                                            //if not in battle
            {
                battleText.text = partyName + " is in Town";                                                   //display in town message
            }
        }
    }

    void ChangeGold(int amount)                                                                             //method to change gold amount, takes an integer amount as a parameter (positive or negative)
    {
        if (amount == 0)
        {
            return;
        }

        gold += amount;

        if (audioSource != null && goldSound != null)
        {
            audioSource.PlayOneShot(goldSound);                                                             //play gold sound effect when gold changes
        }
    }

    void SetSunIntensity(float intensity)                                                                   //method to change the intensity of the sun on sleep 
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
    public void OnSleep(InputAction.CallbackContext context)                                                //function to handle spacebar press, using new InputActions system     
    {
        if (context.performed)
        {
            if (canSleep)                                                                                   //check if sleeping is allowed    
            {
                PTAdventureLog.Log(sleepMsgs[Random.Range(0, sleepMsgs.Length)] + " Day: " + daysElapsed);  //log sleeping message
                daysElapsed++;                                                                              //increment the day
                totalWages = CalculateTotalDailyWages();                                                    //calculate total daily wages from all party members
                ChangeGold(-totalWages);                                                                    //subtract total daily wages from gold
                SetSunIntensity(2f);
                hasFought = false;                                                                          //reset hasFought for the new day
                hasHealed = false;                                                                          //reset hasHealed for the new day
                
                dailyMsg = "Heil, " + partyName +                                                           //greet the party,
                          "! You have " + gold +                                                            //display the party gold
                          " gold and " + partyMembers.Count +                                               //display the number of party members
                          " party members. Daily Wages are " + totalWages +                                 //display the total daily wages 
                          ". Press E to encounter an enemy, Left Click to Attack it or" +                   //display instructions for fighting enemies
                          " Spacebar to sleep through the night.";                                          //display instructions for sleeping
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

    public void OnE(InputAction.CallbackContext context)                                                    //function to handle E key press, using new InputActions system
    {
        if (context.performed)
        {
            if (!hasFought)                                                                                 // Only allow one encounter per day
            {
              int spawnCount = Random.Range(partyCount, partyCount * 3);                                    //random number of enemies to spawn, between party count += * 5
              enemyCount += spawnCount;                                                                     //adds a random number between 4 and 15 to the enemy count  
              
              if (enemyPrefab != null)                                                                      //Create enemy GameObjects from prefab after a null check
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
                      
                      GameObject enemyObj = Instantiate(enemyPrefab, 
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
                                  " " + adversaries[0].Type + "s to fight!";                                //sets the enemy message
                if (enemyCount > 0)                                                                         //check if there are enemies to fight            
                {
                  canSleep = false;                                                                         //disable sleeping    
                }

                PTAdventureLog.Log(enemyMsg);                                                               //print enemy message
                totalEnemies = enemyCount;                                                                  // Store the total number of enemies for victory/gold calculation
                UpdateUI();                                                                                 //update UI when enemies spawn
            }
            else if (hasFought && enemyCount > 0)                                                           //check if the player has already fought for the day
            {
                PTAdventureLog.Log("You are already fighting an enemy!");                                   //log already fighting message
            }
            if (hasFought && enemyCount <= 0)                                                               //check if the player has already fought and there are no enemies to fight
            {
                PTAdventureLog.Log("You have already fought for the day," + 
                                   "and there are no enemies to fight!");                                   //log already fought message
            }
      } 
    }

    public void OnAttack(InputAction.CallbackContext context)                                               //funtion to handle attack key press
    { 
      if (context.performed && adversaries.Count > 0)                                                       //check if attack key is pressed and there are enemies to fight
      {
        if (partyMembers.Count > 0)                                                                         //check if there are party members alive to attack with
        {
          PTAdventureLog.Log("=== PARTY ATTACKS ===");
          

          foreach (PTSoul attacker in partyMembers)                                                         // Each party member attacks a random enemy
          {
              if (adversaries.Count > 0)                                                                    //check if there are still enemies to attack
              {
                  PTSoul target = adversaries[Random.Range(0, adversaries.Count)];                          //select random enemy to target
                  
                  int damage = attacker.DealDamage();                                                       //calculate damage from attacker
                  target.TakeDamage(damage);                                                                //apply damage to target

                  PTAdventureLog.Log(attacker.Name + " attacks " +                                          //log attack messages
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
                  Destroy(deadEnemy.gameObject);                                                            //destroy enemy GameObject
                  adversaries.RemoveAt(i);                                                                  //remove from list

                  PTAdventureLog.Log(deadEnemy.Type + " has been defeated! you found " +
                            deadEnemy.goldReward + " gold on the corpse.");                                 //log enemy defeated message with gold reward
              }
          }
          
          UpdateUI();                                                                                       //update UI after enemies are defeated
          
          // Check if all enemies are defeated
          if (adversaries.Count == 0)
          {
            ChangeGold(commision);                                                                          //add bonus commission for winning
            SetSunIntensity(0.1f);
            canSleep = true;                                                                                //enable sleeping
            enemyCount = 0;                                                                                 //reset enemy count
            totalEnemies = 0;                                                                               //reset total enemies


            PTAdventureLog.Log("You have defeated all the enemies! You can now sleep through the night.");  //log victory message
            PTAdventureLog.Log("You earned a commision of " + commision + 
                               " gold! You have " + gold + " gold in total.");                              //log gold earned message


            UpdateUI();                                                                                     //update UI after victory
            return;                                                                                         //exit function early since battle is over
          }
          
          if (adversaries.Count > 0 && partyMembers.Count > 0)                                              //check if there are still enemies and party members alive
          {
              PTAdventureLog.Log("=== ENEMIES COUNTER-ATTACK ===");
              
              foreach (PTSoul enemyAttacker in adversaries)                                                 //for each enemy in the adversaries list, have them attack a random party member
              {
                  if (partyMembers.Count > 0)                                                               //check if there are still party members to attack
                  {
                      PTSoul partyTarget = partyMembers[Random.Range(0, partyMembers.Count)];               //select random party member to target
                      
                      int enemyDamage = enemyAttacker.DealDamage();                                         //calculate damage from enemy
                      partyTarget.TakeDamage(enemyDamage);                                                  //apply damage to party member

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
                      RemovePartyMember(deadMember);                                                        //remove dead party member


                      PTAdventureLog.Log(deadMember.Name + " has been slain!");                             //log death message
                  }
              }
              
              if (partyMembers.Count == 0)                                                                  //if all party members are dead
              {
                  PTAdventureLog.Log("You've been slaughtered by the enemy! Game Over.");                   //log game over message

                  
                  OnDisable();                                                                              //disable the script
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

    public void On_1(InputAction.CallbackContext context)                                                   //method to handle 1 key press aka Healing
    {
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

        hasHealed = true;                                                                                   //mark healing as used for the day


        PTAdventureLog.Log("The Party has been Blessed! Healing recieved: " + totalHealed + " HP. Cost: " + healCost + " gold. Remaining gold: " + gold);


        UpdateUI();                                                                                         //update UI after healing (gold changed)
      }
    }

    public void On_2(InputAction.CallbackContext context)                                                   //method to pressing the 2 key to recruit a new party member
    {
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

        GameObject randomPrefab = characterPrefabs[Random.Range(0, characterPrefabs.Length)];               // Select a random character prefab
        
        AddPartyMember(randomPrefab);                                                                       //call method, AddPartyMember
      }
    }

    public void On_3(InputAction.CallbackContext context)                                                   //function to handle 3 key press
    {
      if (context.performed)
      {
        return;
      }
    }

    public void On_4(InputAction.CallbackContext context)                                                   //function to handle 4 key press
    {
      if (context.performed)
      {
        PTAdventureLog adventureLog = FindFirstObjectByType<PTAdventureLog>();
        if (adventureLog != null)
        {
          adventureLog.ToggleFocus();
        }
      }
    }
    #endregion
}