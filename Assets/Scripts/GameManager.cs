using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("GameBoard")]
    public int numTiles = 3;
    public RectTransform boardParent;
    public GridLayoutGroup gridLayout;
    public Tile tilePrefab;

    private List<Tile> tiles = new List<Tile>();
    private int[] state;
    private int[] goal;

    [Header("AI Solve")]
    private bool isSolving = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BuildGoal();
        BuildBoard(); // sets up the tiles in a grid
        NewGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void BuildGoal()
    {
        goal = new int[numTiles * numTiles];
        for (int i = 0; i < numTiles*numTiles; i++)
        {
            goal[i] = i + 1;
        }
        goal[numTiles*numTiles - 1] = 0; // last is blank
    }

    void BuildBoard()
    {
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);
        tiles.Clear();

        var size = Mathf.Min(boardParent.rect.width, boardParent.rect.height);
        var cell = Mathf.FloorToInt(size / numTiles);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = numTiles;
        gridLayout.cellSize = new Vector2(cell, cell);
        gridLayout.spacing = new Vector2(8, 8);
        gridLayout.padding.left = gridLayout.padding.right = gridLayout.padding.top = gridLayout.padding.bottom = 8;

        for (int i = 0; i < numTiles * numTiles; i++)
            tiles.Add(Instantiate(tilePrefab, boardParent));
    }

    public void NewGame() //make sure it's solvable
    {
        //if (tiles.Count != numTiles * numTiles)
        //    BuildBoard();

        state = (int[])goal.Clone();
        state = ShuffleByRandomMoves(state, numTiles, 50);
        ApplyToUI();
    }

    void ApplyToUI()
    {
        for (int i = 0; i < state.Length; i++)
            tiles[i].Init(state[i], OnTileClicked);
        if (IsGoal()) Debug.Log("Solved!");
    }

    void OnTileClicked(Tile tile)
    {
        int indx = tiles.IndexOf(tile);
        int blank = System.Array.IndexOf(state, 0);

        int br = blank / numTiles, bc = blank % numTiles, r = indx / numTiles, c = indx % numTiles;
        if (Mathf.Abs(br - r) + Mathf.Abs(bc - c) == 1) // Manhattan distance formula for adjacent
        {
            // swap tile with blank
            (state[blank], state[indx]) = (state[indx], state[blank]);
            ApplyToUI();
        }
    }

    bool IsGoal() // win check
    {
        for (int i = 0; i < state.Length; i++)
        {
            if (state[i] != goal[i]) return false;
        }
        return true;
    }

    // helpers
    struct Swap // swap a and b
    { 
        public int a, b; 
        public Swap(int a, int b) 
        { 
            this.a = a; 
            this.b = b; 
        } 
    } 

    List<Swap> MovableTiles(int[] s, int n)
    {
        var list = new List<Swap>();
        int blank = System.Array.IndexOf(s, 0);
        int r = blank / n, c = blank % n;
        if (r > 0) list.Add(new Swap(blank, blank - n));        // up
        if (r < n - 1) list.Add(new Swap(blank, blank + n));    // down
        if (c > 0) list.Add(new Swap(blank, blank - 1));        // left
        if (c < n - 1) list.Add(new Swap(blank, blank + 1));    // right

        return list;
    }

    int[] Apply(int[] s, Swap m)
    {
        var t = (int[])s.Clone();
        (t[m.a], t[m.b]) = (t[m.b], t[m.a]);
        return t;
    }

    int[] ShuffleByRandomMoves(int[] s0, int n, int k)
    {
        var s = (int[])s0.Clone();
        var rand = new System.Random();
        int lastBlank = System.Array.IndexOf(s, 0);

        for (int i = 0; i < k; i++)
        {
            var moves = MovableTiles(s, n);
            if (moves.Count > 0) moves.RemoveAll(st => st.b == lastBlank);
            var pick = moves[rand.Next(moves.Count)];
            lastBlank = pick.a;
            s = Apply(s, pick);
        }
        return s;
    }

    // AI solve
    public void SolveBFS()
    {
        if (isSolving) return;

        // compute path first
        var startCopy = (int[])state.Clone();
        var path = BFS(startCopy, goal, numTiles);

        if (path == null)
        {
            Debug.LogWarning("BFS: no path found.");
            return;
        }
        if (path.Count == 0)
        {
            Debug.Log("Already solved.");
            return;
        }

        StartCoroutine(BFSAnimation(path, 0.2f)); // adjust delay to taste
    }

    List<Swap> BFS(int[] start, int[] goal, int n, int maxNodes = 500000)
    {
        string Key(int[] s) => string.Join(",", s);

        var q = new Queue<int[]>();
        var visited = new HashSet<string>();
        var parent = new Dictionary<string, (string pk, Swap move)>();

        string sKey = Key(start), gKey = Key(goal);
        if (sKey == gKey) return new List<Swap>();

        q.Enqueue(start);
        visited.Add(sKey);
        parent[sKey] = (null, new Swap(-1, -1));

        int nodes = 0;
        while (q.Count > 0 && nodes < maxNodes)
        {
            nodes++;
            var s = q.Dequeue();
            var sK = Key(s);
            var moves = MovableTiles(s, n);
            foreach (var m in moves)
            {
                var t = Apply(s, m);
                var tK = Key(t);
                if (visited.Contains(tK)) continue;
                visited.Add(tK);
                parent[tK] = (sK, m);
                if (tK == gKey)
                {
                    // found the goal, backtrack to get the path
                    var path = new List<Swap>();
                    var cur = tK;
                    while (parent[cur].pk != null)
                    {
                        var (pk, move) = parent[cur];
                        path.Add(move);
                        cur = pk;
                    }
                    path.Reverse();
                    return path;
                }
                q.Enqueue(t);
            }
        }

        return null; // not found
    }

    IEnumerator BFSAnimation(List<Swap> path, float delay = 0.2f)
    {
        if (path == null || path.Count == 0) yield break;
        isSolving = true;

        foreach (var tile in tiles)
            tile.SetIneractable(false);

        foreach (var move in path)
        {
            state = Apply(state, move);
            ApplyToUI();
            yield return new WaitForSeconds(delay);
        }

        foreach (var tile in tiles)
            tile.SetIneractable(true);

        isSolving = false;
    }
}