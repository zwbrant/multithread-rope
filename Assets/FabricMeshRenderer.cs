using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Fabric))]
public class FabricMeshRenderer : MonoBehaviour
{
    public MeshFilter MeshFilter;
    public Mesh Mesh;

    private Fabric _fabric;
    protected VerletVertex[] _vertices;
    protected int[] _triangles;

    // Start is called before the first frame update
    void Start()
    {
        if (MeshFilter == null)
            MeshFilter = GetComponent<MeshFilter>();

        Mesh = new Mesh();
        MeshFilter.mesh = Mesh;

        StartCoroutine(DelayStart());

        _vertices = _fabric._nVertices.ToArray();

    }

    private IEnumerator DelayStart()
    {
        yield return new WaitForSeconds(.2f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void BuildMesh()
    {
        for (int i = 0; i < _vertices.Length; i++)
        {

        }


    }
}
