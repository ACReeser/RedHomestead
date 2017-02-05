using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Geography;
using RedHomestead.Simulation;
using System.Collections.Generic;
using System.Linq;

namespace RedHomestead.Economy{
    public class PlayerAccount
    {
        public int BankAccount = 100000;
    }

    [Flags]
    public enum DeliveryType { Rover, Lander, Drop }

    public static class EnumExtensions
    {
        public static bool IsSet(this DeliveryType value, DeliveryType flag)
        {
            return (value & flag) == flag;
        }

        public static int DollarsPerKilogram(this DeliveryType dt, float distanceKilometers)
        {
            switch (dt)
            {
                default:
                case DeliveryType.Rover:
                    return 10 * (int)distanceKilometers;
                case DeliveryType.Drop:
                    return 60 * (int)distanceKilometers;
                case DeliveryType.Lander:
                    return 30 * (int)distanceKilometers;
            }
        }

        public static float ShippingTimeSols(this DeliveryType dt, float distanceKilometers)
        {
            switch (dt)
            {
                default:
                case DeliveryType.Rover:
                    //1 sol for scheduling problems and start/stops on way
                    //48.3 km/h (30 mph) driving speed
                    //for 24.7h each day
                    return 1 + (distanceKilometers / (48.3f * 24.7f));
                case DeliveryType.Drop:
                    //0 sol (fast loading)
                    //600 km/h suborbital speed
                    //for 24.7h each day
                    return distanceKilometers / (600 * 24.6f);
                case DeliveryType.Lander:
                    //.5 sol for loading and fueling
                    //300 km/h suborbital speed
                    //for 24.7h each day
                    return .5f + (distanceKilometers / (300 * 24.6f));
            }
        }
    }

    public class Stock
    {
        public Matter Matter { get; set; }
        public int ListPrice { get; set; }
        public int StockAvailable { get; set; }
        public string Name {
            get {
                return Matter.ToString();
            }
        }
    }

    public class Vendor
    {
        public string Name;
        public LatLong Location;
        private float? _playerDist;
        public float DistanceFromPlayerKilometers
        {
            get
            {
                if (!_playerDist.HasValue)
                    _playerDist = this.Location.DistanceMeters(PlayerGeography.BaseLocation);

                return _playerDist.Value;
            }
        }

        public int DistanceFromPlayerKilometersRounded
        {
            get
            {
                return Mathf.RoundToInt(DistanceFromPlayerKilometers);
            }
        }

        public DeliveryType AvailableDelivery;
        public List<Stock> Stock;
        public int TotalUnits
        {
            get
            {
                return Stock.Sum(s => s.StockAvailable);
            }
        }
    }

    public class Order
    {
        public Dictionary<Matter, int> LineItemUnits = new Dictionary<Matter, int>();
        public int DeliverySol;
        public int DeliveryHour;
        public DeliveryType Via;
        public string VendorName;
        public Vendor Vendor;
    }
}
