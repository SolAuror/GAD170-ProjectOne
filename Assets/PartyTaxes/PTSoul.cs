using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PartyTaxes {
public class PTSoul : MonoBehaviour
  {

  [SerializeField] public TextMeshProUGUI levelText;                                                    //reference to TextMeshPro level text obj
  [SerializeField] public TextMeshProUGUI xpText;                                                       //reference to TextMeshPro xp text obj
  [SerializeField] public Slider xpSlider;                                                              //reference to xp slder obj
  [SerializeField] public TextMeshProUGUI nameText;                                                     //reference to TextMeshPro name text obj
  [SerializeField] public TextMeshProUGUI healthText;                                                   //reference to TextMeshPro HP text obj
  [SerializeField] public Slider healthSlider;                                                          //reference to health slider obj


    [Header("Character Attributes")]                                                                //Header for visual separation in the inspector
    public string Name;                                                                             //string for the name of the soul
    public string Type;                                                                             //string to determine the type of soul, such as human, elf goblin ect
    public int level;                                                                               //integer for level of the soul

    public int currentXp;                                                                           //integer for the current XP of the soul
    public int xpToNextLevel;                                                                       //integer for the XP required to reach the next level
    
    public int maxHP;                                                                               //integer for the maximum HP of the soul
    public int currentHP;                                                                           //integer for the current HP of the soul
    public int attack;                                                                              //integer for the attack stat of the soul
    public int defense;                                                                             //integer for the defense stat of the soul
    public int dailywages = 20;                                                                     //integer for the daily wages of the soul
    public bool isAlive => currentHP > 0;                                                           //bool to check if the soul is alive



    [Header("Enemy Attributes")]                                                                    //Header for visiual seperation in the inspector
    public bool isAdversary = false;                                                                //bool to determine if the soul is an enemy or a party member, default is false for party members
    public int goldReward = 10;                                                                     //integer to determine how much gold the soul rewards when defeated
    public int accumulatedLifeXp = 25;                                                              //integer for the XP rewarded when this soul is defeated, it is the liftetime experience of the soul, 
                                                                                                    // mening it will reward more the longer it has been alive set to 10 default for a level 1 soul.

    public void Start()
    {
        currentXp = 0;                                                                               //initialize current XP to 0 at the start of the game
        xpToNextLevel = (level + 1) * 50;                                                           //initialize XP required for next level to 100 at the start of the game
        currentHP = maxHP;                                                                           //initialize current HP to max HP at the start of the game
        UpdateUI();                                                                                  //initialize UI elements with starting values
    }

    public int DealDamage()                                         //method to calculate damage dealt
    {
        int damage = Random.Range(attack, attack * 2);    // Simple damage calculation based on attack and defense
        return Mathf.Max(damage, 0);                                // Ensure damage is not negative
    }

    public int TakeDamage(int damage)                               //method to apply damage taken
    {
        currentHP -= damage;                                        // Subtract damage from current HP                                  
        if (currentHP < 0) currentHP = 0;                           // Ensure HP does not go below 0
        UpdateUI();                                                 // Update UI after taking damage
        return damage;
    }

    public int Heal(int healAmount)                                 //method to heal the soul
    {
        int amountHealed = Mathf.Min(healAmount, maxHP - currentHP); // Calculate actual amount healed
        currentHP += amountHealed;
        if (currentHP > maxHP) currentHP = maxHP;                   // Ensure HP does not exceed max HP
        UpdateUI();                                                 // Update UI after healing
        return amountHealed;
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
    {   int xpOverflow;
        level++;
        accumulatedLifeXp += currentXp;                              // Increase accumulated life XP by the currently held XP before it resets to 0.
        xpOverflow = currentXp - xpToNextLevel;                      // Calculate XP overflow after leveling up
        currentXp = xpOverflow;                                      // Set current XP to the overflow amount
        xpToNextLevel = (level + 1) * 50;                            // Increase XP required for next level
        maxHP += 10;                                                 // Increase max HP on level up
        attack += 2;                                                 // Increase attack on level up
        defense += 2;                                                // Increase defense on level up
        currentHP = maxHP;                                           // Restore HP to max on level up
    }

    public void UpdateUI()                                          //method to update all UI elements
    {
        if (nameText != null)
            nameText.text = Name;                                   // Update name text
        
        if (levelText != null)
            levelText.text = "Lvl " + level.ToString();             // Update level text
        
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