using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Economy;
using RedHomestead.Geography;
using RedHomestead.Simulation;

public static class Corporations {

    public static List<Vendor> Wholesalers = new List<Vendor>()
    {
        new Vendor()
        {
            //Specializes in: commodity
            Name = "MonsMart",
            AvailableDelivery = DeliveryType.Drop | DeliveryType.Lander | DeliveryType.Rover,
            Location = new LatLong()
            {
                LatitudeDegrees = 18.65f,
                LongitudeDegrees = 226.2f
            },
            Stock = new List<Stock>()
            {
                new Stock()
                {
                    ListPrice = 1000,
                    StockAvailable = 100,
                    Matter = Matter.Steel
                },
                new Stock()
                {
                    ListPrice = 800,
                    StockAvailable = 100,
                    Matter = Matter.Copper
                },
                new Stock()
                {
                    ListPrice = 100,
                    StockAvailable = 100,
                    Matter = Matter.RationMeal
                },
                new Stock()
                {
                    ListPrice = 500,
                    StockAvailable = 100,
                    Matter = Matter.Hydrogen
                },
                new Stock()
                {
                    ListPrice = 2000,
                    StockAvailable = 50,
                    Matter = Matter.SiliconWafers
                }
            }
        },
        new Vendor()
        {
            //Specializes in: commodity & high tech
            Name = "Bradbury & Co.",
            AvailableDelivery = DeliveryType.Drop | DeliveryType.Lander | DeliveryType.Rover,
            Location = new LatLong()
            {
                LatitudeDegrees = -4.59f,
                LongitudeDegrees = 137.44f
            },
            Stock = new List<Stock>()
            {
                new Stock()
                {
                    ListPrice = 900,
                    StockAvailable = 50,
                    Matter = Matter.Steel
                },
                new Stock()
                {
                    ListPrice = 100,
                    StockAvailable = 100,
                    Matter = Matter.RationMeal
                },
                new Stock()
                {
                    ListPrice = 100,
                    StockAvailable = 100,
                    Matter = Matter.Hydrogen
                },
                new Stock()
                {
                    ListPrice = 2000,
                    StockAvailable = 100,
                    Matter = Matter.SiliconWafers
                },
                new Stock()
                {
                    ListPrice = 300,
                    StockAvailable = 100,
                    Matter = Matter.Glass
                },
                new Stock()
                {
                    ListPrice = 200,
                    StockAvailable = 100,
                    Matter = Matter.Canvas
                }
            }
        },
        new Vendor()
        {
            //Specializes in: bio-anything
            Name = "Utopia Biochem",
            AvailableDelivery = DeliveryType.Drop | DeliveryType.Lander | DeliveryType.Rover,
            Location = new LatLong()
            {
                LatitudeDegrees = 30f,
                LongitudeDegrees = 90f
            },
            Stock = new List<Stock>()
            {
                new Stock()
                {
                    ListPrice = 100,
                    StockAvailable = 100,
                    Matter = Matter.Hydrogen
                },
                new Stock()
                {
                    ListPrice = 200,
                    StockAvailable = 100,
                    Matter = Matter.Canvas
                },
                new Stock()
                {
                    ListPrice = 500,
                    StockAvailable = 100,
                    Matter = Matter.Biomass
                }
            }
        }
    };
}
