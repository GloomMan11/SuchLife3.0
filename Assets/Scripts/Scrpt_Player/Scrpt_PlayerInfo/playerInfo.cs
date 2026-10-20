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

    //used to make weapons rotate towards the mouse slightly
    private float currentAngle;
    private float angleVelocity;
    private float defaultLocalAngle;
    public bool isAnimated = false;
    [SerializeField] private float maxTiltAngle = 10f;
    [SerializeField] private float restLocalAngle = 180f;

    //Unique behavior of NOT being replicated between clients
    //Server grabs GUID from a tcp request but never shares it to other clients
    public Guid playerGuid;

    private void Start()
    {
        Player = gameObject;
        Transform held = currentlyHeld.transform;
        currentAngle = held.eulerAngles.z;
        defaultLocalAngle = held.localEulerAngles.z; // resting angle from the prefab
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
        if (CurrentlyHeldItem == null)
        {
            return;
        }

        Transform held = currentlyHeld.transform;
        float parentAngle = held.parent != null ? held.parent.eulerAngles.z : 0f;
        float restAngle = parentAngle + restLocalAngle;

        if (CurrentlyHeldItem.useFlag == 4 && !isAnimated)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = mouseWorld - held.position;
            float mouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

            float offsetFromRest = Mathf.DeltaAngle(restAngle, mouseAngle);
            float targetAngle = restAngle + Mathf.Clamp(offsetFromRest, -maxTiltAngle, maxTiltAngle);
            
            currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angleVelocity, 0.05f);
            held.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
        else if (CurrentlyHeldItem.useFlag == 4)
        {
            held.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
        else
        {
            // Resting angle, relative to the player's orientation
            held.localRotation = Quaternion.Euler(0, 0, restLocalAngle);
            currentAngle = restAngle;
            angleVelocity = 0f;
        }
    }
}
