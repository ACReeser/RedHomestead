using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

namespace RedHomestead.Geography
{
    public enum Direction { North, East, South, West }
    public enum MarsRegion {
        acidalia_planitia,
        alba_mons,
        amazonis_planitia,
        aonia_terra,
        arabia_terra,
        arcadia_planitia,
        argyre_planitia,
        chryse_planitia,
        daedalia_planum,
        elysium_mons,
        elysium_planitia,
        hellas_planitia,
        hesperia_planum,
        lunae_planum,
        meridiani_planum,
        noachis_terra,
        north_pole,
        olympus_mons,
        planum_australe,
        promethei_terra,
        south_pole,
        syria_thaumasia,
        syrtis_major_planum,
        tempe_terra,
        terra_cimmeria,
        terra_sabaea,
        terra_sirenum,
        tharsis_montes,
        tyrrhena_terra,
        utopia_planitia,
        valles_marineris,
        vastitas_borealis,
        xanthe_terra
    }

    public struct MarsRegionData
    {
        public float SolarMultiplier;
        public float MineralMultiplier;
        public float WaterMultiplier;
        public Matter AbundantMatter;

        public MarsRegionData(float solar, float mineral, float water, Matter abundant)
        {
            this.SolarMultiplier = solar;
            this.MineralMultiplier = mineral;
            this.WaterMultiplier = water;
            this.AbundantMatter = abundant;
        }

        public string SolarMultiplierString { get { return String.Format("{0:0}% Solar", SolarMultiplier * 100); } }
        public string MineralMultiplierString { get { return String.Format("{0:0}% Mineral", MineralMultiplier * 100); } }
        public string WaterMultiplierString { get { return String.Format("{0:0}% Water", WaterMultiplier * 100); } }
    }

    [Serializable]
    public class BaseLocation
    {
        public MarsRegion Region;
        public LatLong LatLong;
    }

    public static class GeoExtensions
    {
        private static Quaternion eastQ = Quaternion.Euler(0, 90, 0);
        private static Quaternion southQ = Quaternion.Euler(0, 180, 0);
        private static Quaternion westQ = Quaternion.Euler(0, 270, 0);

        public static Quaternion ToQuaternion(this Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                    return Quaternion.identity;
                case Direction.East:
                    return eastQ;
                case Direction.South:
                    return southQ;
                case Direction.West:
                    return westQ;
            }

            return Quaternion.identity;
        }

        public static Direction Rotate(this Direction dir, bool clockwise)
        {
            if (clockwise)
            {
                dir = dir - 1;
                if (dir < 0)
                    dir = Direction.West;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                dir = dir + 1;

                if (dir > Direction.West)
                    dir = Direction.North;
            }

            return dir;
        }

        public static MarsRegion ParseRegion(string input)
        {
            return (MarsRegion)Enum.Parse(typeof(MarsRegion), input);
        }

        public static string Name(this MarsRegion region)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(region.ToString().Replace('_', ' '));
        }

        private static MarsRegionData[] _regionData = new MarsRegionData[]
        {
            //acidalia_planitia,
            new MarsRegionData(1f, 1f, 1.25f, Matter.Silica), //mid latitude, glacier ice
            //alba_mons,
            new MarsRegionData(.8f, 1.25f, .8f, Matter.Iron), //high dust, water in soil
            //amazonis_planitia,
            new MarsRegionData(1.25f, 1f, 1.1f, Matter.Silica), //equatorial, dark streaks
            //aonia_terra,
            new MarsRegionData(.8f, 1.15f, 1.1f, Matter.Iron), //near polar, rough terrain
            //arabia_terra,
            new MarsRegionData(1.25f, 1.15f, 1.1f, Matter.Iron), //equatorial, rough terrain
            //arcadia_planitia,
            new MarsRegionData(.8f, 1f, 1.1f, Matter.Iron), //near polar, water in craters
            //argyre_planitia,
            new MarsRegionData(.8f, 1.25f, 1.1f, Matter.Iron), //near polar, impact crater, ancient lake
            //chryse_planitia,
            new MarsRegionData(1.25f, 1f, 1f, Matter.Iron), //equatorial
            //daedalia_planum,
            new MarsRegionData(1f, 1f, .8f, Matter.Iron), //mid latitude, planum, featureless
            //elysium_mons,
            new MarsRegionData(1f, 1.25f, .6f, Matter.Iron), //mid latitude, mountain
            //elysium_planitia,
            new MarsRegionData(1.25f, 1f, 1.25f, Matter.Iron), //equatorial, water ice in photos
            //hellas_planitia,
            new MarsRegionData(.8f, 1.25f, 1.25f, Matter.Iron), //near polar, impact crater, water can be liquid
            //hesperia_planum,
            new MarsRegionData(1f, 1.1f, 1f, Matter.Iron), //mid latitude, impact craters
            //lunae_planum,
            new MarsRegionData(1.25f, 1.1f, 1f, Matter.Iron), //equatorial, impact craters
            //meridiani_planum,
            new MarsRegionData(1.25f, 1.1f, 1f, Matter.Iron), //equatorial, high iron content
            //noachis_terra,
            new MarsRegionData(.8f, 1.1f, 1f, Matter.Iron), //near polar, rough terrain
            //north_pole,
            new MarsRegionData(.25f, .5f, 2f, Matter.Water), //pole
            //olympus_mons,
            new MarsRegionData(1f, 1.5f, .8f, Matter.Iron), //equatorial, mountain
            //planum_australe,
            new MarsRegionData(.5f, 1.1f, 1.5f, Matter.Iron), //polar, rough
            //promethei_terra,
            new MarsRegionData(.8f, 1.1f, 1.1f, Matter.Water), //near polar, rough
            //south_pole,
            new MarsRegionData(.25f, .75f, 1.75f, Matter.Water), //pole
            //syria_thaumasia,
            new MarsRegionData(1f, 1.1f, 1f, Matter.Iron), //mid latitude, impact craters
            //syrtis_major_planum,
            new MarsRegionData(1.25f, 1.1f, 1f, Matter.Iron), //equatorial, impact craters
            //tempe_terra,
            new MarsRegionData(1f, 1.1f, 1f, Matter.Iron), //mid latitude, impact craters
            //terra_cimmeria,
            new MarsRegionData(1f, 1.1f, 1f, Matter.Iron), //mid latitude, impact craters
            //terra_sabaea,
            new MarsRegionData(1.25f, 1.25f, 1f, Matter.Iron), //equatorial, many impact craters
            //terra_sirenum,
            new MarsRegionData(1.15f, 1.1f, 1f, Matter.Iron), //mid to polar latitude, impact craters
            //tharsis_montes,
            new MarsRegionData(1.25f, 1.5f, .8f, Matter.Iron), //equatorial, mountain
            //tyrrhena_terra,
            new MarsRegionData(1.2f, 1.1f, 1f, Matter.Iron), //mid to equatorial latitude, impact craters
            //utopia_planitia,
            new MarsRegionData(1.2f, 1f, 1.33f, Matter.Iron), //mid to equatorial latitude, large amounts of ice
            //valles_marineris,
            new MarsRegionData(1.25f, 1f, 1.1f, Matter.Iron), //equatorial latitude, deep + shadows
            //vastitas_borealis,
            new MarsRegionData(.5f, 1f, 1.5f, Matter.Water), //polar, smooth
            //xanthe_terra
            new MarsRegionData(1.25f, 1.1f, 1f, Matter.Iron), //equatorial, impact craters
        };

        public static MarsRegionData Data(this MarsRegion region)
        {
            return _regionData[(int)region];
        }
    }

    public static class PlayerGeography
    {
        public static LatLong BaseLocation;
    }

    [Serializable]
    public struct LatLong
    {
        public float LatitudeDegrees;
        public float LongitudeDegrees;

        public float DistanceKilometers(LatLong other)
        {
            return 100f;
        }

        public static LatLong FromPointOnUnitSphere(Vector3 point)
        {

            float r = Mathf.Sqrt(Mathf.Pow(point.x, 2) + Mathf.Pow(point.y, 2) + Mathf.Pow(point.z, 2));

            float phi;

            if (point.x == 0)
                phi = 0;
            else
                phi = Mathf.Atan2(point.z, point.x);
            
            //UnityEngine.Debug.Log(phi);

            return new LatLong()
            {
                //theta
                LatitudeDegrees = 90f - Mathf.Acos(point.y / r) * Mathf.Rad2Deg,
                //phi
                LongitudeDegrees = phi * Mathf.Rad2Deg
            };
        }

        public override string ToString()
        {
            return String.Format("Lat {0:0.##}, Lon {1:0.##}", LatitudeDegrees, LongitudeDegrees);
        }
    }
}