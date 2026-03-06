using UnityEngine;
using TMPro;
using UnityEngine.UI;


namespace PartyTaxes {
public class PTSoul : MonoBehaviour                                                               // PTSoul class — represents the soul of a character, 
  {                                                                                               //with attributes, stats, leveling, and combat methods.

  [SerializeField] public TextMeshProUGUI levelText;                                                    //reference to TextMeshPro level text obj
  [SerializeField] public TextMeshProUGUI xpText;                                                       //reference to TextMeshPro xp text obj
  [SerializeField] public Slider xpSlider;                                                              //reference to xp slder obj
  [SerializeField] public TextMeshProUGUI nameText;                                                     //reference to TextMeshPro name text obj
  [SerializeField] public TextMeshProUGUI healthText;                                                   //reference to TextMeshPro HP text obj
  [SerializeField] public Slider healthSlider;                                                          //reference to health slider obj


    [Header("Character Information")]                                                               //info about the character
    public string Name;                                                                             //string for the name of the soul
    public string Type;                                                                             //string to determine the type of soul, such as human, elf goblin ect
    public int level;                                                                               //integer for level of the soul
    public int currentXp;                                                                           //integer for the current XP of the soul
    public int xpToNextLevel;                                                                       //integer for the XP required to reach the next level
    
    [Header("Attributes")]                                                                          //info about the characters attributes
    public int atrMight;                                                                               //integer for the might attribute of the soul
    public int atrAgility;                                                                             //integer for the agility attribute of the soul
    public int atrConstitution;                                                                        //integer for the constitution attribute of the soul
    public int atrSense;                                                                               //integer for the sense attribute of the soul
    public int atrLuck;                                                                                //integer for the luck attribute of the soul
    public int atrPoints = 0;                                                                //integer for unspent attribute points, starts at 0 and increases on level up

    [Header("Character Stats")]                                                                     //info about the characters stats
    public int maxHP;                                                                               //integer for the maximum HP of the soul, derived from Constitution
    public int currentHP;                                                                           //integer for the current HP of the soul
    public int attack;                                                                              //integer for the attack stat of the soul, derived from Might
    public int critChance;                                                                          //integer for the critical hit chance (%), derived from Luck
    public float critMultiplier;                                                                    //float for the critical hit damage multiplier, derived from Luck, Might and Sense
    public int defense;                                                                             //integer for the defense stat of the soul, derived from Constitution
    public int dailywages = 20;                                                                     //integer for the daily wages of the soul

    [Header("Status Effects")]                                                                   
    public bool isAlive => currentHP > 0;                                                           //bool to check if the soul is alive
    public bool isCowardly = false;                                                                   //bool to determine if the soul is cowardly, -could apply to enemies later
    public bool markedByDeath = false;                                                              //bool to track if character was resurrected, suffers stat penalties and cannot become cowardly

    [Header("Enemy Attributes")]                                                                    //Header for visiual seperation in the inspector
    public bool isAdversary = false;                                                                //bool to determine if the soul is an enemy or a party member, default is false for party members
    public int goldReward = 10;                                                                     //integer to determine how much gold the soul rewards when defeated
    public int accumulatedLifeXp = 25;                                                              //integer for the XP rewarded when this soul is defeated, it is the liftetime experience of the soul, 
                                                                                                    // mening it will reward more the longer it has been alive set to 10 default for a level 1 soul.

    public void Start()
    {
        currentXp = 0;                                                                               //initialize current XP to 0 at the start of the game
        xpToNextLevel = (level + 1) * 50;                                                           //initialize XP required for next level based on level formula
        RecalculateStats();                                                                         //derive stats from attributes
        if(!isAdversary)
            {
                defense += 3;                                                                 //Give party members a flat 3 defense bonus to make them more durable, since they will be taking more damage than enemies.
            }
        currentHP = maxHP;                                                                           //initialize current HP to max HP at the start of the game
        UpdateUI();                                                                                  //initialize UI elements with starting values
    }

    private bool lastHitWasCrit = false;                                                          //tracks whether the last DealDamage call was a critical hit

    public bool WasCrit => lastHitWasCrit;                                                          //public accessor for crit status of last attack

    public int DealDamage()                                         //method to calculate damage dealt, with crit chance
    {
        lastHitWasCrit = Random.Range(0, 100) < critChance;         // Roll for critical hit based on critChance %
        int damage = attack;                                        // Base damage equals attack
        if (lastHitWasCrit)
        {
            damage = Mathf.RoundToInt(damage * critMultiplier);     // Apply crit multiplier on critical hit
        }
        return Mathf.Max(damage, 0);                                // Ensure damage is not negative
    }

    public int TakeDamage(int damage)                               //method to apply damage taken using diminishing returns defense
    {
        float reduction = defense / (float)(defense + 50);           // Diminishing returns: 10 def = 17%, 50 def = 50%, 100 def = 67%
        int actualDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - reduction))); // Always deal at least 1 damage
        currentHP -= actualDamage;
        if (currentHP < 0) currentHP = 0;                           // Ensure HP does not go below 0
        UpdateUI();                                                 // Update UI after taking damage
        return actualDamage;                                        // Return actual damage dealt after defense
    }

    public int Heal(int healAmount)                                 //method to heal the soul
    {
        if (isCowardly)                                              //if the soul is cowardly, reduce healing by 30%
        {
            healAmount = Mathf.RoundToInt(healAmount * 0.7f);
            PTAdventureLog.Log("The Cleric recognizes " + Name + "'s cowardice. 30% reduced healing for them.");
        }
        int amountHealed = Mathf.Min(healAmount, maxHP - currentHP); // Calculate actual amount healed
        currentHP += amountHealed;
        if (currentHP > maxHP) currentHP = maxHP;                   // Ensure HP does not exceed max HP
        UpdateUI();                                                 // Update UI after healing
        return amountHealed;
    }

    public int Bless()                                 //method to fully heal the soul for extra money, but removes cowardly status if they have it
    {
        if (isCowardly)                                              
        {
            isCowardly = false;                                              //Remove cowardly status on blessing
        }
        int amountToHeal = Mathf.Min(maxHP - currentHP, maxHP); // Calculate actual amount to heal
        currentHP += amountToHeal;
        if (currentHP > maxHP) currentHP = maxHP;                   // Ensure HP does not exceed max HP
        PTAdventureLog.Log(Name + " has been blessed by the priest and fully healed!"); // Log blessing event
        UpdateUI();                                                 // Update UI after healing
        return amountToHeal;
    }

    /// Changes a specific attribute by the given amount, clamped between 1 and 99.
    /// Recalculates derived stats (HP, attack, defense) after the change.
    public void ChangeAttribute(string attributeName, int amount)
    {
        switch (attributeName.ToLower())
        {
            case "might":       atrMight = Mathf.Clamp(atrMight + amount, 1, 99); break;
            case "agility":     atrAgility = Mathf.Clamp(atrAgility + amount, 1, 99); break;
            case "constitution": atrConstitution = Mathf.Clamp(atrConstitution + amount, 1, 99); break;
            case "sense":       atrSense = Mathf.Clamp(atrSense + amount, 1, 99); break;
            case "luck":        atrLuck = Mathf.Clamp(atrLuck + amount, 1, 99); break;
            default: Debug.LogWarning("Unknown attribute: " + attributeName); return;
        }
        RecalculateStats();                                          // Recalculate derived stats after attribute change
        UpdateUI();
    }

    /// Recalculates derived stats (maxHP, attack, defense, critChance, critMultiplier) from attributes.
    /// HP = 60 + (Con * 10), Defense = Con * 2, Attack = Might * 4.
    /// CritChance = Luck * 3 (clamped 0-75%), CritMultiplier = 1.5 + (Luck*0.02) + (Might*0.02) + (Sense*0.02).
    public void RecalculateStats()
    {
        int previousMaxHP = maxHP;
        maxHP = 60 + (atrConstitution * 10);                        // 60 base HP + 10 per Constitution
        attack = atrMight * 4;                                       // 4 attack per Might
        defense = atrConstitution * 2;                                // 2 defense per Constitution
        critChance = Mathf.Clamp(atrLuck * 3, 0, 75);               // 3% crit per Luck, capped at 75%
        critMultiplier = 1.5f + (atrLuck * 0.02f) + (atrMight * 0.02f) + (atrSense * 0.02f); // Base 1.5x + scaling from Luck, Might and Sense
        // Adjust current HP proportionally if maxHP changed
        if (previousMaxHP > 0 && currentHP > 0)
        {
            currentHP = Mathf.Min(currentHP, maxHP);                 // Ensure currentHP does not exceed new maxHP
        }
    }

    public void GainXP(int xpAmount)                                  //method to gain XP
    {
        currentXp += xpAmount;
        while (currentXp >= xpToNextLevel)                            // Check for level up
        {
            LevelUp();
        }
        UpdateUI();                                                  // Update UI after gaining XP
    }

    public void LevelUp()                                            //method to handle leveling up
    {   
        int xpOverflow;
        level++;
        atrPoints++;
        accumulatedLifeXp += currentXp;                              // Increase accumulated life XP by the currently held XP before it resets to 0.
        xpOverflow = currentXp - xpToNextLevel;                      // Calculate XP overflow after leveling up
        currentXp = xpOverflow;                                      // Set current XP to the overflow amount
        xpToNextLevel = (level + 1) * 50;                            // Increase XP required for next level
        RecalculateStats();                                          // Recalculate stats from attributes on level up
        currentHP = Random.Range(currentHP + 10, maxHP);                // Restore HP by 10 on level up, capped at max
        dailywages = Mathf.RoundToInt(dailywages * 1.25f);           // Increase daily wages by 25% on level up
        if(!isAdversary)
        {
        PTAdventureLog.Log(Name + " leveled up! Their Daily wages are now " + dailywages + " gold."); // Log level up event
        }
    }

    public void UpdateUI()                                          //method to update all UI elements
    {
        if (nameText != null)
            nameText.text = Name;                                   // Update name text
        
        if (levelText != null)
            levelText.text = level.ToString();             // Update level text
        
        if (healthText != null)
            healthText.text = currentHP + "/" + maxHP;              // Update health text
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHP;                          // Set max value for health slider
            healthSlider.value = currentHP;                         // Update health slider value
        }
        
        if (xpSlider != null)
        {
            xpSlider.maxValue = xpToNextLevel;
            xpSlider.value = currentXp;
        }
        
        if (xpText != null)
        {
            xpText.text = "XP: " + currentXp + "/" + xpToNextLevel;
        }
    }

  }
}