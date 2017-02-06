using UnityEngine;
using System.Collections;

namespace RedHomestead.Geography
{
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