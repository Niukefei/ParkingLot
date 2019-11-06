using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Test : MonoBehaviour
{
    public float speed = 30f;

    // 路线点
    [HideInInspector]
    public List<Vector3> PathPos = new List<Vector3>();
    private int index;

    // 状态步骤
    // 0:开始移动步骤 1:移动步骤完成 2:全部步骤完成
    private int StateStep;

    void Start()
    {
    }

    void Update()
    {
        if (StateStep == 1) MoveCar();
    }

    public void MoveCar()
    {
        if (index == PathPos.Count)
        {
            StateStep = 2;
            print("完成");
        }
        else
        {
            StateStep = 0;
            //Turn();
            //Step1();
            CircleStep1();
        }
    }

    #region 转弯方式一（直接拐弯)
    void Turn()
    {
        transform.LookAt(PathPos[index]);
        Move();
    }
    void Move()
    {
        float dis = ((PathPos[index]) - transform.position).magnitude;
        float time = dis / speed;
        transform.DOMove(PathPos[index], time).SetEase(Ease.Linear).OnComplete(() => {
            index++;
            StateStep = 1;
        });
    }
    #endregion

    #region 转弯方式二（直角拐弯)
    void Step1()
    {
        Vector3 toword = (PathPos[index] - transform.position).normalized;
        Sequence sequence = DOTween.Sequence();
        float t = (toword * 5).magnitude / speed;
        sequence.Append(transform.DOLookAt(PathPos[index], t).SetEase(Ease.Linear));
        sequence.Insert(0, transform.DOMove(transform.position + toword * 5, t).SetEase(Ease.Linear));
        sequence.AppendCallback(Step2);
    }
    void Step2()
    {
        Vector3 toword = (PathPos[index] - transform.position).normalized;
        float mag = (PathPos[index] - transform.position).magnitude - 5;
        float t = mag / speed;
        transform.DOMove(transform.position + toword * mag, t).SetEase(Ease.Linear).OnComplete(Step3);
    }
    void Step3()
    {
        if (index == PathPos.Count - 1)
        {
            // 是最后一步
            float mag = (PathPos[index] - transform.position).magnitude;
            float t = mag / speed;
            transform.DOMove(PathPos[index], t).SetEase(Ease.Linear).OnComplete(() => {
                index++;
                StateStep = 1;
            });
        }
        else
        {
            // 不是最后一步
            Vector3 currentToword = (PathPos[index] - transform.position).normalized;// 当前朝向
            Vector3 nextToword = (PathPos[index + 1] - PathPos[index]).normalized;// 下一步朝向
            Vector3 toword = ((currentToword + nextToword) * 0.5f).normalized;// 中间朝向
            float mag = (PathPos[index] - transform.position).magnitude;
            float t = mag / speed;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOLookAt(PathPos[index] + toword * (PathPos[index + 1] - PathPos[index]).magnitude, t).SetEase(Ease.Linear));
            sequence.Insert(0, transform.DOMove(PathPos[index], t).SetEase(Ease.Linear));
            sequence.AppendCallback(() => {
                index++;
                StateStep = 1;
            });
            sequence.SetAutoKill(false);
        }
    }
    #endregion

    #region 转弯方式三（圆角拐弯)
    void CircleStep1()
    {
        transform.LookAt(PathPos[index]);
        if (index == PathPos.Count - 1)
        {
            // 是最后一步
            transform.DOMove(PathPos[index], 1).SetEase(Ease.Linear).OnComplete(() => { index++; });
        }
        else
        {
            // 不是最后一步
            Vector3 totalVector = PathPos[index] - transform.position;// 总向量（两点之差）
            Vector3 curToword = (totalVector).normalized;// 当前方向
            float mag = totalVector.magnitude;// 两点距离
            Vector3 moveVector = curToword * (mag - 10);// 要走的向量
            transform.DOMove(transform.position + moveVector, 3).SetEase(Ease.Linear).OnComplete(CircleStep2);
        }
    }
    void CircleStep2()
    {
        Vector3 nextToword = (PathPos[index + 1] - PathPos[index]).normalized;// 下一个方向
        print(nextToword);
        Vector3 turnPos = transform.position + nextToword * 10;// 旋转点
        Transform child = transform.GetChild(0);
        Vector3 tempPos = transform.position;// 记录父节点位置
        transform.position = turnPos;
        child.position = tempPos;
        // 转向
        transform.DOLookAt(transform.position + nextToword, 1).OnComplete(() => {
            transform.position = child.position;
            child.localPosition = new Vector3(0, 0, 0);
            index++;
            StateStep = 1;
        });
    }
    #endregion
}
