using UnityEngine;
using UnityEngine.UI;


public class CardOutlineManager : MonoBehaviour, ICardOutlineManager
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource _selectAudioSource;

    private GameObject _currentlyHighlightedCard;

    public bool CardIsHighlighted { get; private set; }

    public void HighlightCard(GameObject cardObject)
    {
        // Null check for safety
        if (cardObject == null) return;

        // If clicking a different card, remove previous highlight
        if (_currentlyHighlightedCard != null && _currentlyHighlightedCard != cardObject)
        {
            RemoveHighlight(_currentlyHighlightedCard);
        }

        // Toggle highlight if clicking same card
        if (_currentlyHighlightedCard == cardObject)
        {
            RemoveHighlight(cardObject);
            return;
        }

        // Add or enable outline
        var outline = GetOrAddOutlineComponent(cardObject);
        outline.enabled = true;
        _currentlyHighlightedCard = cardObject;
        CardIsHighlighted = true;

        PlaySelectionSound();
    }

    public void RemoveHighlight(GameObject cardObject)
    {
        if (cardObject == null) return;

        var outline = cardObject.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        if (_currentlyHighlightedCard == cardObject)
        {
            _currentlyHighlightedCard = null;
            CardIsHighlighted = false;
        }
    }

    public void RemoveHighlight()
    {
        if (_currentlyHighlightedCard == null) return;

        var outline = _currentlyHighlightedCard.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        _currentlyHighlightedCard = null;
        CardIsHighlighted = false;
    }

    private Outline GetOrAddOutlineComponent(GameObject cardObject)
    {
        var outline = cardObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = cardObject.AddComponent<Outline>();
            outline.effectColor = Color.green;
            outline.effectDistance = new Vector2(6f, 6f);
        }
        return outline;
    }

    private void PlaySelectionSound()
    {
        if (_selectAudioSource == null)
        {
            _selectAudioSource = GetComponent<AudioSource>();
            if (_selectAudioSource == null) return;
        }

        if (_selectAudioSource.isPlaying)
        {
            _selectAudioSource.Stop();
        }
        _selectAudioSource.Play();
    }
}