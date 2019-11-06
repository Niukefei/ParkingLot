using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 汽车移动
public class Car : MonoBehaviour
{
    // 车速
    public float Speed = 30f;
    // 默认一格尺寸
    private readonly int GridSize = 10;
    // 转向时间（固定）
    private float TurnTime;
    // 停车路线点
    [HideInInspector]
    public List<Vector3> ComePathPos = new List<Vector3>();
    // 离开路线点
    [HideInInspector]
    public List<Vector3> OutPathPos = new List<Vector3>();
    private int index;

    // 使用车位信息
    [HideInInspector]
    public ParkingPoint parkingPoint;

    // 状态类型（true:进入 false:离开）
    private bool isCome;

    // 状态步骤（0:开始移动步骤 1:移动步骤完成 2:全部步骤完成）
    [HideInInspector]
    public int StateStep;
    // 车身
    [HideInInspector]
    public int CarTypeIndex;//**
    Transform carBody;
    Animation anim;

    void Awake()
    {
        //carBody = transform.GetChild(0);
        //anim = transform.GetComponent<Animation>();
        TurnTime = Mathf.PI * GridSize * 0.5f / Speed * 1.1f;
    }

    public void Refresh(int index)
    {
        CarTypeIndex = index;
        carBody = transform.GetChild(CarTypeIndex).GetChild(0);//**
        anim = transform.GetChild(CarTypeIndex).GetComponent<Animation>();//**
    }

    private void Start()
    {
        carBody = transform.GetChild(CarTypeIndex).GetChild(0);//**
    }

    void Update()
    {
        if (StateStep == 1)
        {
            if (isCome)
                MoveCar(ComePathPos);
            else
                MoveCar(OutPathPos);
        }
    }

    /// <summary>
    /// 汽车进入
    /// </summary>
    public void ComeCar()
    {
        anim.Play();
        isCome = true;
        index = 0;
        transform.LookAt(ComePathPos[index]);
        MoveCar(ComePathPos);
    }

    /// <summary>
    /// 汽车离开
    /// </summary>
    public void OutCar()
    {
        anim.Play();
        if (StateStep != 2) return;
        ParkingMgr.Instance.SetParkingPoint(parkingPoint);
        parkingPoint = null;
        isCome = false;
        index = 0;// 重置步骤index
        MoveCar(OutPathPos);
    }

    // 移动 & 状态
    private void MoveCar(List<Vector3> PathPos)
    {
        if (index != PathPos.Count)
        {
            StateStep = 0;
            CircleStep1(PathPos);
        }
        else
        {
            StateStep = 2;// 完成
            anim.Stop();
            if (isCome)
            {
                //TODO 等待一段时间离开 else 直接调用方法离开
                //CarOut();
            }
            else
                End();
        }
    }

    // 圆角转弯
    private void CircleStep1(List<Vector3> PathPos)
    {
        if (index < PathPos.Count - 1)
        {
            // 不是最后一步
            Vector3 totalVector = PathPos[index] - transform.position;// 总向量（两点之差）
            Vector3 curToword = (totalVector).normalized;// 当前方向
            float mag = totalVector.magnitude - GridSize;// 要走的距离
            Vector3 moveVector = curToword * (mag);// 要走的向量
            transform.DOMove(transform.position + moveVector, mag / Speed).SetEase(Ease.Linear).OnComplete(() => { CircleStep2(PathPos); });
        }
        else
        {
            // 是最后一步
            float time = isCome ? 0.8f : (PathPos[index] - transform.position).magnitude / Speed;
            transform.DOMove(PathPos[index], time).SetEase(Ease.Linear).OnComplete(() =>
            {
                index++;
                StateStep = 1;
            });
        }
    }
    private void CircleStep2(List<Vector3> PathPos)
    {
        Vector3 nextToword = (PathPos[index + 1] - PathPos[index]).normalized;// 下一个方向
        Vector3 turnPos = transform.position + nextToword * GridSize;// 旋转点
        Vector3 tempPos = transform.position;// 记录父节点位置
        transform.position = turnPos;
        carBody.position = tempPos;

        // 朝向
        Vector3 lookAt = Vector3.zero;
        if (isCome)
            lookAt = transform.position + nextToword;
        else
        {
            if (index < 1)// 如果是倒车的转向 朝向与坐标方向相反
                lookAt = transform.position - nextToword;
            else if (index == 1)// 倒车完成后省略转向动作
            {
                lookAt = transform.position + nextToword;
                transform.LookAt(lookAt);
                CarReset();
                return;
            }
            else
                lookAt = transform.position + nextToword;
        }

        // 转向动作
        transform.DOLookAt(lookAt, TurnTime).SetEase(Ease.Linear).OnComplete(CarReset);
    }
    // 汽车复位
    private void CarReset()
    {
        transform.position = carBody.position;
        carBody.localPosition = Vector3.zero;
        index++;
        StateStep = 1;
    }

    // 结束
    private void End()
    {
        //Destroy(gameObject);
        ResetCar();
        MF_AutoPool.Despawn(transform.GetComponent<AP_Reference>());
    }
    // 重置
    private void ResetCar()
    {
        ComePathPos = null;
        OutPathPos = null;
        isCome = false;
        StateStep = 0;
        anim.Stop();
        carBody.transform.localPosition = Vector3.zero;
        transform.GetChild(CarTypeIndex).gameObject.SetActive(false);//**
        CarTypeIndex = 0;
    }
}