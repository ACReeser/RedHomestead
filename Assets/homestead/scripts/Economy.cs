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
        private const float MinimumDeliveryTimeHours = 2f;

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
            float hours = 0f;
            switch (dt)
            {
                default:
                case DeliveryType.Rover:
                    //1 sol for scheduling problems and start/stops on way
                    //48.3 km/h (30 mph) driving speed
                    //for 24.7h each day
                    hours = 24.7f + (distanceKilometers / (48.3f));
                    break;
                case DeliveryType.Lander:
                    //.5 sol for loading and fueling
                    //300 km/h suborbital speed
                    //for 24.7h each day
                    hours = 12.35f + (distanceKilometers / (300));
                    break;
                case DeliveryType.Drop:
                    //0 sol (fast loading)
                    //600 km/h suborbital speed
                    //for 24.7h each day
                    hours = distanceKilometers / (600);
                    break;
            }
            return Mathf.Max(hours, MinimumDeliveryTimeHours);//no delivery can ever take less than 2 hours!
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

    public interface ISolsAndHours
    {
        int Sol { get; set; }
        int Hour { get; set; }
    }

    [Serializable]
    public struct SolsAndHours : ISolsAndHours
    {
        public int Sol { get; set; }
        public int Hour { get; set; }

        public SolsAndHours(int sols, int hours)
        {
            this.Sol = sols;
            this.Hour = hours;
        }

        public override string ToString()
        {
            if (Sol < 1)
            {
                return String.Format("{0} Hours", Hour);
            }
            else
            {
                return String.Format("{0}{1:} Sols", Sol, Hour > 1 ? String.Format("{0:.0}", (double)(Hour / SunOrbit.MartianHoursPerDay)) : "");
            }
        }

        internal static SolsAndHours SolHoursFromNow(float additionalFractionalHours)
        {
            var cheat = SolHourStamp.FromFractionalHours(0, 0, additionalFractionalHours);
            return new SolsAndHours(cheat.Sol, cheat.Hour);
        }
    }

    [Serializable]
    public struct SolHourStamp : ISolsAndHours
    {
        public int Sol { get; set; }
        public int Hour { get; set; }

        public override string ToString()
        {
            return String.Format("Sol {0} Hour {1}", Sol, Hour);
        }

        internal float HoursSinceSol0
        {
            get
            {
                return Sol * SunOrbit.MartianHoursPerDay + Hour;
            }
        }

        internal SolsAndHours SolHoursIntoFuture(SolHourStamp futureDayHour)
        {
            UnityEngine.Debug.Log(String.Format("Getting future from Sol {0} and Hour {1} to Sol {2} and Hour {3}", this.Sol, this.Hour, futureDayHour.Sol, futureDayHour.Hour));

            int daysInFuture = futureDayHour.Sol - this.Sol;
            float hoursInFuture = futureDayHour.Hour - this.Hour;

            UnityEngine.Debug.Log(String.Format("{0} sols and {1} hours in future", daysInFuture, hoursInFuture));

            if (daysInFuture == 1 && hoursInFuture < 0)
            {
                return new SolsAndHours(0, Mathf.RoundToInt(SunOrbit.MartianHoursPerDay + hoursInFuture));
            }
            else
            {
                return new SolsAndHours(daysInFuture, Mathf.RoundToInt(hoursInFuture));
            }
        }

        internal static SolHourStamp Now()
        {
            return new SolHourStamp()
            {
                Sol = SunOrbit.Instance.CurrentSol,
                Hour = Mathf.RoundToInt(SunOrbit.Instance.CurrentHour)
            };
        }
        
        internal static SolHourStamp FromFractionalHours(int startSol, float startHour, float additionalFractionalHours)
        {
            int additionalDays = (int)Math.Truncate(additionalFractionalHours / SunOrbit.MartianHoursPerDay);

            UnityEngine.Debug.Log(additionalDays + " additional days to Sol "+startSol);

            additionalFractionalHours = additionalFractionalHours - (additionalDays * SunOrbit.MartianHoursPerDay);

            UnityEngine.Debug.Log(additionalFractionalHours + " additional hours to Hour "+startHour);

            int newSol = startSol + additionalDays;

            float newHour = startHour + additionalFractionalHours;

            UnityEngine.Debug.Log("will be Sol " + newSol + " Hour " + newHour);

            if (newHour >= SunOrbit.MartianHoursPerDay)
            {
                newSol++;
                newHour -= SunOrbit.MartianHoursPerDay;
                UnityEngine.Debug.Log("rolling over to Sol "+newSol+ " Hour "+newHour);
            }
            
            UnityEngine.Debug.Log("will be Sol " + newSol + " Hour " + Mathf.CeilToInt(newHour));

            return new SolHourStamp()
            {
                Sol = newSol,
                Hour = Mathf.CeilToInt(newHour)
            };
        }
    }

    public class Order
    {
        public Dictionary<Matter, int> LineItemUnits = new Dictionary<Matter, int>();
        public SolHourStamp ETA, Ordered;
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
            ETA = SolHourStamp.FromFractionalHours(SunOrbit.Instance.CurrentSol, SunOrbit.Instance.CurrentHour, 
                this.Via.ShippingTimeHours(this.Vendor.DistanceFromPlayerKilometersRounded));
            UnityEngine.Debug.Log(ETA);
        }

        public void FinalizeOrder()
        {
            UnityEngine.Debug.Log("Finalizing order");
            RecalcDeliveryTime();
            Ordered = SolHourStamp.Now();
            EconomyManager.Instance.Player.BankAccount -= GrandTotal;
        }

        internal Matter[] GetKeyArray()
        {
            Matter[] result = new Matter[this.LineItemUnits.Keys.Count];
            this.LineItemUnits.Keys.CopyTo(result, 0);
            return result;
        }

        public string TimeUntilETA()
        {
            return SolHourStamp.Now().SolHoursIntoFuture(ETA).ToString();
        }
        
        public float DeliveryWaitPercentage()
        {
            float elapsed = SunOrbit.Instance.HoursSinceSol0 - this.Ordered.HoursSinceSol0;
            float duration = this.ETA.HoursSinceSol0 - this.Ordered.HoursSinceSol0;

            return elapsed / duration;
        }
    }

    public interface IDeliveryScript
    {
        void Deliver(Order o);
    }
}
