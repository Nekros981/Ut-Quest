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

        studioSkipRequested = false;
        yield return FadeCanvasGroup(splashStudioGroup, 0f, 1f, studioFadeInDuration);
        yield return WaitForStudioHoldOrSkip();

        var studioOut = studioSkipRequested ? studioSkippedFadeOutDuration : studioFadeOutDuration;
        yield return FadeCanvasGroup(splashStudioGroup, splashStudioGroup.alpha, 0f, studioOut);

        yield return FadeCanvasGroup(mainMenuContentGroup, 0f, 1f, menuFadeInDuration);
        EnableInteraction(mainMenuContentGroup);
    }

    IEnumerator WaitForStudioHoldOrSkip()
    {
        var elapsed = 0f;
        while (elapsed < studioHoldDuration && !studioSkipRequested)
        {
            if (WasScreenTapThisFrame())
                studioSkipRequested = true;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
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
