using TMPro;
using UnityEngine;

public sealed class CampusQuestDefeatCounter : MonoBehaviour
{
    public static CampusQuestDefeatCounter Instance { get; private set; }

    [SerializeField] TMP_Text label;

    int count;

    void Awake()
    {
        Instance = this;
        count = 0;
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void NotifyDefeat()
    {
        count++;
        Refresh();
    }

    void Refresh()
    {
        if (label == null)
            return;
        label.text = "x" + count.ToString("00");
    }
}
