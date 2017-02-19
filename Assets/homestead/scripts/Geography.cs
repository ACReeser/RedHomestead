using UnityEngine;
using System.Collections;

namespace RedHomestead.Geography
{
    public enum Direction { North, East, South, West }

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
    }
}