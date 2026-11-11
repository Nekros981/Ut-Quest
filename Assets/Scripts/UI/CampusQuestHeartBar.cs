using UnityEngine;
using UnityEngine.UI;

public sealed class CampusQuestHeartBar : MonoBehaviour
{
    const int DefaultHearts = 6;

    [SerializeField] int maxHearts = DefaultHearts;
    [SerializeField] Sprite heartFull;
    [SerializeField] Sprite heartEmpty;
    [SerializeField] Image[] heartImages = new Image[DefaultHearts];

    int currentHearts = DefaultHearts;

    public int CurrentHearts => currentHearts;
    public int MaxHearts => maxHearts;

    void Awake()
    {
        if (maxHearts < 1)
            maxHearts = DefaultHearts;
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
        Refresh();
    }

    public void TakeDamage(int amount = 1)
    {
        if (amount <= 0 || currentHearts <= 0)
            return;

        currentHearts = Mathf.Max(0, currentHearts - amount);
        Refresh();

        var mateoForSfx = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        if (mateoForSfx != null)
            CampusQuestGunshot.PlayDamage(mateoForSfx.transform.position);

        if (currentHearts <= 0)
        {
            var mateo = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
            if (mateo != null)
                mateo.Defeat();
        }
    }

    public void SetHearts(int hearts)
    {
        currentHearts = Mathf.Clamp(hearts, 0, maxHearts);
        Refresh();
    }

#if UNITY_EDITOR
    [ContextMenu("Take 1 damage")]
    void EditorTakeDamage() => TakeDamage(1);
#endif

    void Refresh()
    {
        if (heartImages == null || heartImages.Length == 0 || heartFull == null || heartEmpty == null)
            return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            var image = heartImages[i];
            if (image == null)
                continue;
            image.sprite = i < currentHearts ? heartFull : heartEmpty;
        }
    }
}
