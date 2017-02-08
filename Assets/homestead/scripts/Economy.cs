using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Geography;
using RedHomestead.Simulation;
using System.Collections.Generic;
using System.Linq;

namespace RedHomestead.Economy{
    public class PersistentPlayer
    {
        public int BankAccount = 100000;
        public List<Order> EnRouteOrders = new List<Order>();
    }

    [Flags]
    public enum DeliveryType { Rover, Lander, Drop }

    public static class EconomyExtensions
    {
        public static bool IsSet(this DeliveryType value, DeliveryType flag)
        {
            return (value & flag) == flag;
        }
        
        //rover shipping type should be based on miles, not weight
        //lander/drop should be some fraction miles vs weight
        public static int DollarsPerKilogramPerKilometer(this DeliveryType dt, float kilograms, float distanceKilometers)
        {
            switch (dt)
            {
                //this is just kms
                default:
                case DeliveryType.Rover:
                    return 10 * (int)distanceKilometers;
                case DeliveryType.Drop:
                    return 100 * (int)kilograms;
                case DeliveryType.Lander:
                    return 5 * (int)distanceKilometers + 10 * (int)kilograms;
            }
        }

        public static float ShippingTimeHours(this DeliveryType dt, float distanceKilometers)
        {
            switch (dt)
            {
                default:
                case DeliveryType.Rover:
                    //1 sol for scheduling problems and start/stops on way
                    //48.3 km/h (30 mph) driving speed
                    //for 24.7h each day
                    return 24.7f + (distanceKilometers / (48.3f));
                case DeliveryType.Drop:
                    //0 sol (fast loading)
                    //600 km/h suborbital speed
                    //for 24.7h each day
                    return distanceKilometers / (600);
                case DeliveryType.Lander:
                    //.5 sol for loading and fueling
                    //300 km/h suborbital speed
                    //for 24.7h each day
                    return 12.35f + (distanceKilometers / (300));
            }
        }

        public static int MaximumMass(this DeliveryType dt)
        {
            switch (dt)
            {
                case DeliveryType.Drop:
                    return 5000;
                case DeliveryType.Lander:
                    return 10000;
                default:
                case DeliveryType.Rover:
                    return 20000;
            }
        }

        public static int MaximumVolume(this DeliveryType dt)
        {
            switch (dt)
            {
                case DeliveryType.Drop:
                    return 4;
                case DeliveryType.Lander:
                    return 6;
                default:
                case DeliveryType.Rover:
                    return 8;
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
                    _playerDist = this.Location.DistanceKilometers(PlayerGeography.BaseLocation);

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

    [Serializable]
    public struct DayHourStamp
    {
        public int Sol;
        public int Hour;

        public override string ToString()
        {
            return String.Format("{0}{1:} Sols", Sol, Hour > 1 ? String.Format("{0:.0}", (double)(Hour / SunOrbit.MartianHoursPerDay)) : "");
        }

        internal float HoursSinceSol0
        {
            get
            {
                return Sol * SunOrbit.MartianHoursPerDay + Hour;
            }
        }

        internal DayHourStamp DayHoursIntoFuture(DayHourStamp futureDayHour)
        {
            return new DayHourStamp()
            {
                Sol = futureDayHour.Sol - this.Sol,
                Hour = futureDayHour.Hour - this.Hour
            };
        }

        internal static DayHourStamp Now()
        {
            return new DayHourStamp()
            {
                Sol = SunOrbit.Instance.CurrentSol,
                Hour = Mathf.RoundToInt(SunOrbit.Instance.CurrentHour)
            };
        }
        
        internal static DayHourStamp FromFractionalHours(int startSol, float startHour, float additionalFractionalHours)
        {
            int additionalDays = (int)Math.Truncate(additionalFractionalHours / SunOrbit.MartianHoursPerDay);

            additionalFractionalHours = additionalFractionalHours - (additionalDays * SunOrbit.MartianHoursPerDay);

            int newSol = startSol + additionalDays;

            float newHour = startHour + additionalFractionalHours;

            if (newHour >= SunOrbit.MartianHoursPerDay)
            {
                newSol++;
                newHour -= SunOrbit.MartianHoursPerDay;
            }

            return new DayHourStamp()
            {
                Sol = newSol,
                Hour = Mathf.CeilToInt(additionalFractionalHours)
            };
        }
    }

    public class Order
    {
        public Dictionary<Matter, int> LineItemUnits = new Dictionary<Matter, int>();
        public DayHourStamp ETA, Ordered;
        private DeliveryType via;
        public DeliveryType Via
        {
            get { return via; }
            //todo: bug - selecting delivery will get around mass/volume limits
            set {
                via = value;
                RecalcVolumeMassShipping();
                RecalcDeliveryTime();
            }
        }
        public string VendorName;
        public Vendor Vendor;
        public int MatterCost;
        public int ShippingCost;
        public int GrandTotal
        {
            get
            {
                return ShippingCost + MatterCost;
            }
        }

        public bool TryAddLineItems(Stock s, int amount)
        {
            bool hasKey = LineItemUnits.ContainsKey(s.Matter);

            if ((hasKey && LineItemUnits[s.Matter] + amount > -1) || (!hasKey && amount > -1))
            {
                if (hasKey)
                    LineItemUnits[s.Matter] += amount;
                else
                    LineItemUnits[s.Matter] = amount;

                MatterCost += s.ListPrice * amount;
                
                RecalcVolumeMassShipping();

                //validate
                if (amount > 0)
                {
                    if (TotalVolume > Via.MaximumVolume() || TotalMass > Via.MaximumMass())
                    {
                        TryAddLineItems(s, -amount);
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
        
        public float TotalVolume { get; set; }
        public float TotalMass { get; set; }

        private void RecalcVolumeMassShipping()
        {
            TotalVolume = LineItemUnits.Sum(x => x.Key.BaseCubicMeters() * x.Value);
            TotalMass = LineItemUnits.Sum(x => x.Value * x.Key.Kilograms());
            ShippingCost = Via.DollarsPerKilogramPerKilometer(TotalMass, this.Vendor.DistanceFromPlayerKilometersRounded);
        }

        private void RecalcDeliveryTime()
        {
            ETA = DayHourStamp.FromFractionalHours(SunOrbit.Instance.CurrentSol, SunOrbit.Instance.CurrentHour, 
                this.Via.ShippingTimeHours(this.Vendor.DistanceFromPlayerKilometersRounded));
        }

        public void FinalizeOrder()
        {
            RecalcDeliveryTime();
            Ordered = DayHourStamp.Now();
            EconomyManager.Instance.Player.BankAccount -= GrandTotal;
        }

        internal Matter[] GetKeyArray()
        {
            Matter[] result = new Matter[this.LineItemUnits.Keys.Count];
            this.LineItemUnits.Keys.CopyTo(result, 0);
            return result;
        }

        public string ETAFromNow()
        {
            return DayHourStamp.Now().DayHoursIntoFuture(ETA).ToString();
        }
        
        public float DeliveryWaitPercentage()
        {
            float elapsed = SunOrbit.Instance.HoursSinceSol0 - this.Ordered.HoursSinceSol0;
            float duration = this.ETA.HoursSinceSol0 - this.Ordered.HoursSinceSol0;

            return elapsed / duration;
        }
    }
}
