using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 停车场管理器
public class ParkingMgr : MonoBehaviour
{
    [Space()]
    // 出生点
    public Transform StartPoint;
    // 销毁点
    public Transform EndPoint;

    [Space()]
    // 进入转弯点
    public List<Transform> ComeTurningPoint = new List<Transform>();
    // 停车位点
    public List<ParkingPoint> ParkingPoints = new List<ParkingPoint>();
    // 离开转弯点
    public List<Transform> OutTurningPoint = new List<Transform>();

    [Space()]
    // 车位牌
    public TextMeshPro CanParkingNum;

    // 汽车
    //public List<GameObject> CarPrefabs = new List<GameObject>();
    public GameObject CarPrefab;//*
    private Transform ParkingCars;
    // 车速
    public float CarSpeed;
    // 停车场容器
    private List<GameObject> CarList = new List<GameObject>();
    [Space()]
    // 汽车对象池
    public Transform CarPools;
    private AP_Manager p_Manager;

    float add;
    float sub;
    public float addInterval;
    public float subInterval;

    // 默认一格尺寸
    private readonly int GridSize = 10;

    public static ParkingMgr Instance;

    private void Awake()
    {
        Instance = this;
        ParkingCars = transform.Find("ParkingCars");
        p_Manager = CarPools.GetComponent<AP_Manager>();
        InitPools();
    }

    // 初始化汽车对象池
    private void InitPools()
    {
        int max = ParkingPoints.Count;
        CanParkingNum.text = max.ToString();
        //for (int i = 0; i < CarPrefabs.Count; i++)
        //{
        //    p_Manager.CreatePool(CarPrefabs[i], 0, max * 2, AP_enum.EmptyBehavior.Grow, AP_enum.MaxEmptyBehavior.ReuseOldest);
        //}
        //p_Manager.CreatePool(CarPrefab, 0, max * 2, AP_enum.EmptyBehavior.Grow, AP_enum.MaxEmptyBehavior.ReuseOldest);//**
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            AddCar();
        if (Input.GetKeyDown(KeyCode.S))
            SubCar();
        if (Input.GetKeyDown(KeyCode.D))
            removeAllCar();
        //CanParkingNum.text = ParkingPoints.Count.ToString();

        add += Time.deltaTime;
        sub += Time.deltaTime;
        if (add >= addInterval)
        {
            add = 0;
            AddCar();
        }
        if (sub >= subInterval)
        {
            sub = 0;
            SubCar();
        }
    }

    /// <summary>
    /// 创建一个小汽车
    /// </summary>
    public void AddCar()
    {
        if (CheckParkingIsFull()) return;// 检查

        //GameObject car = Instantiate(Car, ParkingCars);
        //GameObject car = MF_AutoPool.Spawn(Car);
        //int random = Random.Range(0, CarPools.childCount);
        //AP_Pool pool = CarPools.GetChild(random).GetComponent<AP_Pool>();
        AP_Pool pool = CarPools.GetComponent<AP_Pool>();//**
        GameObject car = pool.Spawn();
        int index = Random.Range(0, car.transform.childCount);//**
        car.transform.GetChild(index).gameObject.SetActive(true);//**

        car.transform.SetParent(ParkingCars);
        car.transform.position = StartPoint.position;
        CarList.Add(car);

        ParkingPoint parkingPoint = GetParkingPoint();// 获得车位信息
        List<Vector3> OutPath;
        List<Vector3> ComePath = PlanPath(parkingPoint, out OutPath);// 规划路径

        Car carScripts = car.GetComponent<Car>();
        carScripts.Refresh(index);
        carScripts.Speed = CarSpeed;
        carScripts.ComePathPos = ComePath;
        carScripts.OutPathPos = OutPath;
        carScripts.parkingPoint = parkingPoint;
        carScripts.ComeCar();
    }

    /// <summary>
    /// 移除一个小汽车
    /// </summary>
    public void SubCar()
    {
        if (CarList.Count <= 0) return;
        Car car = CarList[0].GetComponent<Car>();
        if (car.StateStep == 2)
        {
            CarList.RemoveAt(0);
            car.OutCar();
        }
    }

    /// <summary>
    /// 清空当前停车场的小汽车
    /// </summary>
    public void removeAllCar()
    {
        for (int i = CarList.Count - 1; i >= 0; i--)
        {
            Car car = CarList[i].GetComponent<Car>();
            if (car.StateStep == 2)
            {
                CarList.RemoveAt(i);
                car.OutCar();
            }
        }
    }

    // 路径
    private List<Vector3> PlanPath(ParkingPoint parkingPoint, out List<Vector3> OutPath)
    {
        // 进入路径
        List<Vector3> ComePath = new List<Vector3>();
        AddPoint(ComePath, ComeTurningPoint);// 进入拐弯点
        Vector3 ParkingPoint = parkingPoint.transform.position;
        ComePath.Add(new Vector3(ParkingPoint.x, 0, ComeTurningPoint[ComeTurningPoint.Count - 1].position.z));// 计算车位栏点
        ComePath.Add(ParkingPoint);// 车位点
        float x = parkingPoint.pointType == 0 ? -GridSize * 1.5f : GridSize * 1.5f;// 暂时通过索引区分左右
        ComePath.Add(ParkingPoint + new Vector3(x, 0, 0));// 停车点

        // 离开路径
        OutPath = new List<Vector3>();
        OutPath.Add(ParkingPoint);
        OutPath.Add(ParkingPoint + new Vector3(0, 0, GridSize));// 倒车点
        OutPath.Add(new Vector3(ParkingPoint.x, 0, OutTurningPoint[0].position.z));
        AddPoint(OutPath, OutTurningPoint);
        OutPath.Add(EndPoint.position);
        return ComePath;
    }
    private void AddPoint(List<Vector3> Path, List<Transform> Points)
    {
        for (int i = 0; i < Points.Count; i++)
        {
            Path.Add(Points[i].position);
        }
    }

    /// <summary>
    /// 使用停车点
    /// </summary>
    public ParkingPoint GetParkingPoint()
    {
        int index = Random.Range(0, ParkingPoints.Count);// 随机车位
        ParkingPoint parkingPoint = ParkingPoints[index];// 车位信息
        ParkingPoints.RemoveAt(index);
        CanParkingNum.text = ParkingPoints.Count.ToString();
        return parkingPoint;
    }

    /// <summary>
    /// 归还停车点
    /// </summary>
    public void SetParkingPoint(ParkingPoint parkingPoint)
    {
        ParkingPoints.Add(parkingPoint);
        CanParkingNum.text = ParkingPoints.Count.ToString();
    }

    // 检查所有车位是否已满
    private bool CheckParkingIsFull()
    {
        bool b = ParkingPoints.Count <= 0;
        //if (b)
        //    print("车位已满");
        return b;
    }
}