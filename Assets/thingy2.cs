using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using static Unity.Mathematics.math;
using Unity.Mathematics;

public class thingy2 : MonoBehaviour
{
    public float Gravity = -1f;
    public List<GameObject> PointGOs;
    public NativeList<Point> Points;



    void Start()
    {
        // print("Start2");
        Points = new NativeList<Point>(Allocator.Persistent);
        Points.Add(new Point() { OldPos = new float3(0, 5, 0), Pos = new float3(0, 5, 0) });

    }

    void Update()
    {

        var job = new VerletJob() {
            DeltaTime = Time.deltaTime,
            Points = Points,
            Gravity = Gravity
        };

        var handle = job.Schedule(Points.Length, 1);

        handle.Complete();

        // transform GOs based on new points
        for (int i = 0; i < PointGOs.Count; i++)
        {
            if (Points.Length <= i)
                break;

            PointGOs[i].transform.position = Points[i].Pos;
        }


    }
}


// all the data needed to represent a Verlet point/particle 
public struct Point
{
    public float3 Pos;
    public float3 OldPos;
}

public struct VerletJob : IJobParallelFor
{
    public NativeArray<Point> Points;
    public float Gravity;
    public float DeltaTime;

    public void Execute(int i)
    {
        UpdateVerlet(i);
    }

    public void UpdateVerlet(int i)
    {
        var point = Points[i];
      
        float3 velocity = point.Pos - point.OldPos;
        velocity.y += Gravity;

        point.OldPos = point.Pos;
        point.Pos = point.Pos + velocity;

        Points[i] = point;
    }
}
