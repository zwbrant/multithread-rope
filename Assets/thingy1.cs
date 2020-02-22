using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using static Unity.Mathematics.math;

public class thingy1 : MonoBehaviour
{
    public List<GameObject> Cubes;

    NativeArray<float> CubeHeights;
    NativeList<float> salmon;

    JobHandle longHandle;

    private void Awake()
    {
        
        //print("Awake1");
        CubeHeights = new NativeArray<float>(5, Allocator.Persistent);
        //Debug.Log("Before: " + System.DateTime.Now.Second);

        //var longJob = new LongJob();
        //longHandle = longJob.Schedule(14, 1);


        
    }

    // Start is called before the first frame update
    void Start()
    {
        print("Start1");

    }

    JobHandle cubeJHandle;
    // Update is called once per frame
    void Update()
    {
        var cubeJob = new CubeJob() { heights = CubeHeights, time = Time.time };
        cubeJHandle = cubeJob.Schedule(5, 1);



        //print(sin(3.14159265359f / 2));
    }

    private void LateUpdate()
    {
        cubeJHandle.Complete();
        UpdateCubePositions();
        //Debug.Log("After: " + System.DateTime.Now.Second);

    }

    private void UpdateCubePositions()
    {
        for (int i = 0; i < Cubes.Count; i++)
        {
            Cubes[i].transform.position = new Vector3(Cubes[i].transform.position.x, CubeHeights[i]);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct CubeJob : IJobParallelFor
    {
        public float time;
        public NativeArray<float> heights;

        public void Execute(int index)
        {
            heights[index] = sin(time + index);
        }
    }

    public struct LongJob : IJobParallelFor
    {
        public void Execute(int i)
        {

                int count = 0;
                long a = 2;
                while (count < 400000)
                {
                    long b = 2;
                    int prime = 1;// to check if found a prime
                    while (b * b <= a)
                    {
                        if (a % b == 0)
                        {
                            prime = 0;
                            break;
                        }
                        b++;
                    }
                    if (prime > 0)
                    {
                        count++;
                    }
                    a++;
                }
                
            
        }
    }
}

