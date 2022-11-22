using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightsManagerV3 : LightsManager
{
    [Header("V3 Configurations")]
    public int GroupId;
    public bool HasColorEvent = true;
    public bool XRotatable = true;
    public bool YRotatable = false;
    public bool XFlip = false;
    public bool YFlip = false;
    public bool ZRotatable = false;
    public bool ZFlip = false;
    public bool TreatZAsX = false;
    [SerializeField] private float brightnessMultiplier = 1;
    [Serializable] public class TranslationConfiguration
    {
        public bool XTranslatable = false;
        public bool XFlip = false;
        public float TranslationMultiplierX = 1;
        public bool YTranslatable = false;
        public bool YFlip = false;
        public float TranslationMultiplierY = 1;
        public bool ZTranslatable = false;
        public bool ZFlip = false;
        public float TranslationMultiplierZ = 1;
    }
    public TranslationConfiguration TranslationConfig;
    public bool IsValidColorLane() => HasColorEvent;
    public bool HasRotationEvent => XRotatable || YRotatable || ZRotatable;
    public bool IsValidRotationAxis(int axis) => (axis == 0 && XRotatable) || (axis == 1 && YRotatable) || (axis == 2 && ZRotatable);
    public bool HasTranslationEvent => TranslationConfig.XTranslatable || TranslationConfig.YTranslatable || TranslationConfig.ZTranslatable;
    public bool IsValidTranslationAxis(int axis) => (axis == 0 && TranslationConfig.XTranslatable) || 
        (axis == 1 && TranslationConfig.YTranslatable) || (axis == 2 && TranslationConfig.ZTranslatable);

    public List<RotatingEvent> ControllingRotations = new List<RotatingEvent>();
    public List<TranslationEvent> ControllingTranslations = new List<TranslationEvent>();

    protected new IEnumerator Start()
    {
        yield return base.Start();
        var lights = GetComponentsInChildren<LightingEvent>();
        if (DisableCustomInitialization) // we reuse this bool option, if it is true, we will only add those lightingEvents having same group id
        {
            int cnt = 0;
            foreach (var light in lights)
            {
                if (light.OverrideLightGroupID == GroupId)
                {
                    ControllingLights.Add(light);
                    light.LightIdx = cnt;
                    cnt++;
                }
            }
        }
        else // all the lights are belonged to this group.
        {
            for (int i = 0; i < lights.Length; ++i)
            {
                lights[i].LightIdx = i;
            }
        }
        if (brightnessMultiplier != 1 && brightnessMultiplier != 0)
        {
            foreach (var light in ControllingLights)
                light.brightnessMultiplier = brightnessMultiplier;
        }

        if (ControllingRotations.Count == 0) // include all rotations
        {
            var rotations = GetComponentsInChildren<RotatingEvent>();
            for (int i = 0; i < rotations.Length; ++i)
            {
                ControllingRotations.Add(rotations[i]);
            }
        }
        for (int i = 0; i < ControllingRotations.Count; ++i)
        {
            ControllingRotations[i].lightsManager = this;
            ControllingRotations[i].RotationIdx = i;
            ControllingRotations[i].XData.flip = XFlip;
            ControllingRotations[i].YData.flip = YFlip;
            ControllingRotations[i].ZData.flip = ZFlip;
        }

        if (ControllingTranslations.Count == 0)
        {
            var translations = GetComponentsInChildren<TranslationEvent>();
            foreach (var trans in translations)
                ControllingTranslations.Add(trans);
        }
        for (int i = 0; i < ControllingTranslations.Count; ++i)
        {
            ControllingTranslations[i].lightsManager = this;
            ControllingTranslations[i].TranslationIdx = i;
            ControllingTranslations[i].XData.flip = TranslationConfig.XFlip;
            ControllingTranslations[i].translationMultiplierX *= TranslationConfig.TranslationMultiplierX;
            ControllingTranslations[i].YData.flip = TranslationConfig.YFlip;
            ControllingTranslations[i].translationMultiplierY *= TranslationConfig.TranslationMultiplierY;
            ControllingTranslations[i].ZData.flip = TranslationConfig.ZFlip;
            ControllingTranslations[i].translationMultiplierZ *= TranslationConfig.TranslationMultiplierZ;
        }

        if (TreatZAsX) XRotatable = true; // for compatibility with sanity check
        yield return null;
    }

    public void ResetNoteIndex()
    {
        foreach (var light in ControllingLights) light.SetNoteIndex(-1, true);
        foreach (var rot in ControllingRotations) rot.ResetNoteIndex();
        foreach (var trans in ControllingTranslations) trans.ResetNoteIndex();
    }

    public override void Boost(bool boost, Color red, Color blue)
    {
        foreach (var light in ControllingLights)
        {
            light.UpdateBoostState(boost);
            light.SetTargetColor((light.TargetColorId == 0 ? red : blue) * HDRIntensity);
        }
    }
}
