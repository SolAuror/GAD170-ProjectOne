using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PartyTaxes;

/// <summary>
/// PTManager partial — Encounter spawning, attack rounds, and flee logic.
/// </summary>
public partial class PTManager
{
    public void OnEncounter(InputAction.CallbackContext context)                                                    //Method to handle E key press, using new InputActions system
    {
        if (gameOver) return;                                                                               //block input after game over
        if (context.performed)
        {
            if (!hasFought)                                                                                 // Only allow one encounter per day
            {
              int extraSpawns = Mathf.Max(0, daysElapsed - 7);                                              //add +1 spawn for each day after day 7 (0 extra on days 1-7, +1 on day 8, +7 on day 14, etc.)
              int spawnMax = (partyCount * 3) + extraSpawns;                                                //maximum spawn count with linear scaling
              int spawnCount = Random.Range(partyCount, spawnMax);                                          //random number of enemies to spawn with linear daily scaling
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
              
              enemyMsg = "An enemy has appeared! There are now " + adversaries.Count + 
                                  " " + (adversaries.Count > 0 ? "<enemy>" + adversaries[0].Name + "</enemy>" : "enemy") + "s to fight!";                                //sets the enemy message
                if (adversaries.Count > 0)                                                                  //check if there are enemies to fight            
                {
                  canSleep = false;                                                                         //disable sleeping    
                }

                PTAdventureLog.Log(enemyMsg);                                                               //print enemy message

                UpdateUI();                                                                                 //update UI when enemies spawn
            }
            if (hasFought && adversaries.Count > 0)                                                    //check if the player has already fought for the day
            {
                PTAdventureLog.Log("You are already fighting an enemy!");                                   //log already fighting message
            }
            else if (hasFought && adversaries.Count <= 0)                                                        //check if the player has already fought and there are no enemies to fight
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

                  if (!target.isAlive && !killCredit.ContainsKey(target))                                   //if this attack killed the enemy and no kill credit exists yet
                  {
                    killCredit[target] = attacker;                                                        //record the attacker as the killer
                  }
                  int actualDamage = target.TakeDamage(damage) - target.defense;                            //apply damage to enemy and get actual damage dealt after defense
                  PTAdventureLog.Log(attacker.Name + " attacks " +                                          //log attack message
                                     "<enemy>" + target.Name + "</enemy>" + " for " + 
                                     actualDamage + " damage! " + 
                                     "<enemy>" + target.Name + "</enemy>" + " has " + 
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
                      PTAdventureLog.Log(killer.Name + " gained " + deadEnemy.accumulatedLifeXp + " XP for slaying " + "<enemy>" + deadEnemy.Name + "</enemy>" + "!"); //log XP gained message
                  }

                  Destroy(deadEnemy.gameObject);                                                            //destroy enemy GameObject
                  adversaries.RemoveAt(i);                                                                  //remove from list

                  PTAdventureLog.Log("<enemy>" + deadEnemy.Name + "</enemy>" + " has been defeated! You found " +
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
            
            PTAdventureLog.Log("You have defeated all the enemies! You can now sleep through the night.");  //log victory message
            PTAdventureLog.Log("You earned a commission of " + commission + 
                               " gold! You have " + gold + " gold in total.");                              //log gold earned message

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
                      int actualDamage = partyTarget.TakeDamage(enemyDamage) - partyTarget.defense;         //apply damage to party member

                      if (!partyTarget.isAlive && !enemyKillCredit.ContainsKey(partyTarget))                //if this attack killed the party member and no kill credit exists yet
                      {
                          enemyKillCredit[partyTarget] = enemyAttacker;                                     //record the enemy as the killer
                      }

                      PTAdventureLog.Log("<enemy>" + enemyAttacker.Name + "</enemy>" + " attacks " + 
                                         partyTarget.Name + " for " + 
                                         actualDamage + " damage! " + 
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
                          PTAdventureLog.Log("<enemy>" + enemyKiller.Name + "</enemy>" + " gained " + 
                                             deadMember.accumulatedLifeXp + " XP for slaying " + 
                                             deadMember.Name + "!");                                        //log that the enemy gained xp for killing the party member
                      }

                      RemovePartyMember(deadMember);                                                        //remove dead party member
                      PTAdventureLog.Log(deadMember.Name + " has been slain!");                             //log death message
                  }
              }
              
              if (partyMembers.Count == 0)                                                                  //if all party members are dead
              {
                  TriggerGameOver("You've been slaughtered by the enemy!");
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

        int fleeCost = Mathf.Max(1, Mathf.FloorToInt(gold * FLEE_COST_PERCENT));                          //10% of current gold, minimum 1
        ChangeGold(-fleeCost);                                                                             //deduct flee cost
        PTAdventureLog.Log("<partyname>" + partyName + "</partyname>" + " fled from battle!" + fleeCost + " gold was lost.");    //log flee cost

        foreach (PTSoul member in partyMembers)                                                           //mark all living party members as cowardly
        {
          if (member.isAlive && !member.markedByDeath)                                                      //marked by death characters cannot become cowardly
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
        SetSunIntensity(0.1f);                                                                             //dim the sun

        PTAdventureLog.Log("You escaped the battle. No gold was looted and no commission was earned. You may now sleep but people in town are now wary of your cowardice!!"); //log post-flee summary
        UpdateUI();                                                                                        //update UI after fleeing
      }
    }
}
