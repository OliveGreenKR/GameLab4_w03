using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class posData//이동할 방향과 시간 저장
{
    public float moveSec=5f;
    public float moveSpeed;
    public Vector3 moveDir;
    public Vector3 rotateDir;
}
public class obstacleMove : MonoBehaviour
{
    public bool comeBack;//이동후 되돌아옴 여부
    public int moveMax;//배열길이
    private int frontAndBack = 1;// 1=front -1=back 
    private int movingNum=0;//이동중인 posData
    private float movingSec;//이동중인 시간
    public List<posData> movePos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //첫번째 값으로 세팅
        moveMax = movePos.Count;
        movingSec = movePos[0].moveSec;
    }

    // Update is called once per frame
    void Update()
    {
        if (moveMax > 0)
        {
            if (movingSec <= 0)//이동시간이 다되면
            {
                if (frontAndBack == 1)
                {
                    movingNum++;
                }
                else if (frontAndBack == -1)
                {
                    movingNum--;
                }

                if (movingNum > moveMax || movingNum < 0)//이동순서가 범위를 넘어가면
                {
                    if (comeBack)
                    {
                        frontAndBack *= -1;
                    }
                    else
                    {
                        movingNum = 0;
                    }
                }
                movingSec = movePos[movingNum].moveSec;//다음 이동 시간 적용
            }
            else//이동시간 남으면
            {
                movingSec -= Time.deltaTime;//이동시간 감소
            }



            transform.Translate(movePos[movingNum].moveDir * movePos[movingNum].moveSpeed * Time.deltaTime * frontAndBack);
            transform.Rotate(movePos[movingNum].rotateDir * Time.deltaTime * frontAndBack);

        }
    }
}
