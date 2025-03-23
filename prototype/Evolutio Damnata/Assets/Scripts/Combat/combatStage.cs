using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/*
 * The CombatStage class is responsible for managing the combat stage of the game.
 * It keeps track of the game state, player and enemy health, mana, and turn count.
 * It also handles the end phase and end turn buttons, and the player and enemy actions.
 */

public class CombatStage : MonoBehaviour
{
    public Sprite wizardOutlineSprite;

    [SerializeField]
    public GameObject manaBar;
    [SerializeField]
    public GameObject manaText;
    public int currentMana;
    public int maxMana;

    [SerializeField]
    public CardManager cardManager;
    [SerializeField]
    public CardOutlineManager cardOutlineManager;
    [SerializeField]
    public CardLibrary cardLibrary;
    [SerializeField]
    public CombatManager combatManager;

    [SerializeField]
    Canvas battleField;

    [SerializeField]
    public SpritePositioning spritePositioning;

    [SerializeField]
    DamageVisualizer damageVisualizer;

    [SerializeField]
    GameObject damageNumberPrefab;

    private bool buttonsInitialized = false;

    private CardSelectionHandler cardSelectionHandler;
    private ButtonCreator buttonCreator;
    private AttackHandler attackHandler;
    private GeneralEntities playerCardSpawner;
    public GeneralEntities enemyCardSpawner;
    private EnemySelectionEffectHandler enemySelectionEffectHandler;
    private PlayerSelectionEffectHandler playerSelectionEffectHandler;

    private AttackLimiter attackLimiter;

    private void Awake()
    {
        attackLimiter = new AttackLimiter();

        playerCardSpawner = new GeneralEntities(spritePositioning, cardLibrary, damageVisualizer, damageNumberPrefab, wizardOutlineSprite, this, attackLimiter, EntityManager._monsterType.Friendly);
        enemyCardSpawner = new GeneralEntities(spritePositioning, cardLibrary, damageVisualizer, damageNumberPrefab, wizardOutlineSprite, this, attackLimiter, EntityManager._monsterType.Enemy);

        cardSelectionHandler = gameObject.AddComponent<CardSelectionHandler>();
        cardSelectionHandler.Initialize(cardManager, combatManager, cardOutlineManager, spritePositioning, this, playerCardSpawner);

        buttonCreator = gameObject.AddComponent<ButtonCreator>();
        buttonCreator.Initialize(battleField, spritePositioning, cardSelectionHandler);

        attackHandler = new AttackHandler(attackLimiter);

        enemySelectionEffectHandler = new EnemySelectionEffectHandler(spritePositioning);
        playerSelectionEffectHandler = new PlayerSelectionEffectHandler(spritePositioning, cardManager);
    }

    // This function will be kept
    public void interactableHighlights()
    {
        if (buttonsInitialized) return;

        buttonCreator.AddButtonsToPlayerEntities();
        buttonCreator.AddButtonsToEnemyEntities();

        buttonsInitialized = true;
    }

    public void HandleMonsterAttack(EntityManager playerEntity, EntityManager enemyEntity)
    {
        attackHandler.HandleMonsterAttack(playerEntity, enemyEntity);
    }

    public void spawnEnemy(string cardName, int whichOutline)
    {
        enemyCardSpawner.SpawnCards(cardName, whichOutline);
    }

    void Start()
    {
        // Start the coroutine to wait for room selection
        StartCoroutine(spritePositioning.WaitForRoomSelection());

        // Set all placeholders to be inactive initially
        StartCoroutine(spritePositioning.SetAllPlaceHoldersInactive());

        // Initialize interactable highlights
        StartCoroutine(InitializeInteractableHighlights());
    }

    private IEnumerator InitializeInteractableHighlights()
    {
        // Wait until placeholders are instantiated
        while (spritePositioning.playerEntities.Count == 0)
        {
            yield return null; // Wait for the next frame
        }

        // Initialize interactable highlights
        interactableHighlights();
    }

    public void updateManaUI()
    {
        Slider manaSlider = manaBar.GetComponent<Slider>();
        manaSlider.maxValue = maxMana;
        manaSlider.value = currentMana;
        manaText.GetComponent<TMP_Text>().text = currentMana.ToString();
    }

    private void Update()
    {
        // Check if a card is selected and update placeholder visibility
        if (cardManager.currentSelectedCard != null)
        {
            EntityManager selectedCardEntityManager = cardManager.currentSelectedCard.GetComponent<EntityManager>();
            if (selectedCardEntityManager != null && selectedCardEntityManager.placed)
            {
                placeHolderActiveState(false);
                enemySelectionEffectHandler.ApplyEffect(true);

                playerSelectionEffectHandler.ApplyEffect();
            }
            else
            {
                placeHolderActiveState(true);
                enemySelectionEffectHandler.ApplyEffect(false);
            }
        }
        else
        {
            placeHolderActiveState(false);
            enemySelectionEffectHandler.ApplyEffect(false);
        }
    }

    public void placeHolderActiveState(bool active)
    {
        for (int i = 0; i < spritePositioning.playerEntities.Count; i++)
        {
            if (spritePositioning.playerEntities[i] != null)
            {
                Image placeholderImage = spritePositioning.playerEntities[i].GetComponent<Image>();
                if (placeholderImage != null && placeholderImage.sprite != null)
                {
                    if (placeholderImage.sprite.name == "wizard_outline")
                    {
                        spritePositioning.playerEntities[i].SetActive(active);
                    }
                }
            }
        }
    }
}
