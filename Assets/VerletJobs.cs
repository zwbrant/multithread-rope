using RoboRyanTron.Unite2017.Variables;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;


[System.Serializable]
public struct VerletAttatchment
{
    public Vector2Int Coord;

    public Transform Transform;
    public Rigidbody Rbody;
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
    public Vec3 Gravity;
    public float DeltaTime;
    public float VerletFactor;
    public float MaxVelocity;

    public void Execute(int i)
    {
        UpdateVerlet(i);
    }

    public void UpdateVerlet(int i)
    {
        var point = Vertices[i];
        if (point.Type != VertexType.Verlet)
            return;

        Vec3 velocity = (point.Pos - point.OldPos) * VerletFactor;

        if (velocity.magnitude > MaxVelocity)
            velocity = velocity * (MaxVelocity / velocity.magnitude);

        // add gravity
        //velocity.y += Gravity;

        // scale to frametime
        //velocity = velocity * Mathf.Pow(DeltaTime, 2);

        // apply velocity to current position
        point.OldPos = point.Pos;
        point.Pos += (velocity + Gravity) * Mathf.Pow(DeltaTime, 2);

        // force point to stay above ground
        if (point.Pos.y < GroundHeight)
            point.Pos.y = GroundHeight;

        Vertices[i] = point;
    }
}


public struct ApplyConstraints : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Vec3> HorzConstraints;
    [ReadOnly]
    public NativeArray<Vec3> VertConstraints;
    [ReadOnly]
    public NativeArray<VerletVertex> VerticesRO;
    public NativeArray<VerletVertex> Vertices;

    public int Width;
    public int Height;
    public float MaxMovement;

    public void Execute(int i)
    {
        VerletVertex vertex = Vertices[i];
        // to describe this vertex's movement, we build a vector
        // by adding each of its adjacent constraint deltas together
        Vec3 changeVect = Vec3.zero;

        int row = i / (Width + 1);
        int col = i % (Width + 1);

        // if this is a transform, we're not moving it
        if (vertex.Type == VertexType.Transform)
            return;

        // is there a vert to LEFT?
        if (col > 0)
        {
            var leftVert = VerticesRO[i - 1];
            // constraint index
            int conIndex = i - (row + 1);
            Vec3 constraint = HorzConstraints[conIndex];



            if (leftVert.Type != VertexType.Transform)
                changeVect -= constraint / 2;
            else
                changeVect -= constraint;
        }

        // is there a vert to RIGHT?
        if (col < Width)
        {
            var rightVert = VerticesRO[i + 1];
            // constraint index
            int conIndex = i - row;
            Vec3 constraint = HorzConstraints[conIndex];

            if (rightVert.Type != VertexType.Transform)
                changeVect += constraint / 2;
            else
                changeVect += constraint;
        }

        // is there a vert to TOP?
        if (row > 0)
        {
            var topVert = VerticesRO[i - (Width + 1)];
            // constraint index
            int conIndex = i - (Width + 1);
            Vec3 constraint = VertConstraints[conIndex];

            if (topVert.Type != VertexType.Transform)
                changeVect -= constraint / 2;
            else
                changeVect -= constraint;
        }

        // is there a vert to BOTTOM?
        if (row < Height)
        {
            var bottomVert = VerticesRO[i + (Width + 1)];
            // constraint index
            int conIndex = i;
            Vec3 constraint = VertConstraints[conIndex];

            if (bottomVert.Type != VertexType.Transform)
                changeVect += constraint / 2;
            else
                changeVect += constraint;
        }

        vertex.Pos += changeVect;
        Vertices[i] = vertex;
    }
}

// Provides vectors that describe the delta between 
// two vertices and their constrained positions
public struct CalculateConstraints : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<VerletVertex> Vertices;
    [NativeDisableParallelForRestriction]
    public NativeArray<Vec3> HorzConstraints;
    [NativeDisableParallelForRestriction]

    public NativeArray<Vec3> VertConstraints;

    public int Width;
    public int Height;
    public float ConstraintLength;


    public void Execute(int i)
    {
        int row = i / (Width + 1);
        int col = i % (Width + 1);

        // calculate RIGHT constraint
        if (col < Width)
        {
            Vec3 vRightDiff = Vertices[i + 1].Pos - Vertices[i].Pos;
            float rightDiffFactor =
                (vRightDiff.magnitude - ConstraintLength) / vRightDiff.magnitude;

            int index = i - row;


                HorzConstraints[index] = (rightDiffFactor <= 0) ?
    Vec3.zero : rightDiffFactor * vRightDiff;


        }

        // calculate BOTTOM constraint
        if (row < Height)
        {
            Vec3 vBottomDiff = Vertices[i + (Width + 1)].Pos - Vertices[i].Pos;
            float bottomDiffFactor =
                (vBottomDiff.magnitude - ConstraintLength) / vBottomDiff.magnitude;

            int index = i;

            VertConstraints[index] = (bottomDiffFactor <= 0) ?
                Vec3.zero : bottomDiffFactor * vBottomDiff;
        }
    }
}
