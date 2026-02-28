using System.Collections.Generic;                                                                             //System.Collections.Generic - for using lists
using UnityEngine;
using UnityEngine.InputSystem;
using PartyTaxes;                                                                                             //using the PartyTaxes namespace to access the PTSoul class

namespace PartyTaxes {
public class PTManager : MonoBehaviour, SimpleControls.IPlayerActions
{
  [Header("Character & Enemy Prefabs")]
  public GameObject[] characterPrefabs;                                                                     //array of character prefabs to instantiate for starting party
  public GameObject enemyPrefab;                                                                            //varaible for enemy prefabs to spawn
  public Transform enemySpawnPoint;                                                                         //transform for enemy spawn location

  [Header("Party & Combat")]
  public List<PTSoul> partyMembers = new List<PTSoul>();                                                        //list to store the party members, using the PTSoul class for an individuals information.
  public List<PTSoul> adversaries = new List<PTSoul>();                                                         //list to store the enemies, using the PTSoul class for an individuals information.
  public int enemyCount = 0;                                                                             //integer # of enemies to fight
  public int totalEnemies = 0;                                                                           //integer to store the total number of enemies for victory/gold calculation
  public int gold = 90;                                                                                    //integer # of gold     
  public int commision = 50;                                                                               //integer # gold bonus from surviving combat
  public int totalWages;                                                                              
  public int daysElapsed = 0;                                                                              //integer # of days elapsed       
  public int partyCount;                                                                             //integer # of party members
  public int maxPartyCount = 4;                                                                          //integer # of max party members
  public AudioClip BGMusic;                                                                                   //AudioClip for background music
  public AudioClip AtkSound;                                                                                   //AudioClip for attack sound effect
  public AudioClip SleepSound;                                                                                   //AudioClip for sleep sound effect


  public string partyName = "The Adventurers";                                                             //variable string for the party name

  public string dailyMsg;                                                                                  //variable string for the daily message
  public string enemyMsg;                                                                                  //variable string for the enemy message
  public string[] sleepMsgs = {
    "You sleep roughly through the night and wake up feeling restless.",
    "You sleep soundly through this night and wake up feeling refreshed.",
    "You don't sleep at all this night, you feel lethargic and worn out."
  };                                                                                                      // possible sleep messages

  public bool canSleep = true;                                                                            //bool to check if sleeping is allowed  
  public bool hasFought = false;                                                                          //bool to check if the player has fought

  private SimpleControls controls;                                                                         //reference to the input controls

    void Awake()
    {
        controls = new SimpleControls();
        controls.Player.SetCallbacks(this);
    }

    void Start()                                                                                        // Start, called once before the first update after the script is loaded
    {   
        // Create starting party members from prefabs
        if (characterPrefabs != null && characterPrefabs.Length > 0)
        {
            foreach (GameObject prefab in characterPrefabs)
            {
                AddPartyMember(prefab);                                                                             //add to party using the AddPartyMember function (which instantiates the prefab)
            }
        }

        partyCount = partyMembers.Count;                                                                //set the party count to the number of party members in the list
        dailyMsg = "Heil, " + partyName +                                                               //greet the party,
                   "! You have " + gold +                                                               //display the party gold
                   " gold and " + partyMembers.Count +                                                  //display the number of party members
                   " party members. Daily Wages are " + CalculateTotalDailyWages() +                    //display the total daily wages 
                   ". Press E to encounter an enemy, Left Click to Attack it or" +                      //display instructions for fighting enemies
                   " Spacebar to sleep through the night.";                                             //display instructions for sleeping
        daysElapsed++;                                                                                  //increment the day
        Debug.Log(dailyMsg);                                                                            //print the daily message
    }

    void Update()                                                                                                     //On Update
    {
        if (gold <= 0)                                                                                    //check if the party is broke and trying to sleep
        {
            Debug.Log("You have run out of gold to pay your bills and so your party turned on you! Game Over.");     //print game over message
            OnDisable();                                                                                              //disable the script
        }
    }

    void OnEnable() //method for enabling the input controls
    {
        controls.Player.Enable();
    }

    void OnDisable() //method for disabling the input controls
    {
        controls.Player.Disable();
    }

    void AddPartyMember(GameObject prefab)                                                                 //function to add a new party member to the party, takes a GameObject prefab as a parameter
    {
        if (partyMembers.Count < maxPartyCount)                                                            //check if the party is not full
        {
            if (prefab != null)
            {
                GameObject characterObj = Instantiate(prefab, transform);                                  //instantiate character as child of PTManager
                PTSoul soul = characterObj.GetComponent<PTSoul>();                                          //get the PTSoul component
                if (soul != null)
                {
                    partyMembers.Add(soul);                                                                 //add the new member to the party
                    partyCount = partyMembers.Count;                                                        //update the party count
                    Debug.Log(soul.Name + " has joined the party!");                                       //print message that a new member has joined
                }
            }
        }
        else
        {
            Debug.Log("The party is full! You cannot add more members to the party.");                    //print message that the party is full
        }
    }

    void RemovePartyMember(PTSoul member)                                                                   //function to remove a party member and destroy their GameObject
    {
        if (partyMembers.Contains(member))
        {
            partyMembers.Remove(member);                                                                    //remove from list
            partyCount = partyMembers.Count;                                                                //update party count
            Debug.Log(member.Name + " has left the party!");
            Destroy(member.gameObject);                                                                     //destroy the GameObject
        }
    }

    int CalculateTotalDailyWages()                                                                          //method to calculate total daily wages from all party members
    {
        totalWages = 0;                                                                                 //initialize total wages
        foreach (PTSoul member in partyMembers)                                                             //loop through all party members
        {
            totalWages += member.dailywages;                                                                //add each member's daily wages to total
        }
        return totalWages;                                                                                  //return total wages
    }

#region Input Actions
    public void OnSleep(InputAction.CallbackContext context)                                                    //function to handle spacebar press, using new InputActions system     
    {
        if (context.performed)
        {
            if (canSleep)                                                                                       //check if sleeping is allowed    
            {
                Debug.Log(sleepMsgs[Random.Range(0, sleepMsgs.Length)] + " Day: " + daysElapsed);               //print sleeping message
                daysElapsed++;                                                                                  //increment the day
                totalWages = CalculateTotalDailyWages();                                                    //calculate total daily wages from all party members
                gold -= totalWages;                                                                             //subtract total daily wages from gold
                hasFought = false;                                                                              //reset hasFought for the new day
                
                dailyMsg = "Heil, " + partyName +                                                               //greet the party,
                          "! You have " + gold +                                                                //display the party gold
                          " gold and " + partyMembers.Count +                                                         //display the number of party members
                          " party members. Daily Wages are " + totalWages +                                   //display the total daily wages 
                          ". Press E to encounter an enemy, Left Click to Attack it or" +                       //display instructions for fighting enemies
                          " Spacebar to sleep through the night.";                                              //display instructions for sleeping
                Debug.Log(dailyMsg);                                                                            //print the daily message
            }
            else
            {
                Debug.Log("You cannot sleep!");                                                                 //print enemy message
            }
        }
    }

    public void OnE(InputAction.CallbackContext context)                                                         //function to handle E key press, using new InputActions system
    {
        if (context.performed)
        {
            if (!hasFought)                                                                                      // Only allow one encounter per day
            {
              int spawnCount = Random.Range(partyCount, partyCount * 5);                                          //random number of enemies to spawn, between party count += * 5
              enemyCount += spawnCount;                                                                         //adds a random number between 4 and 15 to the enemy count  
              
              // Create enemy GameObjects from prefab
              if (enemyPrefab != null)
              {
                  Vector3 baseSpawnPosition = enemySpawnPoint != null ? enemySpawnPoint.position : transform.position;  //use spawn point if assigned, otherwise use PTManager position
                  for (int i = 0; i < spawnCount; i++)                                                                   //loop to add new enemies to the adversaries list based on the spawn count
                  {
                      Vector3 randomOffset = Random.insideUnitSphere * 3f;                                             //generate random position within 1 unit radius
                      randomOffset.y = 0;                                                                               //set y to 0 to keep enemies on the same horizontal plane
                      Vector3 spawnPosition = baseSpawnPosition + randomOffset;                                        //calculate spawn position with offset
                      GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);  //instantiate enemy at random position as child of PTManager
                      enemyObj.name = enemyObj.GetComponent<PTSoul>().Type + (i + 1);                                                              //set GameObject name
                      PTSoul enemySoul = enemyObj.GetComponent<PTSoul>();                                              //get the PTSoul component
                      if (enemySoul != null)
                      {
                          adversaries.Add(enemySoul);                                                                   //add to adversaries list
                      }
                  }
              }
              
              hasFought = true;                                                                                //set hasFought to true for the day
              
              enemyMsg = "An enemy has appeared! There are now " + enemyCount + 
                                  " " + adversaries[0].Type + "s to fight!";                                               //sets the enemy message
                if (enemyCount > 0)                                                                              //check if there are enemies to fight            
                {
                  canSleep = false;                                                                              //disable sleeping    
                }

                Debug.Log(enemyMsg);                                                                             //print enemy message
                totalEnemies = enemyCount;                                                                       // Store the total number of enemies for victory/gold calculation
            }
            else if (hasFought && enemyCount > 0)                                                                //check if the player has already fought for the day
            {
                Debug.Log("You are already fighting an enemy!");                                                 //print already fighting message
            }
            if (hasFought && enemyCount <= 0)                                                                    //check if the player has already fought and there are no enemies to fight
            {
                Debug.Log("You have already fought for the day, and there are no enemies to fight!");            //print already fought message
            }
      } 
    }

    public void OnAttack(InputAction.CallbackContext context)                                                     //funtion to handle attack key press
    { 
      if (context.performed && adversaries.Count > 0)                                                             //check if attack key is pressed and there are enemies to fight
      {
        // Random party member attacks random enemy
        if (partyMembers.Count > 0)
        {
          PTSoul attacker = partyMembers[Random.Range(0, partyMembers.Count)];                                   //select random party member to attack
          PTSoul target = adversaries[Random.Range(0, adversaries.Count)];                                       //select random enemy to target
          
          int damage = attacker.DealDamage();                                                                    //calculate damage from attacker
          target.TakeDamage(damage);                                                                             //apply damage to target
          
          Debug.Log(attacker.Name + " attacks " + target.Type + " for " + damage + " damage! " + target.Type + " has " + target.currentHP + "/" + target.maxHP + " HP remaining.");
          
          // Remove dead enemies
          for (int i = adversaries.Count - 1; i >= 0; i--)                                                       //loop backwards through adversaries to safely remove
          {
              if (!adversaries[i].isAlive)                                                                       //check if enemy is dead
              {
                  PTSoul deadEnemy = adversaries[i];
                  gold += deadEnemy.goldReward;                                                                  //add gold reward
                  Destroy(deadEnemy.gameObject);                                                                 //destroy enemy GameObject
                  adversaries.RemoveAt(i);                                                                       //remove from list

                  Debug.Log(deadEnemy.Type + " has been defeated! you found " + deadEnemy.goldReward + " gold on the corpse.");                             //print enemy defeated message with gold reward
              }
          }
          
          // Enemy counter-attack
          if (adversaries.Count > 0 && partyMembers.Count > 0)                                                   //check if there are still enemies and party members alive
          {
              PTSoul enemyAttacker = adversaries[Random.Range(0, adversaries.Count)];                           //select random enemy to attack
              PTSoul partyTarget = partyMembers[Random.Range(0, partyMembers.Count)];                           //select random party member to target
              
              int enemyDamage = enemyAttacker.DealDamage();                                                      //calculate damage from enemy
              partyTarget.TakeDamage(enemyDamage);                                                               //apply damage to party member
              
              Debug.Log(enemyAttacker.Type + " attacks " + partyTarget.Name + " for " + enemyDamage + " damage! " + partyTarget.Name + " has " + partyTarget.currentHP + "/" + partyTarget.maxHP + " HP remaining.");
              
              // Remove dead party members
              for (int i = partyMembers.Count - 1; i >= 0; i--)                                                 //loop backwards through party members to safely remove
              {
                  if (!partyMembers[i].isAlive)                                                                 //check if party member is dead
                  {
                      PTSoul deadMember = partyMembers[i];
                      RemovePartyMember(deadMember);                                                            //remove dead party member
                      Debug.Log(deadMember.Name + " has been slain!");                                          //print death message
                  }
              }
              
              // Check if all party members are dead
              if (partyMembers.Count == 0)
              {
                  Debug.Log("Your entire party has been wiped out! Game Over.");                                //print game over message
                  OnDisable();                                                                                  //disable the script
                  return;                                                                                       //exit the function
              }
          }
          
          // Check if all enemies are defeated
          if (adversaries.Count == 0)
          {
            gold += commision;                                                                                   //add bonus commission for winning
            canSleep = true;                                                                                     //enable sleeping
            enemyCount = 0;                                                                                      //reset enemy count
            totalEnemies = 0;                                                                                    //reset total enemies

            Debug.Log("You have defeated all the enemies! You can now sleep through the night.");                //print victory message
            Debug.Log("You earned a commision of " + commision + " gold! You have " + gold + " gold in total."); //print gold earned message
          }
          else
          {
            Debug.Log(adversaries.Count + " " + adversaries[0].Type + "s remaining.");                           //print remaining enemies
          }
        }
      }
      else if (context.performed && adversaries.Count == 0)                                                      //check if attack key is pressed and there are no enemies to fight
      {
        Debug.Log("There are no enemies to attack!");                                                            //print no enemies message
      }
    }

    public void On_1(InputAction.CallbackContext context)                                                         //function to handle 1 key press
    {
      if (context.performed)
      {
        return;
      }
    }

    public void On_2(InputAction.CallbackContext context)                                                         //function to handle 2 key press
    {
      if (context.performed)
      {
        return;
      }
    }

    public void On_3(InputAction.CallbackContext context)                                                         //function to handle 3 key press
    {
      if (context.performed)
      {
        return;
      }
    }

    public void On_4(InputAction.CallbackContext context)                                                         //function to handle 4 key press
    {
      if (context.performed)
      {
        return;
      }
    }
    #endregion
}
}