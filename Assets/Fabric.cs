using RoboRyanTron.Unite2017.Variables;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;

public class Fabric : MonoBehaviour
{
    public int VertexCount { get { return (Width + 1) * (Height + 1); } }
    public int SegmentCount
    {
        get
        {
            return ((Width + 1) * Height) +
                ((Height + 1) * Width);
        }
    }

    [Header("Parameters")]
    [Range(1, 200)]
    public int BatchSize = 24;
    [Range(1, 200)]
    public int IterationsPerFrame = 16;
    public float SegmentLength;
    [Range(0f, 100f)]
    public float MaxVelocity = 1000f;
    [Range(1, 100)]
    public int Width;
    [Range(0, 100)]
    public int Height = 0;
    public List<VerletAttatchment> Attatchments;
    public Vec3 Gravity = new Vec3(0, -3f, 0);
    public float GroundHeight = -1000f;

    [Header("Experimental")] 
    public float VerletFactor = 1f;
    public float RbodyForceFactor = 1f;
    public ForceMode RbodyForceMode;

    public NativeArray<VerletVertex> _nVertices;
    private NativeArray<VerletVertex> _nVerticesRO;
    // segments constraints, vertical / horizontal
    private NativeArray<Vec3> _nHorzConstraints;
    private NativeArray<Vec3> _nVertConstraints;

    // Start is called before the first frame update
    void Start()
    {
        InitializeVertices();
    }


    //int fixedUpdateCnt = 0;
    int updateCnt = 0;
    //private void FixedUpdate()
    //{
    //    fixedUpdateCnt++;
    //    print("FixedUpdate: " + fixedUpdateCnt);
    //}


    JobHandle _jobHandle, _findConstraintHandle, _applyConstraintHandle;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (updateCnt >= 1)
            CompleteJobs();
        updateCnt++;

        //print("Update: " + updateCnt);
        UpdateTransformPositions();
        // copy of vertices so that jobs can read synchronously 
        _nVerticesRO = new NativeArray<VerletVertex>(_nVertices, Allocator.TempJob);

        _nHorzConstraints = new NativeArray<Vec3>(Width * (Height + 1), Allocator.TempJob);
        _nVertConstraints = new NativeArray<Vec3>(Height * (Width + 1), Allocator.TempJob);


        var verlet = new VerletJob()
        {
            Vertices = _nVertices,
            GroundHeight = GroundHeight,
            Gravity = Gravity,
            DeltaTime = Time.deltaTime,
            VerletFactor = VerletFactor,
            MaxVelocity = MaxVelocity
        };

        var findConstraints = new CalculateConstraints()
        {
            Vertices = _nVertices,
            ConstraintLength = SegmentLength,
            HorzConstraints = _nHorzConstraints,
            VertConstraints = _nVertConstraints,
            Width = Width,
            Height = Height
        };

        var applyConstraints = new ApplyConstraints()
        {
            Vertices = _nVertices,
            VerticesRO = _nVerticesRO,
            HorzConstraints = _nHorzConstraints,
            VertConstraints = _nVertConstraints,
            Width = Width,
            Height = Height
        };


        _jobHandle = verlet.Schedule(VertexCount, BatchSize);

        for (int i = 0; i < IterationsPerFrame; i++)
        {
            _jobHandle = findConstraints.Schedule(VertexCount, BatchSize, _jobHandle);
            _jobHandle = applyConstraints.Schedule(VertexCount, BatchSize, _jobHandle);
        }

        //_findConstraintHandle = findConstraints.Schedule(SegmentCount + 1, 1, _jobHandle);
        //_applyConstraintHandle = applyConstraints.Schedule(SegmentCount + 1, 1, _findConstraintHandle);
    }

    private void LateUpdate()
    {

    }

    private void CompleteJobs()
    {
        if (!_nVertices.IsCreated)
            return;

        _jobHandle.Complete();
        ApplyRbodyForces();
        for (int i = 0; i < _nVertices.Length - 1; i++)
        {
            int row = i / (Width + 1);
            int col = i % (Width + 1);

            if (col < Width)
            {

                //Debug.DrawLine(_nVertices[i].Pos, _nVertices[i + 1].Pos,
                //Color.Lerp(Color.cyan, Color.magenta, (float)i / ((float)_nVertices.Length - 1f)));
                Debug.DrawLine(_nVertices[i].Pos, _nVertices[i + 1].Pos, Color.red);
            }

            if (row < Height)
            {
                //Debug.DrawLine(_nVertices[i].Pos, _nVertices[i + (Width + 1)].Pos,
                //    Color.Lerp(Color.cyan, Color.magenta, (float)i / ((float)_nVertices.Length - 1f)));
                Debug.DrawLine(_nVertices[i].Pos, _nVertices[i + (Width + 1)].Pos, Color.green);

            }

            if (_nVertices[i].Type == VertexType.Verlet)
            {

            }


        }

        _nVerticesRO.Dispose();
        _nHorzConstraints.Dispose();
        _nVertConstraints.Dispose();
    }

    void ApplyRbodyForces()
    {
        var rbodys = Attatchments.FindAll(x => x.Rbody != null);
        foreach (var rbodyAtch in rbodys)
        {
            Vec3 force = _nVertices[GetVertexIndex(rbodyAtch.Coord)].Pos - rbodyAtch.Rbody.position;
            rbodyAtch.Rbody.AddForce(force, RbodyForceMode);
            //print(force.magnitude);
        }
    }

    void UpdateTransformPositions()
    {
        for (int i = 0; i < Attatchments.Count; i++)
        {
            var vertex = _nVertices[GetVertexIndex(Attatchments[i].Coord)];
            if (vertex.Type == VertexType.Transform)
            {
                vertex.Pos = Attatchments[i].Transform.position;
                _nVertices[GetVertexIndex(Attatchments[i].Coord)] = vertex;
            }
            else if (vertex.Type == VertexType.Rbody)
            {
                vertex.Pos = Attatchments[i].Rbody.position;
                _nVertices[GetVertexIndex(Attatchments[i].Coord)] = vertex;
            }

        }
    }

    public int GetVertexIndex(Vector2Int coord)
    {
        return ((Width + 1) * coord.y) + coord.x;
    }

    void InitializeVertices()
    {
        _nVertices = new NativeArray<VerletVertex>(VertexCount, Allocator.Persistent);

        // add each attachment at its coordinates
        foreach (var atch in Attatchments)
        {
            int index = (atch.Coord.y * (Width + 1)) + atch.Coord.x;

            var type = (atch.Rbody == null) ? VertexType.Transform : VertexType.Rbody;
            var pos = (type == VertexType.Rbody) ?
                atch.Rbody.position : atch.Transform.position;
            _nVertices[index] = new VerletVertex()
            {
                Type = type,
                Pos = pos,
                OldPos = pos
            };
        }

        for (int i = 0; i < VertexCount; i++)
        {
            // if this has been set as an attachment, skip
            if (_nVertices[i].Type != VertexType.Verlet)
                continue;

            int row = i / (Width + 1);
            int col = i % (Width + 1);

            _nVertices[i] = new VerletVertex()
            {
                Type = VertexType.Verlet,
                Pos = transform.position /*+ new Vector3(col * SegmentLength, -row * SegmentLength, 0)*/,
                OldPos = transform.position
            };
        }

        //for (int i = 0; i < Width + 1; i++)
        //{
        //    bool skip = false;
        //    foreach (var atch in Attatchments)
        //    {
        //        // setup an attachment vertex
        //        if (atch.VertexIndex == i)
        //        {
        //            var type = (atch.Rbody == null) ? VertexType.Transform : VertexType.Rbody;
        //            var pos = (type == VertexType.Rbody) ? 
        //                atch.Rbody.position: atch.Transform.position ;
        //            _nVertices[i] = new VerletVertex()
        //            {
        //                Type = type,
        //                Pos = pos,
        //                OldPos = pos
        //            };
        //            skip = true;
        //            break;
        //        }  
        //    }

        //    // setup a normal verlet vertex
        //    if (!skip)
        //    {
        //        _nVertices[i] = new VerletVertex()
        //        {
        //            Type = VertexType.Verlet,
        //            Pos = transform.position,
        //            OldPos = transform.position
        //        };
        //    }        
        //}
    }

    private void OnDisable()
    {
        _jobHandle.Complete();
        try
        {
            _nVerticesRO.Dispose();
            _nHorzConstraints.Dispose();
            _nVertConstraints.Dispose();
            _nVertices.Dispose();
        } catch (Exception e)
        {
            Debug.LogWarning(e);
        }

    }

}
