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
    public float GroundHeight = 0f;
    public List<VerletVertex> Points;
    public List<GameObject> PointGOs;
    public List<float> Beams;

    private JobHandle _verletHandle;
    private JobHandle _beamsHandle;

    void Start()
    {
        // print("Start2");

    }

    private NativeArray<float> _nBeams;
    private NativeArray<VerletVertex> _nPoints;


    void Update()
    {
        // creating new NArray every frame from the inspector list
        _nBeams = new NativeArray<float>(Beams.ToArray(), Allocator.TempJob);
        _nPoints = new NativeArray<VerletVertex>(Points.ToArray(), Allocator.TempJob);

        var job1 = new BeamsJob()
        {
            Points = _nPoints,
            Beams = _nBeams,
            DeltaTime = Time.deltaTime,
            Gravity = Gravity
        };

        //var job2 = new VerletJob()
        //{
        //    Points = _nPoints,
        //    DeltaTime = Time.deltaTime,
        //    Gravity = Gravity
        //};


        _beamsHandle = job1.Schedule(_nBeams.Length, 1);
        //erletHandle = job2.Schedule(_nPoints.Length, 1, _beamsHandle);





    }

    public void LateUpdate()
    {
        _verletHandle.Complete();


        for (int i = 0; i < Points.Count; i++)
        {
            if (PointGOs.Count <= i)
                break;

            // set 3D point position
            PointGOs[i].transform.position = _nPoints[i].Pos;
            // update non-native point list
            Points[i] = _nPoints[i];
        }

        _nBeams.Dispose();
        _nPoints.Dispose();
    }


    public void CompleteVerletJob()
    {

    }

    public void CompleteBeamsJob()
    {
        _beamsHandle.Complete();
    }

    public void OnDestroy()
    {
        try
        {

        } catch{}
    }
}




public struct BeamsJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> Beams;
    [NativeDisableParallelForRestriction]
    public NativeArray<VerletVertex> Points;
    public float Gravity;
    public float DeltaTime;

    public void Execute(int i)
    {
        //if (i >= Points.Length - 1)
        //    return;

        //var p1 = Points[i];
        //var p2 = Points[i + 1];

        //float3 deltaVect = p2.Pos - p1.Pos;
        //float deltaLength = length(deltaVect);

        //float diff = deltaLength - Beams[i];

        //deltaVect = normalize(deltaVect);

        //p1.Pos += deltaVect * (diff * .5f);
        //p2.Pos -= deltaVect * (diff * .5f);

        //// re-asign Points
        //Points[i] = p1;
        //Points[i + 1] = p2;
    }


}