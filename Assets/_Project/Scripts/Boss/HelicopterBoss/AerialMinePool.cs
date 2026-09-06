using System.Collections.Generic;
using UnityEngine;

// Creates every aerial mine before gameplay for bosses and standard enemies.
public class AerialMinePool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private AerialMine minePrefab;
    [SerializeField] private int poolSize = 6;

    [Header("World movement")]
    [SerializeField] private SpeedManager speedManager;

    private List<AerialMine> mines = new List<AerialMine>();

    private void Awake()
    {
        if (minePrefab == null)
        {
            Debug.LogError("AerialMinePool: Mine Prefab non assegnato.", this);
            return;
        }

        if (speedManager == null)
        {
            Debug.LogWarning("AerialMinePool: Speed Manager non assegnato. Le mine non scorreranno.", this);
        }

        for (int i = 0; i < poolSize; i++)
        {
            AerialMine mine = Instantiate(minePrefab, transform);
            mine.Setup(speedManager);
            mine.gameObject.SetActive(false);
            mines.Add(mine);
        }
    }

    public AerialMine GetMine()
    {
        for (int i = 0; i < mines.Count; i++)
        {
            if (mines[i].gameObject.activeSelf)
            {
                continue;
            }

            return mines[i];
        }

        return null;
    }

    public void DespawnAll()
    {
        for (int i = 0; i < mines.Count; i++)
        {
            mines[i].Despawn();
        }
    }
}