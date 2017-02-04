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

        public float DistanceMeters(LatLong other)
        {
            return -1f;
        }
    }
}