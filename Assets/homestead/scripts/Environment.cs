using RedHomestead.Persistence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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
        private const float FollowIntervalSeconds = 1f;
        private System.Random random;
        private Transform DuskAndDawnOnlyParent;
        private SurvivalTimer survivalTimer;
        private EnvironmentData environment;
        private Weather yesterday, today, tomorrow;

        public DustManager(
            SunOrbit orb, 
            Persistence.Game game, 
            Persistence.Base @base, 
            SurvivalTimer timer,
            Func<IEnumerator, Coroutine> startCoroutine,
            Action<Coroutine> stopCoroutine)
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
            this.startCoroutine = startCoroutine;
            this.stopCoroutine = stopCoroutine;
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
            refreshSolarPanels();
        }

        internal void OnSolarPanelAdded(SolarPanel s)
        {
            s.RefreshSolarPanelDustVisuals();
        }

        private void refreshSolarPanels()
        {
            foreach(SolarPanel sp in SolarPanel.AllPanels)
            {
                sp.RefreshSolarPanelDustVisuals();
            }
        }

        private bool playerInHabitat = false;
        private void _OnPlayerInHabitatChange(bool isInHabitat)
        {
            playerInHabitat = isInHabitat;
            RefreshDustParticleSystems();
        }

        private bool isDawnOrDusk = false;
        private readonly Func<IEnumerator, Coroutine> startCoroutine;
        private readonly Action<Coroutine> stopCoroutine;
        private Coroutine currentFollow;
        private bool currentlyShowingDustStormParticles;

        private void _OnDusk(bool isStart)
        {
            isDawnOrDusk = isStart;
            RefreshDustParticleSystems();
            if (!isStart)
                RefreshDarkOrLightDustStormParticles(false);
        }
        private void _OnDawn(bool isStart)
        {
            isDawnOrDusk = isStart;
            RefreshDustParticleSystems();
            if (isStart)
                RefreshDarkOrLightDustStormParticles(true);
        }
        private void RefreshDarkOrLightDustStormParticles(bool isSunUp)
        {
            foreach (Transform t in DuskAndDawnOnlyParent)
            {
                var ps = t.GetComponent<ParticleSystemRenderer>();
                if (ps != null)
                {
                    ps.material = isSunUp ? SunOrbit.Instance.DuststormDay : SunOrbit.Instance.DuststormNight;
                }
            }
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
            if (hour == 6 || hour == 12 || hour == 22)
                AnnounceForecast();

            AddIncrementalDust();

            RenderSettings.fogColor = Color.Lerp(FogColor, Color.black, hour < 6 || hour > 16 ? 1f : 0f);
        }

        private void AddIncrementalDust()
        {
            foreach(SolarPanel s in SolarPanel.AllPanels)
            {
                s.FlexData.DustBuildup += today.DustIntensity / 24f;
                s.RefreshSolarPanelDustVisuals();
            }
        }

        private void AnnounceForecast()
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(String.Format("Today: {0} - {1} dust", today.Narrative, today.Coverage));
            UnityEngine.Debug.Log(String.Format("Tomorrow: {0} - {1} dust", tomorrow.Narrative, tomorrow.Coverage));
#endif
            if (WeatherStation.AllWeatherStations.Count > 0 && WeatherStation.AllWeatherStations.Any(w => w.IsOn))
            {
                GuiBridge.Instance.ShowNews(today.Coverage.News().CloneWithPrefix("Today's weather: "));
                GuiBridge.Instance.ShowNews(tomorrow.Coverage.News().CloneWithPrefix("Tomorrow's weather: "));                
            }
        }

        private void DoAfterSolChange()
        {
            RefreshDustParticleSystems();
        }

        private void RefreshDustParticleSystems()
        {
            this.currentlyShowingDustStormParticles = !playerInHabitat && (isDawnOrDusk || today.Coverage != DustCoverage.None);
            ParticleSystem.MinMaxCurve dustStormRateOverTime = today.Coverage.EmissionCurve();

            foreach (Transform t in DuskAndDawnOnlyParent)
            {
                var ps = t.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var emission = ps.emission;
                    emission.rateOverTime = dustStormRateOverTime;
                    emission.enabled = currentlyShowingDustStormParticles;

                    //var newMain = ps.main;
                    //newMain.startColor = new ParticleSystem.MinMaxGradient()
                    //{
                    //    colorMin = new Color( 77f / 256f, 58f / 256f, 40f / 256f, 1f),
                    //    colorMax = new Color(116f / 256f, 38f / 256f,  0f, 1f),
                    //};

                    if (emission.enabled && !ps.isPlaying)
                        ps.Play();
                    else if (!emission.enabled && ps.isPlaying)
                        ps.Stop();
                }
            }

            if (currentlyShowingDustStormParticles)
            {
                this.currentFollow = this.startCoroutine(follow());
            }
            else
            {
                StopFollowCoroutine();
            }
            RenderSettings.fog = today.Coverage == DustCoverage.Stormy;
        }
        private static Color FogColor = new Color(111f/255f, 37f/255f, 0f/255f, 1f);

        private void StopFollowCoroutine()
        {
            if (this.currentFollow != null)
            {
                this.stopCoroutine(this.currentFollow);
            }
        }

        private IEnumerator follow()
        {
            while(currentlyShowingDustStormParticles)
            {
                yield return new WaitForSeconds(FollowIntervalSeconds);

                foreach (Transform t in DuskAndDawnOnlyParent)
                {
                    t.position = new Vector3(PlayerInput.Instance.transform.position.x, PlayerInput.Instance.transform.position.y, PlayerInput.Instance.transform.position.z);
                }            
            }
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
        public static News News(this DustCoverage coverage)
        {
            switch (coverage)
            {
                default:
                    return NewsSource.WeatherClearSky;
                case DustCoverage.Light:
                    return NewsSource.WeatherLightDust;
                case DustCoverage.Heavy:
                    return NewsSource.WeatherHeavyDust;
                case DustCoverage.Stormy:
                    return NewsSource.WeatherDustStorm;
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