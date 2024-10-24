using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AA
{
    public int a;
}

public class TestRefVal : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AA aa;
        aa.a = 100;
        int k = aa.a;
        modi(ref k);

        Debug.LogError($"k : {k} , a: {aa.a}");

    }

    void modi(ref int a)
    {
        a = 10000;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
