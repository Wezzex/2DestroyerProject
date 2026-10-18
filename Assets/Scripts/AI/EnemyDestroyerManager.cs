using UnityEngine;
public class EnemyDestroyerManager : UnitManager
{

    [Header("References")]
    [SerializeField] private ShipController shipController;
    [SerializeField] private EnemySpawner spawner;

    public GameObject explotionPrefab;

    [Header("Sub-light")]
    [Tooltip("A ship in sub-light heading this way drops out in time to stop about this far from the destroyer.")]
    [SerializeField] private float subLightClearance = 80f;
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


    }

    public override void CreateDeathExplotion()
    {
        Vector3 explotionSpawnPosition = this.transform.position;
        GameObject explotion = Instantiate(explotionPrefab, explotionSpawnPosition, Quaternion.identity);

    }

}