using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class RoomScript : MonoBehaviour
{
    public enum _roomsType {
        standard,
        shop,
        boss
    }

    enum _objectives
    {
        clear,
        boss
    }

    [SerializeField]
    GameObject numberVis;
    [SerializeField]
    public GameObject manaBar;
    [SerializeField]
    public GameObject manaText;
    public int currentMana = 10;
    public int turn = 0;
    public TMP_Text turnText;

    [SerializeField]
    GameObject backgroundImg; //<--- this has been set in the editor
    Sprite newBackgroundImage;
    [SerializeField]
    GameObject monsterPrefab;

    [SerializeField]
    Sprite placeHolderSprites;
    [SerializeField]
    int placeHolderSpriteCount = 3;

    public _roomsType roomsType;
    static float _playAreaHeight = 423f; //<--this is the play area height
    static float _playAreaWidth = 527f; //<--this is the play area height
    [SerializeField]
    float _initalOffsetX = 153f;
    [SerializeField]
    float _initalOffsetY = 220f;

    [SerializeField]
    Canvas targetCanvas;

    [SerializeField]
    CardManager cardManager;
    [SerializeField]
    public CardLibrary cardLibrary;

    [SerializeField]
    CardOutlineManager cardOutlineManager;
    [SerializeField]
    List<Image> Outlines;

    [SerializeField]
    GameObject _combatManager;
    CombatManager combatManager;

    List<GameObject> enemyMonsters = new List<GameObject>();
    List<GameObject> playerMonsters = new List<GameObject>();

    //this needs to be set in generate room, need to be even
    //also used to show where the monster is on the map reference design doc
    //index 0 == player them selves
    //index last == enemy if we have attacking wizards
    //the rest are monsters split evenyl
    //  \-so player side monster are 1 -> (size-2)/2
    //  |-enemys are ((size-2)/2)+1 -> size size - 2
    //[SerializeField]
    //GameObject[] entities = new GameObject[8];
    //int numberOfEntites = 8;

    //this is a self setup function, use enemy generate
    //will need to somehow scale the backgroun obj with the screen size
    public void generateRoom(_roomsType roomType) {
        this.roomsType = roomType;
        //get room image(random background image)
        GlobalResources globalResources = GameObject.Find("ResourceManagaer").GetComponent<GlobalResources>();
        newBackgroundImage = globalResources.dungeonRooms[Random.Range(0, globalResources.dungeonRooms.Count)];
        backgroundImg = GameObject.Find("Canvas");
        backgroundImg.GetComponent<Image>().sprite = newBackgroundImage;

        // Generate room enemy monsters
        int numMonsters = Random.Range(1, 4); // Can only generate minimum of 1 monster, maximum of 3 monsters
        float spaceBetweenMonsters = _playAreaHeight / numMonsters;
        float spaceBetweenMonstersX = _playAreaWidth / numMonsters;

        if (roomsType == _roomsType.shop) { numMonsters = 0; }
        if (roomsType == _roomsType.boss) { numMonsters = 1; }
        for (int i = 0; i < numMonsters; i++) {

            GameObject canv = GameObject.Find("Canvas");
            RectTransform canvRect = canv.GetComponent<RectTransform>();
            Vector2 centre = new Vector2((canvRect.rect.width / 2) * canv.transform.localScale.x, (canvRect.rect.height / 2) * canv.transform.localScale.y);

            float newy = centre.y - (_initalOffsetY + (spaceBetweenMonsters * i));
            float newx = centre.x + _initalOffsetX + (spaceBetweenMonstersX * i);

            GameObject newMonster = Instantiate(monsterPrefab, canv.transform);

            if (roomsType == _roomsType.boss) {
                newMonster.GetComponent<MonsterScript>().GenerateMonster(gameObject, i+3, MonsterScript._monsterType.Enemy);
                enemyMonsters.Add(newMonster);
                newMonster.transform.position = new Vector3(newx, newy, 0);
                newMonster.transform.localScale = new Vector3(-7, 7, 7);
                newMonster.GetComponent<MonsterScript>().placed = true;

                //guarantee the spawns of stronger the monsters below
            }
            else {
                newMonster.GetComponent<MonsterScript>().GenerateMonster(gameObject, i+3, MonsterScript._monsterType.Enemy);
                enemyMonsters.Add(newMonster);
                newMonster.transform.position = new Vector3(newx, newy, 0);
                newMonster.transform.localScale = new Vector3(-7, 7, 7);
                newMonster.GetComponent<MonsterScript>().placed = true;
            }

            GameObject buttonObject = new GameObject($"Select_Button_{enemyMonsters.Count + i}");
            buttonObject.transform.SetParent(newMonster.transform, false); // Add as a child of the Outline
            buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Outline

            // Add required components to make it a Button
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(25, 53); // Why 25x53?

            Button buttonComponent = buttonObject.AddComponent<Button>();

            // Optional: Add an Image component to visualize the Button
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button
            buttonComponent.onClick.AddListener(() =>
            {
                combatManager.EnemySelectedMonster = newMonster;
                combatManager.TriggerAttack();
            });

            Debug.Log(newx+ " :newx -- newy: "+newy);
        }



        /*
         
         
         
         
         <---- DISPLAY CHOICE HIGHLIGHTS CODE --->
         
         
         
         
         */

        spaceBetweenMonsters = _playAreaHeight / 3;
        spaceBetweenMonstersX = _playAreaWidth / 3;

        for (int i = 0; i < placeHolderSpriteCount; i++)
        {
            RectTransform canvRect = targetCanvas.GetComponent<RectTransform>();
            Vector2 centre = new Vector2((canvRect.rect.width / 2) * targetCanvas.transform.localScale.x, (canvRect.rect.height / 2) * targetCanvas.transform.localScale.y);

            float newy = centre.y - (_initalOffsetY + (spaceBetweenMonsters * i));
            float newx = centre.x - _initalOffsetX - (spaceBetweenMonstersX * i);

            GameObject newMonster = Instantiate(monsterPrefab, targetCanvas.transform);
            newMonster.GetComponent<MonsterScript>().GenerateMonster(gameObject, i, MonsterScript._monsterType.Friendly, placeHolderSprites);
            playerMonsters.Add(newMonster);
            newMonster.transform.position = new Vector3(newx, newy, 0);
            newMonster.transform.localScale = new Vector3(7, 7, 7);
            newMonster.name = "Outline" + i;
            Image monsterImage = newMonster.GetComponentInChildren<Image>();
            monsterImage.raycastTarget = false;
            Outlines.Add(monsterImage);
        }




        //deactivates current room, main room will be activated by map script
        unloadRoom();
    }

    public void choiceHighlight()
    {
        for (int i = 0; i < Outlines.Count; i++)
        {
            if (Outlines[i].transform.parent.gameObject.GetComponent<MonsterScript>().placed)
            {
                Outlines[i].enabled = true;
            }
            else
            {
                Outlines[i].enabled = cardOutlineManager.cardIsHighlighted;
            }
        }
    }

    public void regenerateDeadFriendly(int id, float newx, float newy, string name)
    {
        //recreate and delete old monster
        GameObject newMonster = Instantiate(monsterPrefab, targetCanvas.transform);
        newMonster.GetComponent<MonsterScript>().GenerateMonster(gameObject, id, MonsterScript._monsterType.Friendly, placeHolderSprites);
        //playerMonsters.Add(newMonster);
        newMonster.transform.position = new Vector3(newx, newy, 0);
        newMonster.transform.localScale = new Vector3(7, 7, 7);
        newMonster.name = name;
        Image monsterImage = newMonster.GetComponentInChildren<Image>();
        monsterImage.raycastTarget = false;

        //uses the name to get the postion it is replaceing in the outline array
        GameObject tempDelete = playerMonsters[id];
        Outlines[id] = monsterImage;
        playerMonsters[id] = newMonster;
        Destroy(tempDelete);
        newMonster.GetComponent<MonsterScript>().loadMonster();

        //add the interactive button

        // Create a new GameObject for the Button
        GameObject buttonObject = new GameObject($"Button_Outline_{id}");
        buttonObject.transform.SetParent(Outlines[id].gameObject.transform, false); // Add as a child of the Outline
        buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Outline

        // Add required components to make it a Button
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(25, 53); // Why 25x53?

        Button buttonComponent = buttonObject.AddComponent<Button>();

        // Optional: Add an Image component to visualize the Button
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button


        // Add onClick functionality
        int temp_i = id;

        buttonComponent.onClick.AddListener(() =>
        {
            Debug.Log($"Button inside Outline {temp_i} clicked!");

            if (cardManager.currentSelectedCard != null)
            {
                Debug.Log($"Card {cardManager.currentSelectedCard.name} used on monster {temp_i}");

                // Remove card from hand
                List<GameObject> handCardObjects = cardManager.getHandCardObjects();
                foreach (GameObject cardObject in handCardObjects)
                {
                    if (cardObject == cardManager.currentSelectedCard)
                    {
                        handCardObjects.Remove(cardObject);
                        Debug.Log("Removed card from hand.");
                        break;
                    }
                }

                // Spawn card on field
                spawnPlayerCard(cardManager.currentSelectedCard.name, temp_i);

                cardManager.currentSelectedCard = null;
            }
            else
            {
                Debug.Log("No card selected to use on monster!");
            }
        });





    }

    public void interactableHighlights()
    {
        for (int i = 0; i < Outlines.Count; i++)
        {
            if (Outlines[i] == null)
            {
                Debug.LogError($"Outline at index {i} is null!");
                continue;
            }

            // Create a new GameObject for the Button
            GameObject buttonObject = new GameObject($"Button_Outline_{i}");
            buttonObject.transform.SetParent(Outlines[i].gameObject.transform, false); // Add as a child of the Outline
            buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Outline

            // Add required components to make it a Button
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(25, 53); // Why 25x53?

            Button buttonComponent = buttonObject.AddComponent<Button>();

            // Optional: Add an Image component to visualize the Button
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button


            // Add onClick functionality
            int temp_i = i;

            buttonComponent.onClick.AddListener(() =>
            {
                Debug.Log($"Button inside Outline {temp_i} clicked!");

                if (cardManager.currentSelectedCard != null)
                {
                    Debug.Log($"Card {cardManager.currentSelectedCard.name} used on monster {temp_i}");

                    // Remove card from hand
                    List<GameObject> handCardObjects = cardManager.getHandCardObjects();
                    foreach (GameObject cardObject in handCardObjects)
                    {
                        if (cardObject == cardManager.currentSelectedCard)
                        {
                            handCardObjects.Remove(cardObject);
                            Debug.Log("Removed card from hand.");
                            break;
                        }
                    }

                    // Spawn card on field
                    spawnPlayerCard(cardManager.currentSelectedCard.name, temp_i);

                    cardManager.currentSelectedCard = null;
                }
                else
                {
                    Debug.Log("No card selected to use on monster!");
                }
            });
        }
    }

    public void spawnPlayerCard(string cardName, int whichOutline)
    {
        int cardCost = 0;
        foreach (CardLibrary.CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                if (currentMana < cardData.ManaCost) { Debug.Log($"Not enough mana. Card costs {cardData.ManaCost}, player has {currentMana}"); return; } //bail if there isnt enough mana
                else { cardCost = cardData.ManaCost; break; }
            }
        }

        // Remove outline/highlight on current card in hand
        cardOutlineManager.RemoveHighlight();

        // Remove card from hand
        Destroy(cardManager.currentSelectedCard);
        cardManager.currentSelectedCard = null;

        // Set monster attributes
        Outlines[whichOutline].sprite = cardLibrary.cardImageGetter(cardName);

        playerMonsters[whichOutline].GetComponent<MonsterScript>().placed = true;

        // Setting monster's attributes using CardLibrary.CardData
        foreach(CardLibrary.CardData cardData in cardLibrary.cardDataList)
        {
            if(cardName == cardData.CardName)
            {
                //playerMonsters[whichOutline].GetComponent<MonsterScript>().setHealth(cardData.Health);
                playerMonsters[whichOutline].GetComponent<MonsterScript>().setHealth(cardData.Health);
                playerMonsters[whichOutline].GetComponent<MonsterScript>().SetAttackDamage(cardData.AttackPower);
                
                playerMonsters[whichOutline].GetComponent<MonsterScript>()._healthBar.SetActive(true);
                break;
            }
        }

        GameObject buttonObject = new GameObject($"Select_Button_{whichOutline}");
        buttonObject.transform.SetParent(playerMonsters[whichOutline].transform, false); // Add as a child of the Outline
        buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Outline

        // Add required components to make it a Button
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(25, 53); // Why 25x53?

        Button buttonComponent = buttonObject.AddComponent<Button>();

        // Optional: Add an Image component to visualize the Button
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button
        buttonComponent.onClick.AddListener(() =>
        {
            combatManager.SelectedMonster = playerMonsters[whichOutline];
            combatManager.UpdateOutlines();
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
        });

        // Decrease current mana
        currentMana -= cardCost;
        manaBar.GetComponent<Slider>().value = currentMana;
        manaText.GetComponent<TMP_Text>().text = manaBar.GetComponent<Slider>().value.ToString();
    }

    public void DebugLogButton(int i)
    {
        Debug.Log($"Button inside Outline {i} clicked!");
    }

    public void loadRoom()
    {
        gameObject.SetActive(true);
        List<GameObject> entities = new List<GameObject>(playerMonsters);
        entities.AddRange(enemyMonsters);
        foreach (GameObject a in entities)
        {
            if (a != null)
            {
                if (!a.GetComponent<MonsterScript>().dead)
                {
                    a.GetComponent<MonsterScript>().loadMonster();
                    if (a.GetComponent<MonsterScript>().getMonsterType() == MonsterScript._monsterType.Enemy)
                    {
                        a.GetComponent<MonsterScript>()._healthBar.SetActive(true);
                    }
                }
            }
        }
        backgroundImg.GetComponent<Image>().sprite = newBackgroundImage;
        int NumDeadMonsters = 0;
        foreach (GameObject en in enemyMonsters) { NumDeadMonsters += (en.GetComponent<MonsterScript>().dead) == true ? 1 : 0; }
        if (NumDeadMonsters != enemyMonsters.Count) //if not all the monster are dead the show manabar
        {
            manaBar.SetActive(true);
            manaText.SetActive(true);
            manaBar.GetComponent<Slider>().maxValue = currentMana;
            manaBar.GetComponent<Slider>().value = currentMana;
            manaText.GetComponent<TMP_Text>().text = manaBar.GetComponent<Slider>().value.ToString();
        }
        else {//else hide for room that have been cleared
            manaBar.SetActive(false);
            manaText.SetActive(false);
        }
    }

    public void unloadRoom()
    {
        gameObject.SetActive(false);
        List<GameObject> entities = new List<GameObject>(playerMonsters);
        entities.AddRange(enemyMonsters);
        foreach (GameObject a in entities)
        {
            if (a != null)
            {
                a.GetComponent<MonsterScript>().unloadMonster(); 
            }
        }
        //unshow all outlines whne room unloads
        for (int i = 0; i < Outlines.Count; i++)
        {
            if (playerMonsters[i] == null) //if the monster posiitno is null then show it as avaliable space to place maonster
            {
                Outlines[i].enabled = false;
            }
        }

        cardOutlineManager.RemoveHighlight();
        cardManager.currentSelectedCard = null;

        if (combatManager != null)
        {
            combatManager.SelectedMonster = null;
            combatManager.EnemySelectedMonster = null;
        }
    }

    //----------events used by monsters to affect other monsters-----------------
    public void attackEvent(int AttackingID, int AttackedID, float Damage) {
        List<GameObject> entities = new List<GameObject>(playerMonsters);
        entities.AddRange(enemyMonsters);
        float atkDamage = entities[AttackingID].GetComponent<MonsterScript>().getAttackDamage();
        entities[AttackedID].GetComponent<MonsterScript>().takeDamage(atkDamage);
        DamageVisualizer newVisualizer = new DamageVisualizer();
    }
    public void attackBuffEvent(int BuffingID, float buff) {
        List<GameObject> entities = new List<GameObject>(playerMonsters);
        entities.AddRange(enemyMonsters);
        entities[BuffingID].GetComponent<MonsterScript>().attackBuff(buff);
    }
    public void healEvent(int healingID, float health)
    {
        List<GameObject> entities = new List<GameObject>(playerMonsters);
        entities.AddRange(enemyMonsters);
        entities[healingID].GetComponent<MonsterScript>().heal(health);
    }

    //---------------------------fucntions used by mosnters to get information about other monsters
    //returns array with all of enemy monsters
    public List<GameObject> returnEnemies() {
        return enemyMonsters;
    }

    //returns array with all of the player's spawned monsters
    public List<GameObject> returnPlayerMonsters() {
        return playerMonsters;
    }

    public CardManager returnCardManager() {
        return cardManager;
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < Outlines.Count; i++)
        {
            Outlines[i].enabled = false;
        }
        combatManager = _combatManager.GetComponent<CombatManager>();
        interactableHighlights();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(backgroundImg.transform.localScale.y / backgroundImg.GetComponent<SpriteRenderer>().sprite.bounds.size.y);
        choiceHighlight();

    }
}
