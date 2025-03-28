using UnityEngine;

public class CardUI : MonoBehaviour
{
    [SerializeField]
    private Card _card;

    public Card Card
    {
        get => _card;
        set => _card = value;
    }
}
