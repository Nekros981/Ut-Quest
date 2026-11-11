using UnityEngine;

[RequireComponent(typeof(CampusQuestMateoVisualDriver))]
public sealed class CampusQuestGameOverMateo : MonoBehaviour
{
    void Start()
    {
        GetComponent<CampusQuestMateoVisualDriver>().PlayDerrotado();
    }
}
