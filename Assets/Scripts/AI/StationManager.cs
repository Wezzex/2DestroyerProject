using UnityEngine;
using UnityEngine.Events;

public class StationManager : UnitManager
{

    [Header("References")]
    [SerializeField] private ShipController shipController;
    [SerializeField] private EnemySpawner spawner;



    public GameObject explotionPrefab;

    [Header("Sub-light")]
    [Tooltip("A ship in sub-light heading this way drops out in time to stop about this far from the station's centre.")]
    [SerializeField] private float subLightClearance = 150f;
    public override float SubLightClearance => subLightClearance;

    private void Awake()
    {
        shipController = GetComponentInParent<ShipController>();
        spawner = GetComponent<EnemySpawner>();
    }

    public override void OnDestroyedBegin()
    {

    }

    public override void OnDestroyedEnd()
    {

        StationSpawner.Instance.OnStationDestroyed(gameObject);
    }

    public override void CreateDeathExplotion()
    {
        Vector3 explotionSpawnPosition = this.transform.position;
        GameObject explotion = Instantiate(explotionPrefab, explotionSpawnPosition, Quaternion.identity);

    }


}
