using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class posData//�̵��� ����� �ð� ����
{
    public float moveSec=5f;
    public float moveSpeed;
    public Vector3 moveDir;
    public Vector3 rotateDir;
}
public class obstacleMove : MonoBehaviour
{
    public bool comeBack;//�̵��� �ǵ��ƿ� ����
    public int moveMax;//�迭����
    private int frontAndBack = 1;// 1=front -1=back 
    private int movingNum=0;//�̵����� posData
    private float movingSec;//�̵����� �ð�
    public List<posData> movePos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //ù��° ������ ����
        moveMax = movePos.Count;
        movingSec = movePos[0].moveSec;
    }

    // Update is called once per frame
    void Update()
    {
        if (moveMax > 0)
        {
            if (movingSec <= 0)//�̵��ð��� �ٵǸ�
            {
                if (frontAndBack == 1)
                {
                    movingNum++;
                }
                else if (frontAndBack == -1)
                {
                    movingNum--;
                }

                if (movingNum > moveMax || movingNum < 0)//�̵������� ������ �Ѿ��
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
                movingSec = movePos[movingNum].moveSec;//���� �̵� �ð� ����
            }
            else//�̵��ð� ������
            {
                movingSec -= Time.deltaTime;//�̵��ð� ����
            }



            transform.Translate(movePos[movingNum].moveDir * movePos[movingNum].moveSpeed * Time.deltaTime * frontAndBack);
            transform.Rotate(movePos[movingNum].rotateDir * Time.deltaTime * frontAndBack);

        }
    }
}
