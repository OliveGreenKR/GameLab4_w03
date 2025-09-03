using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class posData//이동할 방향과 시간 저장
{
    public bool moveGlobal;
    public float moveSec;
    public float moveSpeed;
    public Vector3 moveDir;
    public Vector3 rotateDir;
}
public class obstacleMove : MonoBehaviour
{
    public bool repeat = true;//이동후 되돌아옴 여부
    private int moveMax;//배열길이
    private int frontAndBack = 1;// 1=front -1=back 
    public int movingNum=0;//이동중인 posData
    public float movingSec;//이동중인 시간
    private int test=1;
    public List<posData> movePos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //첫번째 값으로 세팅
        if (movePos.Count > 0)
        {
            moveMax = movePos.Count;
            Debug.Log(moveMax);
            movingSec = movePos[movingNum].moveSec;//이동중시간을 입력한 첫번째 시간으로
        }
            
    }

    // Update is called once per frame
    void Update()
    {
        
        if (movingSec <= 0)//이동시간이 다되었을 때
        {

            if (movingNum + frontAndBack > moveMax-1 || movingNum+frontAndBack < 0)//다음 이동순서가 없다면
            {
                if (repeat)//반복 활성화시
                {
                    frontAndBack *= -1;//이동 방향 전환
                }
                else
                {
                    movingNum = 0;//초기 이동으로
                }
            }
            else//정상 이동중이면
            {
                movingNum += frontAndBack; //방향에 따라 다음 이동+-1
            }
            movingSec = movePos[movingNum].moveSec;//다음 이동 시간 적용
        }
        else//이동시간 남으면
        {
            movingSec -= Time.deltaTime;//이동시간 감소
        }
        if (movePos[movingNum].moveGlobal)
        {
            transform.Translate(movePos[movingNum].moveDir * movePos[movingNum].moveSpeed * Time.deltaTime * frontAndBack,Space.World);
        }
        else
        {
            transform.Translate(movePos[movingNum].moveDir * movePos[movingNum].moveSpeed * Time.deltaTime * frontAndBack);
        }        
        transform.Rotate(movePos[movingNum].rotateDir * Time.deltaTime * frontAndBack);
    }
    
}
