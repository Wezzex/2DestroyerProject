using Unity.VisualScripting;
using UnityEngine;

public class ObjectGeneratorRandomPositionUtil : MonoBehaviour
{
    [SerializeField] private GameObject objectPrefab;

    [SerializeField] private float radius = 0.2f;

    protected Vector3 GetRandomPosition()
    {
        return Random.insideUnitSphere * radius + transform.position;
    }

    protected Quaternion RandomRotation()
    {
        return Quaternion.Euler(0,0, Random.Range(0, 360));
    }

    public void CreateObject()
    {
        Vector3 position = GetRandomPosition();
        GameObject impactObject = GetObject();

        impactObject.transform.position = position;
        impactObject.transform.rotation = RandomRotation();
    }

    protected virtual GameObject GetObject()
    {
        return Instantiate(objectPrefab);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
