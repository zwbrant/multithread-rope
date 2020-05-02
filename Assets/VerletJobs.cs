using RoboRyanTron.Unite2017.Variables;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;


[System.Serializable]
public struct VerletAttatchment
{
    public Transform Transform;
    public int VertexIndex;
}

// all the data needed to represent a Verlet point/particle 
[System.Serializable]
public struct VerletVertex
{
    public VertexType Type;
    public Vec3 Pos;
    public Vec3 OldPos;
    public Vec3 RbodyForces;
}

public enum VertexType
{
    Verlet,
    Rbody,
    Transform
}

public struct VerletJob : IJobParallelFor
{
    public NativeArray<VerletVertex> Vertices;
    public float GroundHeight;
    public float Gravity;
    public float DeltaTime;

    public void Execute(int i)
    {
        UpdateVerlet(i);
    }

    public void UpdateVerlet(int i)
    {
        var point = Vertices[i];
        if (point.Type != VertexType.Verlet)
            return;

        Vec3 velocity = point.Pos - point.OldPos;

        // add gravity
        velocity.y += Gravity;

        // scale to frametime
        velocity = velocity * Mathf.Pow(DeltaTime, 2);

        // apply velocity to current position
        point.OldPos = point.Pos;
        point.Pos = point.Pos + velocity;

        // force point to stay above ground
        if (point.Pos.y < GroundHeight)
            point.Pos.y = GroundHeight;

        Vertices[i] = point;
    }
}


public struct ApplyConstraints : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Vec3> constraintVerts;
    [ReadOnly]
    public NativeArray<VerletVertex> verticesRO;
    public NativeArray<VerletVertex> vertices;

    public void Execute(int i)
    {
        VerletVertex vertex = vertices[i];
        Vec3 changeVect = Vec3.zero;

        // add vert from last segment
        if (i != 0 && vertex.Type != VertexType.Transform)
        {
            var lastVert = verticesRO[i - 1];
            // change this vert in proportion to the next one's mass
            switch (lastVert.Type)
            {
                case VertexType.Rbody:
                    // not implemented
                    break;
                case VertexType.Transform:
                    changeVect -= constraintVerts[i - 1];
                    break;
                default:
                    changeVect -= constraintVerts[i - 1] / 2;
                    break;
            }
        }
        // add vert from next segment
        if (i != vertices.Length - 1 && vertex.Type != VertexType.Transform)
        {
            var nextVert = verticesRO[i + 1];
            // change this vert in proportion to the next one's mass
            switch (nextVert.Type)
            {
                case VertexType.Rbody:
                    // not implemented
                    break;
                case VertexType.Transform:
                    changeVect += constraintVerts[i];
                    break;
                default:
                    changeVect += constraintVerts[i] / 2;
                    break;
            }
        }

        vertex.Pos += changeVect;
        vertices[i] = vertex;
    }
}

// Provides vectors that describe the delta between 
// two vertices and their constrained positions
public struct CalculateConstraints : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<VerletVertex> vertices;
    public NativeArray<Vec3> constraintVerts;

    public float constraintLength;

    public void Execute(int i)
    {
        if (i == vertices.Length - 1)
            return;

        Vec3 vDiff = vertices[i + 1].Pos - vertices[i].Pos;

        float diffFactor = (vDiff.magnitude - constraintLength) / vDiff.magnitude;

        if (diffFactor <= 0)
            constraintVerts[i] = Vec3.zero;
        else
            constraintVerts[i] = diffFactor * vDiff;

    }
}