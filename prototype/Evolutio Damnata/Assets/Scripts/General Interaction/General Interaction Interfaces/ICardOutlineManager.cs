using UnityEngine;

public interface ICardOutlineManager
{
    bool CardIsHighlighted { get; }
    void HighlightCard(GameObject cardObject);
    void RemoveHighlight(GameObject cardObject);
    void RemoveHighlight();
}