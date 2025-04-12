using UnityEngine;

/**
 * This class is used to store all the keywords that are used in the game.
 * This is a singleton class, so it can be accessed from any other class.
 */

public class Keywords : MonoBehaviour
{
    public enum MonsterKeyword
    {
        None,
        Taunt,
        Ranged,
    }
}
