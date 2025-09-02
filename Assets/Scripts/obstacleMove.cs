using UnityEngine;

public class obstacleMove : MonoBehaviour
{
    public float moveSpeed;
    public Vector3 moveDir= Vector3.forward;
    public Vector3 rotateDir;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(moveDir * moveSpeed*Time.deltaTime);
        transform.Rotate(rotateDir* Time.deltaTime);
    }
}
