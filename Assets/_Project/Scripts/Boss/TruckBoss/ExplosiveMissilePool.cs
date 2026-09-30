// Fornisce e riutilizza i missili esplosivi.
using System.Collections.Generic;
using UnityEngine;

namespace BeyondSky.Boss
{
    public class ExplosiveMissilePool : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private ExplosiveMissile missilePrefab;
        [SerializeField] private int poolSize = 4;

        private List<ExplosiveMissile> missiles = new List<ExplosiveMissile>();

        private void Awake()
        {
            if (missilePrefab == null)
            {
                Debug.LogError("ExplosiveMissilePool: missilePrefab non assegnato.", this);
                return;
            }

            for (int i = 0; i < poolSize; i++)
            {
                ExplosiveMissile missile = Instantiate(missilePrefab, transform);
                missile.gameObject.SetActive(false);
                missiles.Add(missile);
            }
        }

        public ExplosiveMissile GetMissile()
        {
            for (int i = 0; i < missiles.Count; i++)
            {
                if (missiles[i].gameObject.activeSelf)
                {
                    continue;
                }

                return missiles[i];
            }

            return null;
        }

        public void DespawnAll()
        {
            for (int i = 0; i < missiles.Count; i++)
            {
                missiles[i].Despawn();
            }
        }
    }
}
