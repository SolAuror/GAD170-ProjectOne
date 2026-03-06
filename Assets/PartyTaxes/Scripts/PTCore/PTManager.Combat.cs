using System.Collections.Generic;                                                       //for using lists and dictionaries to manage party members, enemies, and kill credit
using System.Linq;                                                                      //for using LINQ, which makes sorting and filtering lists much easier and cleaner
using UnityEngine;
using UnityEngine.InputSystem;
using PartyTaxes;


/// PTManager partial — Combat Part: Encounter spawning, attack rounds, and flee logic.
public partial class PTManager 
{
  // ─── Kill tracking ───
  private Dictionary<PTSoul, PTSoul> killCredit      = new Dictionary<PTSoul, PTSoul>();  // which party member landed each killing blow
  private Dictionary<PTSoul, PTSoul> enemyKillCredit = new Dictionary<PTSoul, PTSoul>();  // which enemy landed each killing blow

  // ─── Turn queue state ───
  private List<PTSoul> partyTurnQueue    = new List<PTSoul>();                            // party members sorted by atrSense for the current round
  private int          partyTurnIndex    = 0;                                             // current position in the party queue
  private bool         enemyGoesFirst    = false;                                         // true when enemies have higher avg atrSense
  private bool         awaitingPartyInput = false;                                        // true when waiting for the player to press Attack
  private PTSoul       activeActor       = null;                                          // the party member whose turn it currently is


  public void ChangeXP(int xpAmount, string source = null)
  {
    if (partyMembers.Count > 0)
    {
      int xpShare = Mathf.FloorToInt((float)xpAmount / partyMembers.Count);
      foreach (PTSoul member in partyMembers)
        {
            if (member.isAlive)
            {
            member.GainXP(xpShare);
            if (!string.IsNullOrEmpty(source))
            {
                PTAdventureLog.Log(member.Name + " gained " + xpShare + " XP!");
            }
        }
      }
    }
  }

    public void OnEncounter(InputAction.CallbackContext context)                                            //Method to handle E key press, using new InputActions system
    {
        if (gameOver) return;                                                                               //block input after game over
        if (context.performed)
        {
            if (!hasFought)                                                                                 // Only allow one encounter per day
            {
              int extraSpawns = Mathf.Max(0, daysElapsed - 7);                                              //add +1 spawn for each day after day 7 (0 extra on days 1-7, +1 on day 8, +7 on day 14, etc.)
              int spawnMax = (partyCount * 2) + extraSpawns;                                                //maximum spawn count with linear scaling
              int spawnCount = Random.Range(partyCount, spawnMax);                                          //random number of enemies to spawn with linear daily scaling
              if (soulGen != null && soulGen.enemyBasePrefab != null)
              {
                  Vector3 baseSpawnPosition = enemySpawnPoint != null ?
                                              enemySpawnPoint.position :
                                              transform.position;                                           //use spawn point if assigned, otherwise use PTManager position
                  float minDistance = 1.5f;                                                                 //minimum distance between enemies
                  int maxAttempts = 30;                                                                     //maximum attempts to find valid position
                  int partyLevel  = GetAveragePartyLevel();                                                 //current average party level for type selection

                  for (int i = 0; i < spawnCount; i++)
                  {
                      Vector3 spawnPosition = baseSpawnPosition;

                      for (int attempt = 0; attempt < maxAttempts; attempt++)                               //try to find a non-overlapping position
                      {
                          Vector3 randomOffset = Random.insideUnitSphere * 3f;
                          randomOffset.y = 0;
                          spawnPosition = baseSpawnPosition + randomOffset;

                          bool validPosition = true;
                          foreach (PTSoul existingEnemy in adversaries)
                          {
                              if (Vector3.Distance(spawnPosition, existingEnemy.transform.position) < minDistance)
                              {
                                  validPosition = false;
                                  break;
                              }
                          }
                          if (validPosition) break;
                      }

                      GameObject enemyObj = soulGen.SpawnEnemy(transform, spawnPosition, partyLevel);       //delegate all type selection and stat generation to PTSoulGen
                      if (enemyObj != null)
                      {
                          PTSoul enemySoul = enemyObj.GetComponent<PTSoul>();
                          if (enemySoul != null)
                              adversaries.Add(enemySoul);                                                   //add to adversaries list
                      }
                  }
              }
              else if (enemyPrefabs != null && enemyPrefabs.Length > 0)                                     //legacy fallback: use assigned prefab array if no soulGen
              {
                  Vector3 baseSpawnPosition = enemySpawnPoint != null ?
                                              enemySpawnPoint.position :
                                              transform.position;
                  float minDistance = 1.5f;
                  int maxAttempts = 30;

                  for (int i = 0; i < spawnCount; i++)
                  {
                      Vector3 spawnPosition = baseSpawnPosition;
                      bool validPosition = false;

                      for (int attempt = 0; attempt < maxAttempts; attempt++)
                      {
                          Vector3 randomOffset = Random.insideUnitSphere * 3f;
                          randomOffset.y = 0;
                          spawnPosition = baseSpawnPosition + randomOffset;

                          validPosition = true;
                          foreach (PTSoul existingEnemy in adversaries)
                          {
                              if (Vector3.Distance(spawnPosition, existingEnemy.transform.position) < minDistance)
                              {
                                  validPosition = false;
                                  break;
                              }
                          }
                          if (validPosition) break;
                      }

                      GameObject randomEnemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                      int avgLevel = GetAveragePartyLevel();
                      if (randomEnemyPrefab.GetComponent<PTSoul>().level > avgLevel)
                      {
                          System.Collections.Generic.List<GameObject> suitableEnemies = new System.Collections.Generic.List<GameObject>();
                          foreach (GameObject prefab in enemyPrefabs)
                              if (prefab.GetComponent<PTSoul>().level <= avgLevel)
                                  suitableEnemies.Add(prefab);
                          if (suitableEnemies.Count > 0)
                              randomEnemyPrefab = suitableEnemies[Random.Range(0, suitableEnemies.Count)];
                      }

                      GameObject enemyObj = Instantiate(randomEnemyPrefab, spawnPosition, Quaternion.identity, transform);
                      enemyObj.name = enemyObj.GetComponent<PTSoul>().Type + (i + 1);
                      PTSoul enemySoul = enemyObj.GetComponent<PTSoul>();
                      if (enemySoul != null)
                          adversaries.Add(enemySoul);
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
                StartCombat();                                                                              //initialise turn queue and begin first turn
            }
            else if (hasFought && adversaries.Count > 0)                                               //check if the player has already fought for the day
            {
                PTAdventureLog.Log("You are already fighting an enemy!");                                   //log already fighting message
            }
            else if (hasFought && adversaries.Count <= 0)                                              //check if the player has already fought and there are no enemies to fight
            {
                PTAdventureLog.Log("You have already fought for the day, " + 
                                   "there are no enemies to fight!");                                   //log already fought message
            }
      } 
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TURN QUEUE SYSTEM
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when an encounter starts. Builds the first round and begins combat.
    /// </summary>
    private void StartCombat()
    {
        killCredit.Clear();
        enemyKillCredit.Clear();
        awaitingPartyInput = false;
        activeActor = null;
        PTAdventureLog.Log("=== COMBAT BEGINS ===");
        StartRound();
    }

    /// <summary>
    /// Resets all combat state. Call when combat ends for any reason.
    /// </summary>
    private void ResetCombatState()
    {
        partyTurnQueue.Clear();
        partyTurnIndex    = 0;
        awaitingPartyInput = false;
        activeActor       = null;
        killCredit.Clear();
        enemyKillCredit.Clear();
    }

    /// <summary>
    /// Begins a new round: builds sorted phase queues, determines phase order by
    /// average atrSense, then either runs the enemy phase first (auto) before
    /// handing control to the party, or goes straight to the party phase.
    /// </summary>
    private void StartRound()
    {
        killCredit.Clear();
        enemyKillCredit.Clear();

        // Build party turn order (highest sense first)
        partyTurnQueue = partyMembers.Where(m => m.isAlive)
                                     .OrderByDescending(m => m.atrSense)
                                     .ToList();
        partyTurnIndex = 0;

        // Determine phase order by average atrSense
        float partyAvg  = partyMembers.Count  > 0 ? (float)partyMembers.Sum(m => m.atrSense)  / partyMembers.Count  : 0f;
        float enemyAvg  = adversaries.Count   > 0 ? (float)adversaries.Sum(e => e.atrSense)   / adversaries.Count   : 0f;
        enemyGoesFirst  = enemyAvg > partyAvg;                       // party wins on tie

        if (debugMode)
            Debug.Log("Round start — Party avg Sense: " + partyAvg + " | Enemy avg Sense: " + enemyAvg +
                      " → " + (enemyGoesFirst ? "Enemies" : "Party") + " go first.");

        if (enemyGoesFirst)
        {
            PTAdventureLog.Log("=== ENEMY PHASE ===");
            bool allPartyDead = RunEnemyPhase();
            UpdateUI();
            if (allPartyDead) { ResetCombatState(); TriggerGameOver("You've been slaughtered by the enemy!"); return; }
        }

        // Begin party phase (pauses for input on each member)
        PTAdventureLog.Log("=== PARTY PHASE ===");
        AdvancePartyTurn();
    }

    /// <summary>
    /// Advances to the next living party member's turn, pausing for player input.
    /// When the party queue is exhausted, runs the enemy phase (if not already run)
    /// then starts a new round.
    /// </summary>
    private void AdvancePartyTurn()
    {
        // Find next living party member
        while (partyTurnIndex < partyTurnQueue.Count)
        {
            PTSoul actor = partyTurnQueue[partyTurnIndex];
            partyTurnIndex++;

            if (!actor.isAlive) continue;                            // skip anyone who died this round
            if (adversaries.Count == 0) break;                       // all enemies dead, skip remaining

            // Pause and wait for player input
            activeActor = actor;
            awaitingPartyInput = true;
            PTAdventureLog.Log(">>> It is " + actor.Name + "'s turn! Press Attack.");
            UpdateUI();
            return;
        }

        // ── Party phase complete ───────────────────────────────────
        if (adversaries.Count == 0) { OnAllEnemiesDefeated(); return; }

        if (!enemyGoesFirst)                                         // enemies haven't run yet this round
        {
            PTAdventureLog.Log("=== ENEMY PHASE ===");
            bool allPartyDead = RunEnemyPhase();
            UpdateUI();
            if (allPartyDead) { ResetCombatState(); TriggerGameOver("You've been slaughtered by the enemy!"); return; }
        }

        if (adversaries.Count == 0) { OnAllEnemiesDefeated(); return; }
        if (partyMembers.Count == 0) { ResetCombatState(); TriggerGameOver("You've been slaughtered by the enemy!"); return; }

        // ── Start the next round ──────────────────────────────────
        PTAdventureLog.Log("─── New Round ───");
        StartRound();
    }

    /// <summary>
    /// Runs all living enemies in atrSense order automatically.
    /// Returns true if all party members are dead.
    /// </summary>
    private bool RunEnemyPhase()
    {
        List<PTSoul> enemyOrder = adversaries.Where(e => e.isAlive)
                                             .OrderByDescending(e => e.atrSense)
                                             .ToList();
        foreach (PTSoul enemy in enemyOrder)
        {
            if (!enemy.isAlive)    continue;                         // may have died during this phase
            if (partyMembers.Count == 0) break;

            bool allPartyDead = ProcessEnemyAttack(enemy);
            if (allPartyDead) return true;
        }
        return partyMembers.Count == 0;
    }

    /// <summary>
    /// Executes the active party member's attack against a random enemy,
    /// processes the result immediately, then resumes AdvanceToNextPartyTurn.
    /// </summary>
    private bool ProcessPartyAttack(PTSoul attacker)
    {
        if (adversaries.Count == 0) return true;

        PTSoul target = adversaries[Random.Range(0, adversaries.Count)];
        int damage      = attacker.DealDamage();
        bool wasCrit    = attacker.WasCrit;
        int actualDamage = target.TakeDamage(damage);
        string critTag   = wasCrit ? " <color=yellow>CRITICAL HIT!</color>" : "";

        PTAdventureLog.Log(attacker.Name + " attacks " +
                           "<enemy>" + target.Name + "</enemy>" + " for " + actualDamage + " damage!" + critTag + " " +
                           "<enemy>" + target.Name + "</enemy>" + " has " +
                           target.currentHP + "/" + target.maxHP + " HP remaining.");

        if (!target.isAlive)
        {
            if (!killCredit.ContainsKey(target)) killCredit[target] = attacker;
            PTAdventureLog.Log("<enemy>" + target.Name + "</enemy>" + " has been slain by " + attacker.Name + "!");
            ChangeGold(target.goldReward);
            ChangeXP(target.accumulatedLifeXp, "<enemy>" + target.Name + "</enemy>");
            PTAdventureLog.Log("<enemy>" + target.Name + "</enemy>" + " has been slain! You found " + target.goldReward + " gold on the corpse.");
            Destroy(target.gameObject);
            adversaries.Remove(target);
        }

        return adversaries.Count == 0;
    }

    /// <summary>
    /// Executes one enemy's automatic attack, processes the result immediately.
    /// </summary>
    private bool ProcessEnemyAttack(PTSoul enemyAttacker)
    {
        if (partyMembers.Count == 0) return true;

        PTSoul partyTarget = partyMembers[Random.Range(0, partyMembers.Count)];
        int enemyDamage  = enemyAttacker.DealDamage();
        bool wasCrit     = enemyAttacker.WasCrit;
        int actualDamage = partyTarget.TakeDamage(enemyDamage);
        string critTag   = wasCrit ? " <color=yellow>CRITICAL HIT!</color>" : "";

        PTAdventureLog.Log("<enemy>" + enemyAttacker.Name + "</enemy>" + " attacks " +
                           partyTarget.Name + " for " + actualDamage + " damage!" + critTag + " " +
                           partyTarget.Name + " has " +
                           partyTarget.currentHP + "/" + partyTarget.maxHP + " HP remaining.");

        if (!partyTarget.isAlive)
        {
            if (!enemyKillCredit.ContainsKey(partyTarget)) enemyKillCredit[partyTarget] = enemyAttacker;
            PTAdventureLog.Log(partyTarget.Name + " has been slain by " + enemyAttacker.Name + "!");

            List<PTSoul> livingEnemies = adversaries.Where(a => a.isAlive).ToList();
            if (livingEnemies.Count > 0)
            {
                int xpShare = Mathf.FloorToInt((float)partyTarget.accumulatedLifeXp / livingEnemies.Count);
                foreach (PTSoul adv in livingEnemies)
                {
                    adv.GainXP(xpShare);
                    PTAdventureLog.Log("<enemy>" + adv.Name + "</enemy>" + " gained " + xpShare + " XP for slaying " + partyTarget.Name + "!");
                }
            }
            RemovePartyMember(partyTarget);
        }

        return partyMembers.Count == 0;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // INPUT HANDLER
    // ─────────────────────────────────────────────────────────────────────────────

    public void OnAttack(InputAction.CallbackContext context)                                               //Method to handle Left Click / Attack key
    {
        if (gameOver) return;
        if (!context.performed) return;

        if (adversaries.Count == 0)
        {
            PTAdventureLog.Log("There are no enemies to attack!");
            return;
        }

        if (!awaitingPartyInput)
        {
            PTAdventureLog.Log("It is not the party's turn yet!");
            return;
        }

        // Safety: skip if active actor died since their turn was announced
        if (activeActor == null || !activeActor.isAlive)
        {
            awaitingPartyInput = false;
            AdvancePartyTurn();
            return;
        }

        // Execute this party member's attack
        awaitingPartyInput = false;
        PTSoul attacker = activeActor;
        activeActor = null;

        bool allEnemiesDead = ProcessPartyAttack(attacker);
        UpdateUI();

        if (allEnemiesDead)
        {
            OnAllEnemiesDefeated();
            return;
        }

        // Resume the party phase — advance to the next party member
        AdvancePartyTurn();
    }

    // ─── Victory handler ───
    private void OnAllEnemiesDefeated()
    {
        ResetCombatState();
        ChangeGold(commission);
        SetSunIntensity(0.1f);
        canSleep = true;
        PTAdventureLog.Log("You have defeated all the enemies! You can now sleep through the night.");
        PTAdventureLog.Log("You earned a commission of " + commission + " gold! You have " + gold + " gold in total.");
        UpdateUI();
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

        // Calculate average enemy agility
        float avgEnemyAgility = 0f;
        foreach (PTSoul enemy in adversaries)
        {
          avgEnemyAgility += enemy.atrAgility;
        }
        avgEnemyAgility /= adversaries.Count;

        int fleeCost = Mathf.Max(1, Mathf.FloorToInt(gold * FLEE_COST_PERCENT));                          //10% of current gold, minimum 1
        ChangeGold(-fleeCost);                                                                             //deduct flee cost
        PTAdventureLog.Log("<partyname>" + partyName + "</partyname>" + " attempts to flee! " + fleeCost + " gold was lost.");

        // Each party member attempts to run individually based on their agility vs average enemy agility
        List<PTSoul> escaped = new List<PTSoul>();
        List<PTSoul> failedToEscape = new List<PTSoul>();

        foreach (PTSoul member in partyMembers)
        {
          if (!member.isAlive) continue;

          // Base 50% chance, +/- difference between member agility and average enemy agility
          float agilityDifference = member.atrAgility - avgEnemyAgility;
          int escapeChance = Mathf.Clamp(50 + Mathf.RoundToInt(agilityDifference), 5, 95);                //clamp between 5% and 95%

          int roll = Random.Range(0, 100);
          if (roll < escapeChance)
          {
            escaped.Add(member);
            PTAdventureLog.Log(member.Name + " escaped! (" + escapeChance + "% chance)");
          }
          else
          {
            failedToEscape.Add(member);
            PTAdventureLog.Log(member.Name + " failed to run away! (" + escapeChance + "% chance)");
          }
        }

        // Mark escaped members as cowardly
        foreach (PTSoul member in escaped)
        {
          if (!member.markedByDeath)
          {
            member.isCowardly = true;
          }
        }

        if (escaped.Count > 0)
        {
          PTAdventureLog.Log(escaped.Count + " party member(s) escaped and have been marked as Cowardly!");
        }

        // If everyone escaped, end combat
        if (failedToEscape.Count == 0)
        {
          for (int i = adversaries.Count - 1; i >= 0; i--)                                                //destroy all enemies without gold reward
          {
            Destroy(adversaries[i].gameObject);
          }
          adversaries.Clear();

          canSleep = true;                                                                                 //enable sleeping
          SetSunIntensity(0.1f);

          ResetCombatState();                                                                               //reset turn queue since combat is over
          PTAdventureLog.Log("The whole party escaped the battle! No gold was looted and no commission was earned.");
        }
        else
        {
          PTAdventureLog.Log(failedToEscape.Count + " party member(s) are still stuck in combat!");
          // Resume the queue for members still in combat
          if (!awaitingPartyInput)
              AdvancePartyTurn();
        }

        UpdateUI();
      }
    }
}
