using UnityEngine;
using System.Collections;
using System;

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
        herperia_planum,
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
    }

    public static class PlayerGeography
    {
        public static LatLong BaseLocation;
    }

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
                phi = Mathf.Atan(point.z / point.x);

            //phi += Mathf.PI / 4f;
            //phi %= 1f * Mathf.PI;

            //UnityEngine.Debug.Log(String.Format("{2} = {0}/{1}", point.z, point.x, point.z / point.x));
            UnityEngine.Debug.Log(phi);

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