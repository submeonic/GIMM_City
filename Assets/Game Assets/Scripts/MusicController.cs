using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class MusicController : MonoBehaviour
{
    public enum MusicEnergyLevel { VeryLow, Low, Medium, High, VeryHigh }

    [Header("🔊 Audio Mixer & Parameters")]
    public AudioMixer mixer;

    [System.Serializable]
    public class Track
    {
        public string name;
        public string mixerParam;
        [Range(-80, 0)] public float activeVolume = 0;
        [Range(-80, 0)] public float inactiveVolume = -80;
    }

    [Header("🎚️ Mixer Tracks")]
    public Track mainBass;
    public Track subBass;
    public Track melodyA;
    public Track melodyB;
    public Track auxA;
    public Track auxB;
    public Track drumsFullLoop;
    public Track drumsInFill;

    [Header("🎵 Audio Sources")]
    public AudioSource syncSource;
    public float bpm = 120f;
    public int beatsPerBar = 4;

    [Header("⚙️ Settings")]
    public MusicEnergyLevel currentLevel = MusicEnergyLevel.VeryLow;
    public float fadeTime = 1f;

    // Internal
    private float secondsPerBar;
    private int barCount = 0;
    private Coroutine transitionRoutine;
    private bool melodyToggle;
    private MusicEnergyLevel previousLevel;
    private MusicEnergyLevel pendingLevel;
    private bool waitingForEndOfPhrase = false;
    private bool fullDrumsArePlaying = false;
    private MusicEnergyLevel lastDrumStartLevel = MusicEnergyLevel.VeryLow;

    void Start()
    {
        secondsPerBar = (60f / bpm) * beatsPerBar;
        SetEnergyLevel(currentLevel, true);
        previousLevel = currentLevel;
        StartCoroutine(BarTimer());
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Application.isEditor && Application.isPlaying && currentLevel != previousLevel)
        {
            SetEnergyLevel(currentLevel);
            previousLevel = currentLevel;
        }
#endif
    }

    IEnumerator BarTimer()
    {
        yield return new WaitUntil(() => syncSource.isPlaying);
        double dspStartTime = AudioSettings.dspTime - (syncSource.timeSamples / (double)syncSource.clip.frequency);
        double nextTime = dspStartTime + secondsPerBar;

        while (true)
        {
            double wait = nextTime - AudioSettings.dspTime;
            if (wait > 0)
                yield return new WaitForSeconds((float)wait);

            OnBar();
            nextTime += secondsPerBar;
        }
    }

    void OnBar()
    {
        barCount++;

        // Bar 3: In-Fill for High
        if ((barCount + 1) % 4 == 0 &&
            currentLevel == MusicEnergyLevel.High &&
            ShouldPlayFullDrums(currentLevel, lastDrumStartLevel))
        {
            StartCoroutine(StartDrumsWithInFillThenApplyHighExtras());
            lastDrumStartLevel = currentLevel;
        }

        // Bar 4: full loop triggers, melody toggles
        if (barCount % 4 == 0)
        {
            HandleMelodyToggle();

            if (currentLevel == MusicEnergyLevel.VeryHigh && !fullDrumsArePlaying)
            {
                StartCoroutine(TurnOnFullLoopAtBar());
                lastDrumStartLevel = currentLevel;
            }

            if (currentLevel == MusicEnergyLevel.High && !fullDrumsArePlaying)
            {
                StartCoroutine(TurnOnFullLoopAtBar());
                lastDrumStartLevel = currentLevel;
                StartCoroutine(ApplyHighExtrasAfterInFill());
            }

            if (waitingForEndOfPhrase)
            {
                waitingForEndOfPhrase = false;
                SetEnergyLevel(pendingLevel);
            }
        }
    }

    void HandleMelodyToggle()
    {
        if ((currentLevel >= MusicEnergyLevel.Low && currentLevel <= MusicEnergyLevel.Medium) ||
            currentLevel == MusicEnergyLevel.VeryHigh)
        {
            melodyToggle = !melodyToggle;
            StartCoroutine(SetTrack(melodyA, melodyToggle));
            StartCoroutine(SetTrack(melodyB, !melodyToggle));
        }
        else
        {
            StartCoroutine(SetTrack(melodyA, false));
            StartCoroutine(SetTrack(melodyB, false));
        }
    }

    bool ShouldPlayFullDrums(MusicEnergyLevel current, MusicEnergyLevel lastTriggered)
    {
        bool fromLower = lastTriggered < MusicEnergyLevel.High;
        bool toHigh = current == MusicEnergyLevel.High || current == MusicEnergyLevel.VeryHigh;
        return toHigh && fromLower && !fullDrumsArePlaying;
    }

    IEnumerator StartDrumsWithInFillThenApplyHighExtras()
    {
        Debug.Log($"[MusicController] In-Fill triggered at bar {barCount}");
        yield return SetTrack(drumsInFill, true, immediate: true);
        yield return new WaitForSeconds(secondsPerBar);
        yield return SetTrack(drumsInFill, false, immediate: true);

        if (!fullDrumsArePlaying)
        {
            yield return SetTrack(drumsFullLoop, true, immediate: true);
            fullDrumsArePlaying = true;
        }

        StartCoroutine(ApplyHighExtrasAfterInFill());
    }

    IEnumerator ApplyHighExtrasAfterInFill()
    {
        yield return SetTrack(subBass, true);
        yield return SetTrack(auxA, true);
        yield return SetTrack(auxB, false);
        yield return SetTrack(melodyA, false);
        yield return SetTrack(melodyB, false);
    }

    IEnumerator TurnOnFullLoopAtBar()
    {
        Debug.Log($"[MusicController] Full Loop ON at bar {barCount} for state {currentLevel}");
        yield return SetTrack(drumsFullLoop, true, immediate: true);
        fullDrumsArePlaying = true;
    }

    public void SetEnergyLevel(MusicEnergyLevel newLevel, bool instant = false)
    {
        if (!Application.isPlaying) return;

        bool steppingDownFromDrums =
            (currentLevel == MusicEnergyLevel.High || currentLevel == MusicEnergyLevel.VeryHigh) &&
            (newLevel < MusicEnergyLevel.High);

        if (steppingDownFromDrums && !instant)
        {
            pendingLevel = newLevel;
            waitingForEndOfPhrase = true;
            return;
        }

        currentLevel = newLevel;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        if (currentLevel < MusicEnergyLevel.High)
            transitionRoutine = StartCoroutine(ApplyStateWithDrumExit(newLevel, instant));
        else
            transitionRoutine = StartCoroutine(ApplyState(newLevel, instant));
    }

    IEnumerator ApplyState(MusicEnergyLevel level, bool instant = false)
    {
        yield return SetTrack(mainBass, true, instant);

        switch (level)
        {
            case MusicEnergyLevel.VeryLow:
                yield return SetTrack(subBass, false, instant);
                yield return SetTrack(auxA, false, instant);
                yield return SetTrack(auxB, false, instant);
                yield return SetTrack(melodyA, false, instant);
                yield return SetTrack(melodyB, false, instant);
                break;

            case MusicEnergyLevel.Low:
                yield return SetTrack(subBass, false, instant);
                yield return SetTrack(auxA, false, instant);
                yield return SetTrack(auxB, false, instant);
                break;

            case MusicEnergyLevel.Medium:
                yield return SetTrack(subBass, true, instant);
                yield return SetTrack(auxA, true, instant);
                yield return SetTrack(auxB, Random.value > 0.5f, instant);
                break;

            case MusicEnergyLevel.High:
                // ✅ Do NOT mute drums — let bar logic handle
                break;

            case MusicEnergyLevel.VeryHigh:
                yield return SetTrack(subBass, true, instant);
                yield return SetTrack(auxA, true, instant);
                yield return SetTrack(auxB, true, instant);
                yield return SetTrack(melodyA, true, instant);
                yield return SetTrack(melodyB, false, instant);
                // ✅ Do NOT mute drums — let bar logic handle
                break;
        }
    }

    IEnumerator ApplyStateWithDrumExit(MusicEnergyLevel level, bool instant)
    {
        yield return ApplyState(level, instant);
        yield return SetAllDrumsOff(instant);
    }

    IEnumerator SetTrack(Track track, bool enable, bool instant = false, bool immediate = false)
    {
        float target = enable ? track.activeVolume : track.inactiveVolume;

        if (immediate || instant)
        {
            mixer.SetFloat(track.mixerParam, target);
            yield break;
        }

        mixer.GetFloat(track.mixerParam, out float current);
        float t = 0;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float val = Mathf.Lerp(current, target, t / fadeTime);
            mixer.SetFloat(track.mixerParam, val);
            yield return null;
        }

        mixer.SetFloat(track.mixerParam, target);
    }

    IEnumerator SetAllDrumsOff(bool instant)
    {
        yield return SetTrack(drumsFullLoop, false, instant, true);
        yield return SetTrack(drumsInFill, false, instant, true);
        fullDrumsArePlaying = false;
        lastDrumStartLevel = MusicEnergyLevel.VeryLow;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying && currentLevel != previousLevel)
        {
            SetEnergyLevel(currentLevel);
        }
    }
#endif
}
