using UnityEngine;

public class PlayerManager : UnitManager
{
    [Header("References")]
    [SerializeField] private ShipController shipController;


    public GameObject explotionPrefab;


    private void Awake()
    {
        shipController = GetComponent<ShipController>();
    }
    public override void OnDestroyedBegin()
    {

    }

    public override void OnDestroyedEnd()
    {

        GameManager.Instance.RequestGameOver(GameManager.GameOverReason.PlayerDied);
    }

    public override void CreateDeathExplotion()
    {
        Vector3 explotionSpawnPosition = this.transform.position;
        GameObject explotion = Instantiate(explotionPrefab, explotionSpawnPosition, Quaternion.identity);

    }
}
