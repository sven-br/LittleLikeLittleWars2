﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Star : MonoBehaviour, IMessageReceiver
{
    [SerializeField] private int units = 0;
    [SerializeField] private StarOwner owner = StarOwner.neutral;
    [SerializeField] private GameObject selectedOutline = null;
    [SerializeField] private Star[] neighbors = null;
    private bool registered = false;

    Dictionary<StarOwner, Color> colormapping = new Dictionary<StarOwner, Color>()
{
    { StarOwner.player0, Color.red },
    { StarOwner.player1, Color.yellow },
    { StarOwner.player2, Color.blue },
    { StarOwner.player3, Color.green },
    { StarOwner.neutral, Color.grey }
};

    public enum StarOwner
    {
        player0,
        player1,
        player2,
        player3,
        neutral,
    }

    private void SetOwner(StarOwner owner)
    {
        this.owner = owner;
        SetColour();

        var message = MessageProvider.GetMessage<StarOwnerChangedMessage>();
        MessageManager.SendMessage(message);
    }

    public StarOwner getOwner()
    {
        return owner;
    }

    public StarOwner Owner
    {
        get { return owner; }
        private set
        {
            owner = value;
            SetColour();

            var message = MessageProvider.GetMessage<StarOwnerChangedMessage>();
            MessageManager.SendMessage(message);
        }
    }

    public int Units
    {
        get { return units; }
        private set { units = value; UpdateText(); }
    }

    private void SetColour()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.color = colormapping[owner];
    }

    public bool HasNeighbor(Star other)
    {
        foreach (var neighbor in neighbors)
        {
            if (neighbor == other)
                return true;
        }
        return false;
    }
    void Start()
    {
        UpdateText();
        SetColour();

        MessageManager.StartReceivingMessage<UnitTransferMessage>(this);
        MessageManager.StartReceivingMessage<StarSelectedMessage>(this);
        MessageManager.StartReceivingMessage<AllStarsUnselectedMessage>(this);
        MessageManager.StartReceivingMessage<TickMessage>(this);
    }

    void Update()
    {
        if (!registered)
        {
            registered = true;
            var message = MessageProvider.GetMessage<RegisterStarMessage>();
            message.star = this;
            MessageManager.SendMessage(message);
        }
    }

    void UpdateText()
    {
        var text = GetComponentInChildren<TextMeshProUGUI>();
        text.text = Units.ToString();
    }

    void UpdateSelected(bool selected)
    {
        selectedOutline.SetActive(selected);
    }

    void OnMouseDown()
    {
        var message = MessageProvider.GetMessage<StarClickedMessage>();
        message.star = this;
        message.owner = owner;
        MessageManager.SendMessage(message);
    }

    private void IncreaseUnits(int amount)
    {
        Units += amount;
        Debug.Log("star received " + amount + " units");
    }

    private void DecreaseUnits(int amount)
    {
        Units -= amount;
        Debug.Log("star lost " + amount + " units");
    }

    void IMessageReceiver.MessageReceived(Message message)
    {
        if (message is UnitTransferMessage)
        {
            var unitTransferMessage = message as UnitTransferMessage;
            if (unitTransferMessage.sender == this)
            {
                DecreaseUnits(unitTransferMessage.amount);
            }
            if (unitTransferMessage.receiver == this)
            {
                if (unitTransferMessage.owner == this.owner)
                {
                    IncreaseUnits(unitTransferMessage.amount);
                }
                else
                {
                    int diff = this.units - unitTransferMessage.amount;

                    if (diff > 0)
                    {
                        DecreaseUnits(unitTransferMessage.amount);
                    }
                    else if (diff < 0)
                    {
                        SetOwner(unitTransferMessage.owner);
                        this.Units = (-diff);
                    }
                    else
                    {
                        DecreaseUnits(unitTransferMessage.amount);
                        SetOwner(StarOwner.neutral);
                    }
                        
                }
                
            }
        }

        else if (message is StarSelectedMessage)
        {
            var starSelectedMessage = message as StarSelectedMessage;
            if ((Object)starSelectedMessage.star == this)
            {
                UpdateSelected(true);
            }
        }

        else if (message is AllStarsUnselectedMessage)
        {
            var allStarsUnselectedMessage = message as AllStarsUnselectedMessage;
            UpdateSelected(false);
        }

        else if (message is TickMessage)
        {
            if (owner != StarOwner.neutral)
                IncreaseUnits(1);
        }
    }
}
