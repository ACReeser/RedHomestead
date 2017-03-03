using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;

namespace RedHomestead.Persistence
{
    [Serializable]
    public class Base {
        //player data
        public List<ResourceComponent> Crates { get; set; }
        public List<Habitat> Habitats { get; set; }
        //hobbit hole data
        //floorplan data
        //stuff data
        //module data
        ////container data
        ////pipe data
    }

    public static class PersistentDataManager
    {
        public const string baseFileName = "base.dat";
        public static BinaryFormatter _formatter = new BinaryFormatter();

        public static void SaveBase(Base toSave)
        {
            using(FileStream file = File.Open(Path.Combine(UnityEngine.Application.persistentDataPath, baseFileName), FileMode.OpenOrCreate))
            {
                _formatter.Serialize(file, toSave);
            }
        }

        public static Base LoadBase()
        {
            using (FileStream file = File.Open(Path.Combine(UnityEngine.Application.persistentDataPath, baseFileName), FileMode.Open))
            {
                return _formatter.Deserialize(file) as Base;
            }
        }
    }

}
