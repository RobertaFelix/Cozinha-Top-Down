using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour
{
    [Header("Cadeiras")]
    [SerializeField]
    private Transform[] chairs;

    [SerializeField]
    private int availableChairCount = 2;

    public bool HasOccupiedChair => occupied != null && System.Array.IndexOf(occupied, true) >= 0;
    public int Capacity => gameObject.activeSelf ? Mathf.Clamp(availableChairCount, 0, chairs.Length) : 0;
    private bool[] occupied;

    private void Awake()
    {
        occupied ??= new bool[chairs.Length];
    }

    public bool HasFreeChair()
    {
        occupied ??= new bool[chairs.Length];
        int limit = Capacity;

        for (int i = 0; i < limit; i++)
        {
            if (!occupied[i])
                return true;
        }

        return false;
    }

    public Transform GetRandomFreeChair()
    {
        List<int> freeChairs =
            new List<int>();

        occupied ??= new bool[chairs.Length];
        int limit = Capacity;

        for (int i = 0; i < limit; i++)
        {
            if (!occupied[i])
            {
                freeChairs.Add(i);
            }
        }

        if (freeChairs.Count == 0)
            return null;

        int randomIndex =
            Random.Range(
                0,
                freeChairs.Count
            );

        int chairIndex =
            freeChairs[randomIndex];

        occupied[chairIndex] = true;

        return chairs[chairIndex];
    }

    public void FreeChair(Transform chair)
    {
        for (int i = 0; i < chairs.Length; i++)
        {
            if (chairs[i] == chair)
            {
                occupied[i] = false;
                return;
            }
        }
    }
}