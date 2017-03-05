using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RedHomestead.EVA;

namespace RedHomestead.Persistence
{
    public interface IDataContainer<D> where D : RedHomesteadData
    {
        D Data { get; set; }
    }

    [Serializable]
    public abstract class RedHomesteadData
    {
        protected abstract void BeforeMarshal(UnityEngine.MonoBehaviour container);
        public RedHomesteadData Marshal(UnityEngine.MonoBehaviour container)
        {
            this.BeforeMarshal(container);
            return this;
        }
    }

    [Serializable]
    public class FacingData : RedHomesteadData
    {
        public Vector3 Position;
        public Quaternion Rotation;

        protected override void BeforeMarshal(MonoBehaviour container)
        {
            Position = container.transform.position;
            Rotation = container.transform.rotation;
        }
    }

    [Serializable]
    public class PlayerData: FacingData
    {
        public float ExcavationPerSecond = 1f;
        public float ConstructionPerSecond = 1f;
        public PackData PackData;

        protected override void BeforeMarshal(MonoBehaviour container)
        {
            base.BeforeMarshal(container);
            this.PackData = SurvivalTimer.Instance.Data;
        }
    }

    [Serializable]
    public class EnvironmentData
    {

    }

    [Serializable]
    public class Base {
        public static Base Current;

        public PlayerData Player { get; set; }
        public CrateData[] Crates { get; set; }
        public HabitatData[] Habitats { get; set; }
        //hobbit hole data
        //floorplan data
        //stuff data
        //module data
        ////container data
        ////pipe data

        public void Marshal()
        {
            this.Player = PlayerInput.Instance.Data.Marshal(PlayerInput.Instance) as PlayerData;
            this._MarshalMany<ResourceComponent, CrateData>((crates) => this.Crates = crates);
            this._MarshalMany<Habitat, HabitatData>((habitats) => this.Habitats = habitats);
        }

        private void _MarshalMany<C, D>(Action<D[]> setter) where C : UnityEngine.MonoBehaviour, IDataContainer<D> where D : RedHomesteadData
        {
            setter(Array.ConvertAll(UnityEngine.Transform.FindObjectsOfType<C>(),
                container =>
                (D)container.Data.Marshal(container)
            ));
        }
    }

    [Serializable]
    public class Game
    {
        public static Game Current { get; set; }
        public EnvironmentData Environment;
        public Base[] Bases { get; set; }
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
