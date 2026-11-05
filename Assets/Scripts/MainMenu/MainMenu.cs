using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    [Header("Splash groups")]
    [SerializeField] CanvasGroup splashLogoGroup;
    [SerializeField] CanvasGroup splashStudioGroup;
    [SerializeField] CanvasGroup mainMenuContentGroup;

    [Header("Logo timing (seconds)")]
    [SerializeField] float logoFadeInDuration = 0.5f;
    [SerializeField] float logoHoldDuration = 1.5f;
    [SerializeField] float logoFadeOutDuration = 0.4f;

    [Header("Studio timing (seconds)")]
    [SerializeField] float studioFadeInDuration = 0.5f;
    [SerializeField] float studioHoldDuration = 2f;
    [SerializeField] float studioFadeOutDuration = 0.4f;
    [SerializeField] float studioSkippedFadeOutDuration = 0.2f;

    [Header("Menu timing (seconds)")]
    [SerializeField] float menuFadeInDuration = 0.4f;

    [Header("Audio")]
    [SerializeField] MenuBackgroundMusic menuBackgroundMusic;

    bool studioSkipRequested;

    void Start()
    {
        PrepareGroup(splashLogoGroup);
        PrepareGroup(splashStudioGroup);
        PrepareGroup(mainMenuContentGroup);
        StartCoroutine(PlayIntroSequence());
    }

    static void PrepareGroup(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    IEnumerator PlayIntroSequence()
    {
        yield return FadeCanvasGroup(splashLogoGroup, 0f, 1f, logoFadeInDuration);
        yield return new WaitForSeconds(logoHoldDuration);
        yield return FadeCanvasGroup(splashLogoGroup, 1f, 0f, logoFadeOutDuration);

        yield return PlayStudioSplash();

        yield return FadeCanvasGroup(mainMenuContentGroup, 0f, 1f, menuFadeInDuration);
        EnableInteraction(mainMenuContentGroup);
    }

    IEnumerator PlayStudioSplash()
    {
        if (splashStudioGroup == null)
            yield break;

        var studioTotalDuration = studioFadeInDuration + studioHoldDuration + studioFadeOutDuration;
        var musicStartTime = studioTotalDuration * 0.75f;
        var fadeInEnd = studioFadeInDuration;
        var holdEnd = studioFadeInDuration + studioHoldDuration;
        var elapsed = 0f;
        var musicStarted = false;
        studioSkipRequested = false;

        void TryStartMusic(bool force)
        {
            if (musicStarted || menuBackgroundMusic == null)
                return;

            if (!force && elapsed < musicStartTime)
                return;

            musicStarted = true;
            menuBackgroundMusic.PlayLoop();
        }

        splashStudioGroup.alpha = 0f;

        while (true)
        {
            if (elapsed >= fadeInEnd && elapsed < holdEnd && !studioSkipRequested && WasScreenTapThisFrame())
            {
                studioSkipRequested = true;
                TryStartMusic(force: true);
            }

            if (elapsed < fadeInEnd)
            {
                var t = studioFadeInDuration <= 0f ? 1f : elapsed / studioFadeInDuration;
                splashStudioGroup.alpha = Mathf.Lerp(0f, 1f, t);
            }
            else if (elapsed < holdEnd)
            {
                splashStudioGroup.alpha = 1f;
            }
            else
            {
                break;
            }

            TryStartMusic(force: false);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (studioSkipRequested)
            TryStartMusic(force: true);

        var fadeOutDuration = studioSkipRequested ? studioSkippedFadeOutDuration : studioFadeOutDuration;
        var fadeOutStart = elapsed;
        var fadeOutEnd = fadeOutStart + fadeOutDuration;
        var fadeFrom = splashStudioGroup.alpha;

        while (elapsed < fadeOutEnd)
        {
            TryStartMusic(force: false);

            var t = fadeOutDuration <= 0f
                ? 1f
                : Mathf.Clamp01((elapsed - fadeOutStart) / fadeOutDuration);
            splashStudioGroup.alpha = Mathf.Lerp(fadeFrom, 0f, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        splashStudioGroup.alpha = 0f;
        TryStartMusic(force: false);
    }

    static bool WasScreenTapThisFrame()
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
                return true;
        }

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            return true;

        return false;
    }

    static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        var elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    static void EnableInteraction(CanvasGroup group)
    {
        if (group == null)
            return;

        group.interactable = true;
        group.blocksRaycasts = true;
    }
}
