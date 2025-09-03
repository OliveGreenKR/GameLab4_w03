using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class posData//이동할 방향과 시간 저장
{
    public string playerName;
    public float moveSec;
    public float moveSpeed;
    public Vector3 moveDir;
    public Vector3 rotateDir;
}
public class obstacleMove : MonoBehaviour
{
    public bool comeBack;//이동후 되돌아옴 여부
    public int moveNum;//배열길이
    private int movingNum;//이동중인 posData
    private float movingSec;//이동중인 시간
    public List<posData> movePos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveNum = movePos.Count;
        movingSec = movePos[0].moveSec;
    }

    // Update is called once per frame
    void Update()
    {
        if (movingSec<=0)
        {

            movingNum++;
        }
        
        transform.Translate(movePos[movingNum].moveDir * movePos[movingNum].moveSpeed * Time.deltaTime);
        transform.Rotate(movePos[movingNum].rotateDir * Time.deltaTime);
        movePos[movingNum].moveSec -=Time.deltaTime ;

    }
}
