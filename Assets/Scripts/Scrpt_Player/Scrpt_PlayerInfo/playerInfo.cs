using System;
using System.Net.Sockets;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    //data class to store attributes concerning the player so multiple scripts can access
    //put attributes like health in here instead of the health class

    //held item info
    public GameObject Player { get; private set; }
    private string username;
    private string heldType;
    private Item _heldItem;
    public int heldItemIndex = 0;
    //public PlayerItemHoldView heldItemView;
    [SerializeField] GameObject currentlyHeld; //currently held item

    public Item CurrentlyHeldItem;

    //Unique behavior of NOT being replicated between clients
    //Server grabs GUID from a tcp request but never shares it to other clients
    public Guid playerGuid;

    private void Start()
    {
        Player = gameObject;
        //heldItemView = GameObject.Find("ViewInHand").GetComponent<PlayerItemHoldView>();
    }
    public Item HeldItem
    {
        get
        {
            return _heldItem;
        }

        set
        {
            //Debug.Log("change held item!");
            _heldItem = value;
            //when held item is changed, change the renderer accordingly
            playerHandUpdate(_heldItem);
        }
    }

    private void playerHandUpdate(Item newItem)
    {
        CurrentlyHeldItem = newItem;

        //switch rotation when that gets added
        if (newItem != null)
        {
            currentlyHeld.GetComponent<SpriteRenderer>().sprite = CurrentlyHeldItem.icon;
        }
        else
        {
            currentlyHeld.GetComponent<SpriteRenderer>().sprite = null;
        }
    }


    public void Update()
    {
        currentlyHeld.GetComponent<Transform>().Rotate(new Vector3(0, 0, 1), 90 * Time.deltaTime);
    }

}
