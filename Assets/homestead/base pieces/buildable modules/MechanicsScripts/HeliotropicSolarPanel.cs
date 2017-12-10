using RedHomestead.Buildings;
using RedHomestead.Persistence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HeliotropicSolarPanel : SolarPanel
{
    public Transform[] PivotPoles = new Transform[2];

    void Awake()
    {
        Efficiency = HeliotropicEfficiency;
    }

    public override Module GetModuleType()
    {
        return Module.SolarPanelSmall;
    }

    protected override void OnStart()
    {
        SunOrbit.Instance.OnHourChange += _OnHourChange;
        _OnHourChange(Game.Current.Environment.CurrentSol, Game.Current.Environment.CurrentHour);

        base.OnStart();
    }

    private const float TiltAmount = 50f;
    private void _OnHourChange(int sol, float hour)
    {
        //"preview" solar panels were throwing errors
        if (this.gameObject.activeInHierarchy && hour < 19 && hour > 5)
        {
            float tiltAmount = Mathf.Lerp(90f + TiltAmount, 90f - TiltAmount, (float)(hour - 6f) / 12f);
            StartCoroutine(RotateTo(tiltAmount));
        }
    }

    private IEnumerator RotateTo(float tiltAmount)
    {
        Quaternion from = PivotPoles[0].localRotation;
        Quaternion to = Quaternion.Euler(0f, -90f, tiltAmount);

        float time = 0f;
        while (time < 1f)
        {
            foreach (Transform panel in PivotPoles)
            {
                panel.localRotation = Quaternion.Lerp(from, to, time);
            }
            time += Time.deltaTime;
            yield return null;
        }
        foreach (Transform panel in PivotPoles)
        {
            panel.localRotation = to;
        }
    }
}