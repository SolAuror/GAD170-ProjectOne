using UnityEngine;
using PartyTaxes;

public class PTRecruitGenerator : MonoBehaviour
{
    [Header("Name Generation")]
    public string[] namePrefixes = { "Brave", "Bold", "Swift", "Mighty", "Clever", "Noble", "Shadow", "Iron", "Wise", "Wild" };
    public string[] nameSuffixes = { "heart", "blade", "fist", "shield", "soul", "walker", "runner", "striker", "forge", "wind" };
    public string[] fullNames = { "Salen", "Loneth", "Dran", "Fayn", "Conti", "Gilaine", "Goro", "Horus", "Hester", "Juli" };
    
    [Header("Type Options")]
    public string[] characterTypes = { "Human", "Elf", "Dwarf", "Halfling" };
    
    [Header("Stat Ranges (Level 1)")]
    public Vector2Int hpRange = new Vector2Int(50, 100);
    public Vector2Int attackRange = new Vector2Int(8, 15);
    public Vector2Int defenseRange = new Vector2Int(5, 12);
    public Vector2Int wageRange = new Vector2Int(5, 20);
    
    /// <summary>
    /// Generate a random name using various combinations of prefixes, suffixes, and full names
    /// </summary>
    public string GenerateRandomName()
    {
        int choice = Random.Range(0, 3);
        
        if (choice == 0) // Use full name
        {
            return fullNames[Random.Range(0, fullNames.Length)];
        }
        else if (choice == 1) // Prefix + Suffix (compound word)
        {
            return namePrefixes[Random.Range(0, namePrefixes.Length)] + 
                   nameSuffixes[Random.Range(0, nameSuffixes.Length)];
        }
        else // Prefix + space + Suffix (two-word name)
        {
            return namePrefixes[Random.Range(0, namePrefixes.Length)] + " " + 
                   nameSuffixes[Random.Range(0, nameSuffixes.Length)];
        }
    }
    
    /// <summary>
    /// Randomize all stats for a PTSoul character at level 1
    /// </summary>
    public void RandomizeStats(PTSoul soul)
    {
        if (soul == null) return;
        
        soul.Name = GenerateRandomName();
        soul.Type = characterTypes[Random.Range(0, characterTypes.Length)];
        soul.maxHP = Random.Range(hpRange.x, hpRange.y + 1);
        soul.currentHP = soul.maxHP;
        soul.attack = Random.Range(attackRange.x, attackRange.y + 1);
        soul.defense = Random.Range(defenseRange.x, defenseRange.y + 1);
        soul.dailywages = Random.Range(wageRange.x, wageRange.y + 1);
        soul.level = 1;
        soul.currentXp = 0;
        soul.xpToNextLevel = 100;
        soul.UpdateUI(); // Update UI to reflect new stats
    }
    
    /// <summary>
    /// Create a completely random recruit from a base prefab template
    /// </summary>
    public GameObject CreateRandomRecruit(GameObject basePrefab, Transform parent, Vector3 position)
    {
        if (basePrefab == null) return null;
        
        GameObject newRecruit = Instantiate(basePrefab, position, Quaternion.identity, parent);
        PTSoul soul = newRecruit.GetComponent<PTSoul>();
        
        if (soul != null)
        {
            RandomizeStats(soul);
        }
        
        return newRecruit;
    }
    
    /// <summary>
    /// Randomize only the name of a PTSoul, keeping other stats intact
    /// </summary>
    public void RandomizeName(PTSoul soul)
    {
        if (soul == null) return;
        soul.Name = GenerateRandomName();
        soul.UpdateUI();
    }
    
    /// <summary>
    /// Randomize only combat stats, keeping name and level intact
    /// </summary>
    public void RandomizeCombatStats(PTSoul soul)
    {
        if (soul == null) return;
        
        // Scale stats based on current level
        int levelMultiplier = soul.level;
        soul.maxHP = Random.Range(hpRange.x, hpRange.y + 1) + (levelMultiplier - 1) * 10;
        soul.currentHP = soul.maxHP;
        soul.attack = Random.Range(attackRange.x, attackRange.y + 1) + (levelMultiplier - 1) * 2;
        soul.defense = Random.Range(defenseRange.x, defenseRange.y + 1) + (levelMultiplier - 1) * 2;
        soul.dailywages = Random.Range(wageRange.x, wageRange.y + 1) * levelMultiplier;
        soul.UpdateUI();
    }
}
