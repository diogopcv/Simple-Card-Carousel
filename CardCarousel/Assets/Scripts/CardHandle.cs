using UnityEngine;
/// <summary>
/// Class responsible for Card click behaviour 
/// </summary>
public class CardHandle : MonoBehaviour, IClickListener
{
    private int _id;

    public int id { get => _id; set => _id = value; }

    void Start()
    {
        BoxCollider2D collider2D = GetComponent<BoxCollider2D>();
        SpriteRenderer spr = GetComponent<SpriteRenderer>();
        collider2D.size = new Vector2(spr.size.x, spr.size.y);
    }

    public void OnClick()
    {
        print("Do Something in card number " + id);
    }
}