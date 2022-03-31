using System.Linq;
using UnityEngine;

/// <summary>
/// Class responsible for card management
/// </summary>
public class CardManager : MonoBehaviour
{
    [SerializeField]
    private Sprite[] sprites; // Sprites (textures) array. Number of cards is equal to number of sprites
    [SerializeField]
    private GameObject cardPrefab; // Template of card
    [SerializeField]
    private float heightCarousel; // Height of carousel
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float gapXFrac = 0.2f; // Gap between cards. Percentage of card's size 
    [SerializeField]
    [Range(0.5f, 1.0f)]
    private float scaleOcclusion = 0.8f; // scale applied in side cards
    [SerializeField]
    [Range(0.05f, 0.5f)]
    private float dragThreshold = 0.25f; // how big are the drag to change the focus card. Based on screen width

    private Transform[] cards; // Transform references of the cards
    private int[] cardsOrder; // Array concatenated with cards array. Card's order in carousel
    private Vector2 cardSize; // Card's size
    private float gapX; // Gap between cards
    private float startPosX; // position of the first card

    private bool drag = false; // is dragging?
    private float screenWidth; // screen width
    private float lastTouchPosDrag = 0f; // last click position
    private float deltaPosDrag = 0f; // click position variation in relation to the last update
    private float deltaPosDragAc = 0f; // cumulative variation of click position (unsigned) in relation to last update

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        CreatePanels();
        InitPanels(0);
    }

    void Update()
    {

#if (UNITY_EDITOR || UNITY_STANDALONE)
        if (Input.GetMouseButtonDown(0))
        {
            // Begin of the drag
            lastTouchPosDrag = Input.mousePosition.x;
            deltaPosDrag = 0f;
            deltaPosDragAc = 0f;
            drag = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // End of the drag
            drag = false;

            // Analizes if it's just a click
            if ((deltaPosDragAc / screenWidth) * (cardSize.x + gapX) < 0.1f)
            {
                Collider2D cardCollider = Physics2D.OverlapPoint(cam.ScreenToWorldPoint(Input.mousePosition));            

                if (cardCollider != null)
                {
                    IClickListener card = cardCollider.GetComponent<IClickListener>();
                    if (card.id == cardsOrder[cards.Length / 2])
                    {
                        card.OnClick();
                    }
                }
            }
            else if (Mathf.Abs((deltaPosDrag / screenWidth)) >= dragThreshold)
            {
                // If the drag was bigger than dragThreshold, shifts the order of the cards
                ShiftPanels(deltaPosDrag > 0);
            }
        }

        if (drag)
        {
            // Update drag variables. Moves the card following the drag.
            deltaPosDrag += (Input.mousePosition.x - lastTouchPosDrag);
            deltaPosDragAc += Mathf.Abs(Input.mousePosition.x - lastTouchPosDrag);
            lastTouchPosDrag = Input.mousePosition.x;
            DisplaceCards((deltaPosDrag / screenWidth) * (cardSize.x + gapX));
        }
        else
        {
            // Positions in the center of the current focus card
            DisplaceCards(0);
        }
#elif (UNITY_ANDROID || UNITY_IOS)
        if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    lastTouchPosDrag = touch.position.x;
                    deltaPosDrag = 0f;
                    deltaPosDragAc = 0f;
                    drag = true;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    drag = false;
                    if ((deltaPosDragAc / screenWidth) * (cardSize.x + gapX) < 0.1f)
                    {
                        Collider2D cardCollider = Physics2D.OverlapPoint(cam.ScreenToWorldPoint(touch.position));            

                        if (cardCollider != null)
                        {                         
                            IClickListener card = cardCollider.GetComponent<IClickListener>();
                            if (card.id == cardsOrder[cards.Length / 2])
                            {
                                card.OnClick();
                            }
                        }
                    }
                    else if (Mathf.Abs((deltaPosDrag / screenWidth)) >= dragThreshold)
                    {
                        ShiftPanels(deltaPosDrag > 0);
                    }
                }
            }

            if (drag)
            {
                Touch touch = Input.GetTouch(0);
                deltaPosDrag += (touch.position.x - lastTouchPosDrag);
                deltaPosDragAc += Mathf.Abs(touch.position.x - lastTouchPosDrag);
                lastTouchPosDrag = touch.position.x;
                DisplaceCards((deltaPosDrag / screenWidth) * (cardSize.x + gapX));
            }
            else
            {
                DisplaceCards(0);
            }
#endif
    }

    /// <summary>
    /// Moves the card following the drag
    /// </summary>
    /// <param name="centerPosX">Offset applied in all cards</param>
    void DisplaceCards(float centerPosX)
    {
        float posX = startPosX;
        for (int i = 0; i < cards.Length; i++)
        {
            cards[cardsOrder[i]].position = Vector3.Lerp(cards[cardsOrder[i]].position, new Vector3(posX + centerPosX, heightCarousel, 0), 10 * Time.deltaTime);
            posX += (cardSize.x + gapX);

            float scaleX = CalculateScale(cards[cardsOrder[i]].position.x);
            cards[cardsOrder[i]].localScale = Vector2.one * scaleX;
        }
    }

    /// <summary>
    /// Calculates the scale of the card. The scale is based on position.
    /// </summary>
    /// <param name="posX">Card x-position</param>
    /// <returns></returns>
    float CalculateScale(float posX)
    {
        return Mathf.Clamp((1 - Mathf.Abs(posX) / (cardSize.x + gapX) * (1 - scaleOcclusion)), 0.5f, 1f);
    }

    /// <summary>
    /// Create and initialize cards based on sprite array
    /// </summary>
    void CreatePanels()
    {
        for(int i = 0; i < sprites.Length; i++)
        {
            GameObject go = Instantiate(cardPrefab, transform);
            SpriteRenderer spr = go.GetComponent<SpriteRenderer>();
            spr.sprite = sprites[0];
            spr.color = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0.5f, 1f));
            go.GetComponent<CardHandle>().id = i;
        }
    }

    /// <summary>
    /// Initialize Carousel
    /// </summary>
    /// <param name="centerMode">Focus card index</param>
    void InitPanels(int centerMode)
    {
        cards = transform.Cast<Transform>().ToArray();
        cardsOrder = new int[cards.Length];
        SpriteRenderer panelSpr = cards[0].GetComponent<SpriteRenderer>();
        cardSize = panelSpr.bounds.size;
        gapX = cardSize.x * gapXFrac;
        screenWidth = (float)Screen.width;

        int centerOffset = cards.Length / 2;

        for (int i = 0; i < cards.Length; i++)
        {
            cardsOrder[(i + centerOffset) % cards.Length] = (i + centerMode) % cards.Length;
        }

        startPosX = -centerOffset * (cardSize.x + gapX);

        float posX = startPosX;
        for (int i = 0; i < cards.Length; i++)
        {
            cards[cardsOrder[i]].position = new Vector3(posX, heightCarousel, 0);
            posX += (cardSize.x + gapX);
            float scaleX = CalculateScale(cards[cardsOrder[i]].position.x);
            cards[cardsOrder[i]].localScale = new Vector3(scaleX, scaleX, 0);
        }
    }

    /// <summary>
    /// Shifts the order of the cards. Called on click end if the drag was bigger than dragThreshold.
    /// </summarsy>
    /// <param name="isRight">The movement direction is right?</param>
    void ShiftPanels(bool isRight)
    {
        int[] arrayAux = (int[])cardsOrder.Clone();

        if (isRight)
        {
            cardsOrder[0] = arrayAux[arrayAux.Length - 1];
            cards[arrayAux[arrayAux.Length - 1]].position = cards[arrayAux[0]].position;
            System.Array.Copy(arrayAux, 0, cardsOrder, 1, arrayAux.Length - 1);
        }
        else
        {
            cardsOrder[arrayAux.Length - 1] = arrayAux[0];
            cards[arrayAux[0]].position = cards[arrayAux[arrayAux.Length - 1]].position;
            System.Array.Copy(arrayAux, 1, cardsOrder, 0, arrayAux.Length - 1);
        }
    }
}