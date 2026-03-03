using System.Collections.Generic;
using UnityEngine;
using PartyTaxes;

/// <summary>
/// PTManager partial — Party formation, member management, and leveling helpers.
/// </summary>
public partial class PTManager
{
    Vector3 GetNextPartyMemberTransform(out Quaternion rotation)                                         //Helper: repositions existing members and returns position + rotation for the next member
    {
        rotation = Quaternion.Euler(0, 180, 0);                                                             //set correct rotation for party members (facing positive Z)
        
        Vector3 baseSpawnPosition = partySpawnPoint != null ?
                                    partySpawnPoint.position :
                                    transform.position;                                                     //use spawn point if assigned, otherwise use PTManager position
        float spacing = 1.2f;                                                                               //spacing between party members along x-axis
        int newCount = partyMembers.Count + 1;                                                              //total party size after this member is added
        float totalWidth = (newCount - 1) * spacing;                                                        //total width of the party formation
        float startX = -(totalWidth / 2f);                                                                  //leftmost position, so the group stays centered on the spawn point

        for (int j = 0; j < partyMembers.Count; j++)                                                       //reposition existing members to keep the group centered
        {
            partyMembers[j].transform.position = baseSpawnPosition + new Vector3(startX + j * spacing, 0, 0);
            partyMembers[j].transform.rotation = rotation;                                                 //apply party member rotation
        }

        return baseSpawnPosition + new Vector3(startX + partyMembers.Count * spacing, 0, 0);               //new member takes the last slot
    }

    void AddPartyMember(GameObject prefab)                                                                  //Method to add a new party member to the party, takes a GameObject prefab as a parameter
    {
        if (partyMembers.Count < maxPartyCount)                                                             //check if the party is not full
        {
            if (prefab != null)
            {
                PTSoul prefabSoul = prefab.GetComponent<PTSoul>();
                if (prefabSoul != null)
                {
                    bool isDuplicate = deadCharacterNames.Contains(prefabSoul.Name);          //block characters who have died from being re-recruited
                    if (!isDuplicate)
                    {
                        foreach (PTSoul existingMember in partyMembers)
                        {
                            if (existingMember.Name == prefabSoul.Name)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }
                    }
                    
                    if (isDuplicate && characterPrefabs != null && characterPrefabs.Length > 0) //find alternative if duplicate detected
                    {
                        GameObject alternativePrefab = null;
                        foreach (GameObject candidatePrefab in characterPrefabs)
                        {
                            if (candidatePrefab == null) continue;
                            PTSoul candidateSoul = candidatePrefab.GetComponent<PTSoul>();
                            if (candidateSoul == null) continue;
                            
                            bool isAvailable = true;
                            foreach (PTSoul existingMember in partyMembers)
                            {
                                if (existingMember.Name == candidateSoul.Name)
                                {
                                    isAvailable = false;
                                    break;
                                }
                            }
                            
                            if (isAvailable && !deadCharacterNames.Contains(candidateSoul.Name)) //also skip dead characters when searching for alternatives
                            {
                                alternativePrefab = candidatePrefab;
                                break;
                            }
                        }
                        
                        if (alternativePrefab != null)
                        {
                            prefab = alternativePrefab; //use the alternative instead
                        }
                        else
                        {
                            return; //no alternatives available, exit without adding
                        }
                    }
                    else if (isDuplicate)
                    {
                        return; //duplicate with no alternatives to search, exit without adding
                    }
                }
                
                Quaternion spawnRotation;
                Vector3 spawnPosition = GetNextPartyMemberTransform(out spawnRotation);                    //reposition existing members and get the next slot in the formation
                
                GameObject characterObj = Instantiate(prefab,                                               //create an instance of the character at the designated spawn position as child of PTManager
                                                      spawnPosition, 
                                                      spawnRotation, 
                                                      transform);                                           
                PTSoul soul = characterObj.GetComponent<PTSoul>();                                          //get the PTSoul component from the instance
                if (soul != null)                                                                           //if the soul exists on the prefab
                {
                    partyMembers.Add(soul);                                                                 //add the new member to the party

                                                                                                            // Level up new recruit to average party level, excluding the new member
                    LevelUpToPartyAverage(soul);

                    PTAdventureLog.Log(soul.Name + " joined " + partyName + "!");                    //log message that a new member has joined
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
            //only add to dead lists if the member is actually dead
            if (!member.isAlive)
            {
                //find matching prefab for resurrection purposes
                GameObject matchingPrefab = null;
                if (characterPrefabs != null && characterPrefabs.Length > 0)
                {
                    foreach (GameObject prefab in characterPrefabs)
                    {
                        PTSoul prefabSoul = prefab?.GetComponent<PTSoul>();
                        if (prefabSoul != null && prefabSoul.Name == member.Name)
                        {
                            matchingPrefab = prefab;
                            break;
                        }
                    }
                    if (matchingPrefab == null) matchingPrefab = characterPrefabs[0]; //fallback to first prefab as template
                }

                deadCharacterNames.Add(member.Name);                                                            //track this character as dead to prevent respawning
                deadCharacters.Add(new DeadCharacterData(member, matchingPrefab));                              //store full character data for resurrection
            }
            
            partyMembers.Remove(member);                                                                    //remove from list
            Destroy(member.gameObject);                                                                     //destroy the GameObject

            PTAdventureLog.Log(member.Name + " has left the party!");                                       //log message that a member has left

            if (debugMode)                                                                                  //if debug mode is enabled, print debug message
            {
                Debug.Log(member.Name + " has left the party!");
            }
        }
    }

    void LevelUpToPartyAverage(PTSoul newMember)                                                            //Level up new member to match party average
    {
        if (partyMembers.Count <= 1) return;                                                                //no need if they're the only member or first member

        int totalLevels = 0;
        foreach (PTSoul member in partyMembers)
        {
            if (member != newMember)                                                                        //exclude the new member from calculation
            {
                totalLevels += member.level;
            }
        }
        int avgLevel = totalLevels / (partyMembers.Count - 1);

        while (newMember.level < avgLevel)
        {
            newMember.LevelUp();
        }

        if (newMember.level > 1 && debugMode)
        {
            Debug.Log(newMember.Name + " leveled up to match party average (Level " + avgLevel + ")");
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
}
