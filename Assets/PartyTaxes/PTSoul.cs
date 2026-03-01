using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PartyTaxes {
public class PTSoul : MonoBehaviour
  {

  [SerializeField] public TextMeshProUGUI levelText;                                                    //reference to TextMeshPro level text obj
  [SerializeField] public TextMeshProUGUI xpText;                                                       //reference to TextMeshPro gold text obj
  [SerializeField] public Slider xpSlider;                                                              //reference to xp slder obj
  [SerializeField] public TextMeshProUGUI nameText;                                                     //reference to TextMeshPro name text obj
  [SerializeField] public TextMeshProUGUI healthText;                                                   //reference to TextMeshPro HP text obj
  [SerializeField] public Slider healthSlider;                                                          //reference to TextMeshPro attack text obj


    [Header("Character Attributes")]                                                                //Header for visual separation in the inspector
    public string Name;                                                                             //string for the name of the soul, used to identify the soul in the party and in combat
    public string Type;                                                                             //string to determine the type of soul, such as human, elf goblin ect
    public int level;                                                                               //string for level of the soul, can be used for scaling difficulty and rewards
    public int maxHP;                                                                               //integer for the maximum HP of the soul, used to determine how much damage it can take before dying
    public int currentHP;                                                                           //integer for the current HP of the soul, used to determine if it is alive or dead and how much damage it can take before dying
    public int attack;                                                                              //integer for the attack stat of the soul, used to determine how much damage it can deal to enemies
    public int defense;                                                                             //integer for the defense stat of the soul, used to determine how much damage it can reduce when taking damage from enemies
    public int dailywages = 20;                                                                     //integer for the daily wages of the soul, used to determine how much gold it costs to keep the soul in the party each day
    public bool isAlive => currentHP > 0;                                                           //bool to check if the soul is alive, returns true if currentHP is greater than 0, false otherwise  



    [Header("Enemy Attributes")]                                                                        //Header for visiual seperation in the inspector
    public bool isAdversary = false;                                                                //bool to determine if the soul is an enemy or a party member, default is false for party members
    public int goldReward = 10;                                                                     //integer to determine how much gold the soul rewards when defeated



    public void Start()
    {
        currentHP = maxHP;                                                                           //initialize current HP to max HP at the start of the game
        UpdateUI();                                                                                  //initialize UI elements with starting values
    }

    public int DealDamage()                                         //method to calculate damage dealt
    {
        int damage = Random.Range(attack, attack * 2) - defense;    // Simple damage calculation based on attack and defense
        return Mathf.Max(damage, 0);                                // Ensure damage is not negative
    }

    public int TakeDamage(int damage)                               //method to apply damage taken
    {
        currentHP -= damage;
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
            // XP slider can be implemented when XP system is added
            xpSlider.value = 0;
        }
        
        if (xpText != null)
        {
            // XP text can be implemented when XP system is added
            xpText.text = "XP: 0";
        }
    }

    // Additional character attributes and methods can be added here
  }
}