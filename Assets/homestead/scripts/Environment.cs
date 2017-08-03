using RedHomestead.Persistence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RedHomestead.Environment
{
    public enum DustCoverage { None = 0, Light, Heavy, Stormy }
    public enum WeatherNarrative { None, MoreOfSame, DarkerSkies, LighterSkies, SuddenStorm, SuddenClear }
    public struct WeatherVariables
    {
        /// <summary>
        /// 0..100 where 0 means no inertia, 100 means certainty of same weather
        /// </summary>
        public int PreviousSystemInertia;
        /// <summary>
        /// 0..100 where 0 means no dust, 100 means full dust storm
        /// </summary>
        public int DustPercentageNumerator;
        /// <summary>
        /// -100..100
        /// </summary>
        public int Chaos;

        public WeatherVariables(System.Random random)
        {
            PreviousSystemInertia = random.Next(0, 101);
            DustPercentageNumerator = random.Next(0, 101);
            Chaos = random.Next(-100, 101);
        }
    }

    public struct Weather
    {
        public int Sol;
        public float DustIntensity;
        public DustCoverage Coverage;
        public WeatherNarrative Narrative;

        public Weather(System.Random random)
        {
            Sol = 0;
            WeatherVariables vars = new WeatherVariables(random);
            DustIntensity = vars.Chaos / 100f;
            Coverage = DustIntensity.FromFloat();
            Narrative = WeatherNarrative.None;
        }
        public Weather(System.Random random, Weather previousDay)
        {
            Sol = previousDay.Sol + 1;
            WeatherVariables vars = new WeatherVariables(random);

            //0 means completely new number, 100 means same weather as yesterday
            int sameness = vars.PreviousSystemInertia + vars.Chaos;
            sameness = Math.Max(sameness, 0);
            sameness = Math.Min(sameness, 100);

            DustIntensity = Mathf.Lerp(vars.DustPercentageNumerator / 100f, previousDay.DustIntensity, sameness / 100f);
            Coverage = DustIntensity.FromFloat();

            if (previousDay.DustIntensity <= .25f && DustIntensity > .75f)
                Narrative = WeatherNarrative.SuddenStorm;
            else if (previousDay.DustIntensity > .75f && DustIntensity < .25f)
                Narrative = WeatherNarrative.SuddenClear;
            else if (previousDay.Coverage == Coverage)
                Narrative = WeatherNarrative.MoreOfSame;
            else if (previousDay.DustIntensity < DustIntensity)
                Narrative = WeatherNarrative.DarkerSkies;
            else if (previousDay.DustIntensity > DustIntensity)
                Narrative = WeatherNarrative.LighterSkies;
            else
                Narrative = WeatherNarrative.None;
        }
    }

    public class DustManager
    {
        private System.Random random;
        private Transform DuskAndDawnOnlyParent;
        private SurvivalTimer survivalTimer;
        private EnvironmentData environment;
        private Weather yesterday, today, tomorrow;

        public DustManager(SunOrbit orb, Persistence.Game game, Persistence.Base @base, SurvivalTimer timer)
        {
            survivalTimer = timer;
            survivalTimer.OnPlayerInHabitatChange += _OnPlayerInHabitatChange;
            orb.OnSolChange += _OnSolChange;
            orb.OnHourChange += _OnHourChange;
            orb.OnDawn += _OnDawn;
            orb.OnDusk += _OnDusk;
            DuskAndDawnOnlyParent = orb.DuskAndDawnOnlyParent;
            environment = game.Environment;
            this.random = new System.Random(@base.WeatherSeed);
            FastForward(environment.CurrentSol);
            DoAfterSolChange();

            if (environment.CurrentHour > 12)
                AnnounceForecast();
            else
            {
#if UNITY_EDITOR
                AnnounceForecast();
#endif
            }
        }

        private bool playerInHabitat = false;
        private void _OnPlayerInHabitatChange(bool isInHabitat)
        {
            playerInHabitat = isInHabitat;
            RefreshDustParticleSystems();
        }

        private bool isDawnOrDusk = false;
        private void _OnDusk(bool isStart)
        {
            isDawnOrDusk = isStart;
            RefreshDustParticleSystems();
        }

        private void _OnDawn(bool isStart)
        {
            isDawnOrDusk = isStart;
            RefreshDustParticleSystems();
        }

        private void FastForward(int gameSol)
        {
            today = new Weather(random);
            tomorrow = new Weather(random, today);
            IncrementWeather();
            while (today.Sol < gameSol)
            {
                IncrementWeather();
            }
        }

        private void IncrementWeather()
        {
            yesterday = today;
            today = tomorrow;
            environment.DustIntensity = today.DustIntensity;
            tomorrow = new Weather(random, today);
        }

        private void _OnSolChange(int sol)
        {
            IncrementWeather();
            DoAfterSolChange();
        }

        private void _OnHourChange(int sol, float hour)
        {
            if (hour == 12)
                AnnounceForecast();
        }

        private void AnnounceForecast()
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(String.Format("Today: {0} - {1} dust", today.Narrative, today.Coverage));
            UnityEngine.Debug.Log(String.Format("Tomorrow: {0} - {1} dust", tomorrow.Narrative, tomorrow.Coverage));
#endif
        }

        private void DoAfterSolChange()
        {
            RefreshDustParticleSystems();
        }

        private void RefreshDustParticleSystems()
        {
            foreach (Transform t in DuskAndDawnOnlyParent)
            {
                var ps = t.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var emission = ps.emission;
                    emission.rateOverTime = today.Coverage.EmissionCurve();
                    emission.enabled = !playerInHabitat && (isDawnOrDusk || today.Coverage != DustCoverage.None);

                    if (emission.enabled && !ps.isPlaying)
                        ps.Play();
                    else if (!emission.enabled && ps.isPlaying)
                        ps.Stop();
                }
            }

            RenderSettings.fog = today.Coverage == DustCoverage.Stormy;
        }
    }

    public static class EnvironmentExtensions
    {
        public const float StormThreshold = .75f;
        public const float HeavyThreshold = .5f;
        public const float LightThreshold = .25f;

        private static ParticleSystem.MinMaxCurve lightCurve = new ParticleSystem.MinMaxCurve(50);
        private static ParticleSystem.MinMaxCurve heavyCurve = new ParticleSystem.MinMaxCurve(100);
        private static ParticleSystem.MinMaxCurve stormyCurve = new ParticleSystem.MinMaxCurve(200);

        public static ParticleSystem.MinMaxCurve EmissionCurve(this DustCoverage coverage)
        {
            switch (coverage)
            {
                default:
                case DustCoverage.Light:
                    return lightCurve;
                case DustCoverage.Heavy:
                    return heavyCurve;
                case DustCoverage.Stormy:
                    return stormyCurve;
            }
        }

        public static DustCoverage FromFloat(this float intensity)
        {
            if (intensity >= StormThreshold)
                return DustCoverage.Stormy;
            else if (intensity >= HeavyThreshold)
                return DustCoverage.Heavy;
            else if (intensity >= LightThreshold)
                return DustCoverage.Light;
            else
                return DustCoverage.None;
        }
    }
}