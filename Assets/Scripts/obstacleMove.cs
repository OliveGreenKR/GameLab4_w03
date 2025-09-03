using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class posData//�̵��� ����� �ð� ����
{
    public bool moveGlobal;
    public float moveSec;
    public float moveSpeed;
    public Vector3 moveDir;
    public Vector3 rotateDir;
}
public class obstacleMove : MonoBehaviour
{
    public bool repeat = true;//�̵��� �ǵ��ƿ� ����
    private int moveMax;//�迭����
    private int frontAndBack = 1;// 1=front -1=back 
    public int movingNum=0;//�̵����� posData
    public float movingSec;//�̵����� �ð�
    private int test=1;
    public List<posData> movePos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //ù��° ������ ����
        if (movePos.Count > 0)
        {
            moveMax = movePos.Count;
            Debug.Log(moveMax);
            movingSec = movePos[movingNum].moveSec;//�̵��߽ð��� �Է��� ù��° �ð�����
        }
            
    }

    // Update is called once per frame
    void Update()
    {
        
        if (movingSec <= 0)//�̵��ð��� �ٵǾ��� ��
        {

            if (movingNum + frontAndBack > moveMax-1 || movingNum+frontAndBack < 0)//���� �̵������� ���ٸ�
            {
                if (repeat)//�ݺ� Ȱ��ȭ��
                {
                    frontAndBack *= -1;//�̵� ���� ��ȯ
                }
                else
                {
                    movingNum = 0;//�ʱ� �̵�����
                }
            }
            else//���� �̵����̸�
            {
                movingNum += frontAndBack; //���⿡ ���� ���� �̵�+-1
            }
            movingSec = movePos[movingNum].moveSec;//���� �̵� �ð� ����
        }
        else//�̵��ð� ������
        {
            movingSec -= Time.deltaTime;//�̵��ð� ����
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
