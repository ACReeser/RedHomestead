using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Economy;
using RedHomestead.Geography;

public static class Corporations{

    public static List<Vendor> Wholesalers = new List<Vendor>()
    {
        new Vendor()
        {
            Name = "MonsMart",
            AvailableDelivery = DeliveryType.Drop & DeliveryType.Lander & DeliveryType.Rover,
            Location = new LatLong()
            {
                LatitudeDegrees = 18.65f,
                LongitudeDegrees = 226.2f
            },
            Stock = new List<Stock>()
            {

            }
        },
        new Vendor()
        {
            Name = "Bradbury & Co.",
            AvailableDelivery = DeliveryType.Drop & DeliveryType.Lander & DeliveryType.Rover,
            Location = new LatLong()
            {
                LatitudeDegrees = -4.59f,
                LongitudeDegrees = 137.44f
            },
            Stock = new List<Stock>()
            {

            }
        }
    };
}
