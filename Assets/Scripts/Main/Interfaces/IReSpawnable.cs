using UnityEngine;

public interface IReSpawnable
{
    [SerializeField] public Vector3 LastSpawnPosition { get; }

    public bool ReSpawn(Vector3 worldPosition, Quaternion worldRotation);

}
