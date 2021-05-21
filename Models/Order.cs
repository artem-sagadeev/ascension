﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public enum Status
    {
        NotPaid,
        Paid,
        Packing,
        Delivering,
        Delivered,
        Cancelled
    }

    public enum DeliveryType
    {
        Delivery,
        Pickup
    }

    public class Order
    {
        public int Id { get; set; }

        public DateTime OrderTime { get; set; }

        
        public int UserId { get; set; }
        
        public string RecipientName { get; set; }
        
        public string RecipientSurname { get; set; }
        
        public string RecipientEmail { get; set; }

        public int Amount { get; set; }

        public Status Status { get; set; } // NotPaid ,Paid ,Packing, Delivering, Delivered.

        public DeliveryType DeliveryType { get; set; } // Delivery, SelfTake

        public string DeliveryAddress { get; set; } // If DeliveryType == "Delivery"
        public List<ProductLine> ProductLines { get; set; }
        
        [NotMapped]
        public List<int> ProductLineIds { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, UserId {UserId}";
        }

        public Order()
        {
        }
    }
}