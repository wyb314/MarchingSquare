using Drawing;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Unity.VisualScripting;
using static UnityEditor.PlayerSettings;

public class MarchingSquare : MonoBehaviour
{
    public class Line
    {
        public Vector3 start;
        public Vector3 end;

        public Line(Vector3 start, Vector3 end)
        {
            this.start = start;
            this.end = end;
        }
    }
    public class CellInfo 
    {
        public int x, y;
        public List<Line> lines = new List<Line>();
        public CellInfo(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public void ClearLines()
        {
            lines.Clear();
        }

        public void AddLine(params Line[] _lines) 
        {
            lines.AddRange(_lines);
        }
    }

    public int randomSeed;
    public int gridNumX = 10;
    public int gridNumY = 10;
    public int gridSize = 20;
    public float radius = 24;
    private byte[,] bits;
    private Rect mBoundary;

    private CellInfo[,] mCellInfos;

    [SerializeField] bool showGridInfo = false;
    [SerializeField] bool showBitInfo = false;
    [SerializeField] bool showBoundary = false;
    [SerializeField] Color mBoundaryColor = Color.yellow;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(randomSeed);
        ReGenerateBits();
    }

    [ShowIf("@UnityEngine.Application.isPlaying")]
    [Button]
    void ReGenerateBits()
    {
        bits = new byte[gridNumX + 1, gridNumY + 1];
        for (int i = 0; i < bits.GetLength(0); i++)
        {
            for (int j = 0; j < bits.GetLength(1); j++)
            {
                var v = UnityEngine.Random.Range(0, byte.MaxValue);
                bits[i, j] =(byte)(v >= 128 ? 1 : 0);
            }
        }

        mCellInfos = new CellInfo[gridNumX, gridNumY];
        for (int i = 0; i < gridNumX; i++)
        {
            for (int j = 0; j < gridNumY; j++)
            {
                mCellInfos[i, j] = new CellInfo(i,j);
                UpdateCellInfo(mCellInfos[i, j]);
            }
        }

        var center = new Vector2(gridSize * gridNumX * 0.5f, gridSize * gridNumY * 0.5f);
        var size = new Vector2(gridSize * gridNumX, gridSize * gridNumY) + new Vector2(radius, radius) * 2;
        var pos = center - size * 0.5f;
        mBoundary = new Rect(pos, size);
    }

    // Update is called once per frame
    void Update()
    {
        var ingame = Draw.ingame;

        if (showBoundary)
        {
            var v0 = mBoundary.min;
            var v2 = mBoundary.max;
            var v1 = new Vector2(v2.x,v0.y);
            var v3 = new Vector2(v0.x, v2.y);
            ingame.Line(v0,v1, mBoundaryColor);
            ingame.Line(v1, v2, mBoundaryColor);
            ingame.Line(v2, v3, mBoundaryColor);
            ingame.Line(v3, v0, mBoundaryColor);
        }

        for (int i = 0; i <= gridNumX; i++)
        {
            var startX = i * gridSize;
            var startY = 0;
            var endX = startX;
            var endY = gridNumY * gridSize;
            ingame.Line(new Vector3(startX, startY), new Vector3(endX, endY));
        }

        for (int i = 0; i <= gridNumY; i++)
        {
            var startX = 0;
            var startY = i * gridSize;
            var endX = gridNumX * gridSize;
            var endY = startY;
            ingame.Line(new Vector3(startX, startY), new Vector3(endX, endY));
        }

        for (int i = 0; i < bits.GetLength(0); i++)
        {
            for (int j = 0; j < bits.GetLength(1); j++)
            {
                var bitVal = bits[i, j];
                var pos = new Vector3(i * gridSize, j * gridSize);
                var color = bitVal == 1 ? Color.white : Color.black;
                if (showBitInfo)
                {
                    ingame.Label2D(position: pos, text: $"({i},{j})", sizeInPixels: 10, alignment: LabelAlignment.Center);
                }
                ingame.SphereOutline(pos, radius, color);
            }
        }

        for (int i = 0; i < gridNumX; i++)
        {
            for (int j = 0; j < gridNumY; j++)
            {
                DrawCellInfo(mCellInfos[i,j]);
            }
        }

        ProcessInput();
    }

    bool InCicle(in Vector2 pos , in Vector2 cicle , float rdius)
    {
        return Vector2.SqrMagnitude(pos-cicle)< radius * radius;
    }
    void ProcessInput() 
    {
        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Input.mousePosition;
            var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0;
            var worldPos2d = new Vector2(worldPos.x,worldPos.y);
            if (!mBoundary.Contains(worldPos2d))
            {
                return;
            }

            var tx = Mathf.FloorToInt(worldPos.x + gridSize * 0.5f) / gridSize;
            var ty = Mathf.FloorToInt(worldPos.y + gridSize * 0.5f) / gridSize;

            var vPos = new Vector2(tx * gridSize, ty * gridSize);
            if (InCicle(in vPos, worldPos, radius))
            {
                bits[tx, ty] = (byte)(bits[tx, ty] == 1 ? 0 : 1);
                TryUpdateCellInfo(tx -1,ty -1);
                TryUpdateCellInfo(tx, ty - 1);
                TryUpdateCellInfo(tx, ty);
                TryUpdateCellInfo(tx-1, ty);
            }
            
        }
    }

    int GetState(in Vector2Int topLeft, in Vector2Int topRight, in Vector2Int bottomLeft, in Vector2Int bottomRight)
    {
        var bit0 = bits[bottomLeft.x, bottomLeft.y];
        var bit1 = bits[bottomRight.x, bottomRight.y];
        var bit2 = bits[topRight.x, topRight.y];
        var bit3 = bits[topLeft.x, topLeft.y];

        return (bit0 << 0) | (bit1 << 1) | (bit2 << 2) | (bit3 << 3);
    }

    [SerializeField] Color lineColor = Color.green;
    void DrawSquare(Vector3 origin,int state)
    {
        var ingame = Draw.ingame;
        switch (state) 
        {
            case 1 :
            case 14:
                ingame.Line(origin + new Vector3(gridSize * 0.5f,0), origin + new Vector3(0,gridSize * 0.5f),lineColor);
                break;
            case 2:
            case 13:
                ingame.Line(origin + new Vector3(gridSize * 0.5f, 0), origin + new Vector3(gridSize, gridSize * 0.5f), lineColor);
                break;
            case 3:
            case 12:
                ingame.Line(origin + new Vector3(0,gridSize * 0.5f), origin + new Vector3(gridSize, gridSize * 0.5f), lineColor);
                break;
            case 4:
            case 11:
                ingame.Line(origin + new Vector3(gridSize, gridSize * 0.5f), origin + new Vector3(gridSize * 0.5f, gridSize), lineColor);
                break;
            case 5:
                ingame.Line(origin + new Vector3(gridSize, gridSize * 0.5f), origin + new Vector3(gridSize * 0.5f, gridSize), lineColor);
                ingame.Line(origin + new Vector3(gridSize * 0.5f, 0), origin + new Vector3(0, gridSize * 0.5f), lineColor);
                break;
            case 6:
            case 9:
                ingame.Line(origin + new Vector3(gridSize * 0.5f,0), origin + new Vector3(gridSize * 0.5f, gridSize), lineColor);
                break;
            case 7:
            case 8:
                ingame.Line(origin + new Vector3(0,gridSize * 0.5f), origin + new Vector3(gridSize * 0.5f, gridSize), lineColor);
                break;
            case 10:
                ingame.Line(origin + new Vector3(gridSize * 0.5f, 0), origin + new Vector3(gridSize, gridSize * 0.5f), lineColor);
                ingame.Line(origin + new Vector3(0, gridSize * 0.5f), origin + new Vector3(gridSize * 0.5f, gridSize), lineColor);
                break;
        }
    }

    void DrawCellInfo(CellInfo cellInfo)
    {
        var ingame = Draw.ingame;
        foreach (var line in cellInfo.lines)
        {
            ingame.Line(line.start, line.end, lineColor);
        }

        var pos = new Vector3(gridSize * cellInfo.x + gridSize * 0.5f,
            gridSize * cellInfo.y + gridSize * 0.5f);
        if (showGridInfo)
        {
            ingame.Label2D(position: pos, text: $"({cellInfo.x},{cellInfo.y})", sizeInPixels: 10, alignment: LabelAlignment.Center);
        }
    }

    void TryUpdateCellInfo(int x, int y)
    {
        if (x >= 0 && x < gridNumX
            && y >= 0 && y < gridNumY)
        {
            UpdateCellInfo(mCellInfos[x,y]);
        }
    }

    void UpdateCellInfo(CellInfo cellInfo)
    {
        cellInfo.ClearLines();
        var i = cellInfo.x;
        var j = cellInfo.y;
        Vector2Int topLeft = new Vector2Int(i, j + 1);
        Vector2Int topRight = new Vector2Int(i + 1, j + 1);
        Vector2Int bottomLeft = new Vector2Int(i, j);
        Vector2Int bottomRight = new Vector2Int(i + 1, j);
        var state = GetState(in topLeft, in topRight, in bottomLeft, in bottomRight);
        var origin = new Vector3(i * gridSize, j * gridSize);
        switch (state)
        {
            case 1:
            case 14:
                cellInfo.AddLine(new Line(origin + new Vector3(gridSize * 0.5f, 0), origin + new Vector3(0, gridSize * 0.5f)));
                break;
            case 2:
            case 13:
                cellInfo.AddLine(new Line(origin + new Vector3(gridSize * 0.5f, 0), origin + new Vector3(gridSize, gridSize * 0.5f)));
                break;
            case 3:
            case 12:
                cellInfo.AddLine(new Line(origin + new Vector3(0, gridSize * 0.5f), origin + new Vector3(gridSize, gridSize * 0.5f)));
                break;
            case 4:
            case 11:
                cellInfo.AddLine(new Line(origin + new Vector3(gridSize, gridSize * 0.5f), origin + new Vector3(gridSize * 0.5f, gridSize)));
                break;
            case 5:
                cellInfo.AddLine(new Line(origin + new Vector3(gridSize, gridSize * 0.5f), origin + new Vector3(gridSize * 0.5f, gridSize)));
                cellInfo.AddLine(new Line(origin + new Vector3(gridSize * 0.5f, 0), origin + new Vector3(0, gridSize * 0.5f)));
                break;
            case 6:
            case 9:
                cellInfo.AddLine(new Line(origin + new Vector3(gridSize * 0.5f, 0), origin + new Vector3(gridSize * 0.5f, gridSize)));
                break;
            case 7:
            case 8:
                cellInfo.AddLine(new Line(origin + new Vector3(0, gridSize * 0.5f), origin + new Vector3(gridSize * 0.5f, gridSize)));
                break;
            case 10:
                cellInfo.AddLine(new Line(origin + new Vector3(gridSize * 0.5f, 0), origin + new Vector3(gridSize, gridSize * 0.5f)));
                cellInfo.AddLine(new Line(origin + new Vector3(0, gridSize * 0.5f), origin + new Vector3(gridSize * 0.5f, gridSize)));
                break;
        }
    }
}
