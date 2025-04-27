using UnityEngine;

public class StaticMapDisabler : MonoBehaviour
{
    [SerializeField] private GameObject staticMap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        staticMap.SetActive(false);
    }
}
