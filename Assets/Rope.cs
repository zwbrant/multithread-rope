using RoboRyanTron.Unite2017.Variables;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;

public class Rope : MonoBehaviour
{
    [Header("Parameters")]
    public float SegmentLength;
    public int SegmentCount;
    public List<VerletAttatchment> Attatchments;
    public FloatVariable Gravity;
    public FloatVariable GroundHeight;

    [Header("References")]
    public LineRenderer LineRenderer;

    private NativeArray<VerletVertex> _nVertices;
    private NativeArray<VerletVertex> _nVerticesRO;
    private NativeArray<Vec3> _nConstraintVerts;


    // Start is called before the first frame update
    void Start()
    {
        InitializeVertices();
    }

    JobHandle _verletHandle, _findConstraintHandle, _applyConstraintHandle;

    // Update is called once per frame
    void Update()
    {
        UpdateTransformPositions();
        _nVerticesRO = new NativeArray<VerletVertex>(_nVertices, Allocator.TempJob);
        _nConstraintVerts = new NativeArray<Vec3>(SegmentCount, Allocator.TempJob);

        var verlet = new VerletJob()
        {
            GroundHeight = GroundHeight.Value,
            Gravity = Gravity.Value,
            DeltaTime = Time.deltaTime,
            Vertices = _nVertices
        };

        var findConstraints = new CalculateConstraints()
        {
            constraintLength = SegmentLength,
            vertices = _nVertices,
            constraintVerts = _nConstraintVerts
        };

        var applyConstraints = new ApplyConstraints()
        {
            constraintVerts = _nConstraintVerts,
            verticesRO = _nVerticesRO,
            vertices = _nVertices
        };

        _verletHandle = verlet.Schedule(SegmentCount + 1, 1);
        _findConstraintHandle = findConstraints.Schedule(SegmentCount + 1, 1, _verletHandle);
        _applyConstraintHandle = applyConstraints.Schedule(SegmentCount + 1, 1, _findConstraintHandle);
    }

    private void LateUpdate()
    {
        _applyConstraintHandle.Complete();

        for (int i = 0; i < _nVertices.Length - 1; i++)
        {
            Debug.DrawLine(_nVertices[i].Pos, _nVertices[i + 1].Pos, 
                Color.Lerp(Color.cyan, Color.magenta, (float)i / ((float)_nVertices.Length - 1f)));
        }

        _nVerticesRO.Dispose();
        _nConstraintVerts.Dispose();
    }

    void UpdateTransformPositions()
    {
        for (int i = 0; i < Attatchments.Count; i++)
        {
            var vertex = _nVertices[Attatchments[i].VertexIndex];
            if (vertex.Type == VertexType.Transform)
            {
                vertex.Pos = Attatchments[i].Transform.position;
                _nVertices[Attatchments[i].VertexIndex] = vertex;  
            }
                
        }
    }

    void InitializeVertices()
    {
        _nVertices = new NativeArray<VerletVertex>(SegmentCount + 1, Allocator.Persistent);

        for (int i = 0; i < SegmentCount + 1; i++)
        {
            bool skip = false;
            foreach (var atchment in Attatchments)
            {
                if (atchment.VertexIndex == i)
                {
                    _nVertices[i] = new VerletVertex()
                    {
                        Type = VertexType.Transform,
                        Pos = atchment.Transform.position,
                        OldPos = atchment.Transform.position
                    };
                    skip = true;
                    break;
                }  
            }

            if (!skip)
            {
                _nVertices[i] = new VerletVertex()
                {
                    Type = VertexType.Verlet,
                    Pos = transform.position + Vec3.right * i,
                    OldPos = transform.position + Vec3.right * i
                };
            }        
        }
    }

    private void OnDisable()
    {
        _nVertices.Dispose();
    }

}
