using UnityEngine;

public class FighterManager : UnitManager
{

    [Header("References")]
    [SerializeField] private ShipController shipController;
    [SerializeField] private EnemySpawner spawner;

    public GameObject explotionPrefab;

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
