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
    [SerializeField] private int difficultyLevel = 0; // default 3x3
    [SerializeField] private TMPro.TMP_Text difficultyLabel;
    [SerializeField] private int shuffleMoves = 10;
    private bool isSolving = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BuildGoal();
        BuildBoard(); // sets up the tiles in a grid
        NewGame();

        int startSize = BoardSizeForDifficulty(difficultyLevel);
        SetSize(startSize); // ensure board is set to initial difficulty size
        if (difficultyLabel != null)
        {
            difficultyLabel.text = DifficultyName(difficultyLevel);
        }
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
        if (tiles.Count != numTiles * numTiles)
            BuildBoard();

        state = (int[])goal.Clone();

        shuffleMoves = GetBfsEasyShuffle(numTiles);
        state = ShuffleByRandomMoves(state, numTiles, shuffleMoves);
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
            if (moves.Count > 1) moves.RemoveAll(st => st.b == lastBlank);
            var pick = moves[rand.Next(moves.Count)];
            lastBlank = pick.a;
            s = Apply(s, pick);
        }
        return s;
    }

    // choose tiny shuffle depths so BFS is safe
    int GetBfsEasyShuffle(int n)
    {
        if (n == 3) return 12;
        if (n == 4) return 16;   
        if (n == 5) return 14;  
        if (n == 6) return 13;   
        return 15;
    }

    // slider helper
    int BoardSizeForDifficulty(int d)
    {
        if (d == 0) return 3;   // Easy
        if (d == 1) return 4;   // Medium
        if (d == 2) return 5;   // Hard
        return 6;               // Extreme
    }

    string DifficultyName(int d)
    {
        if (d == 0) return "Difficulty Level: Easy (3×3)";
        if (d == 1) return "Difficulty Level: Medium (4×4)";
        if (d == 2) return "Difficulty Level: Hard (5×5)";
        return "Difficulty Level: Extreme (6×6)";
    }

    public void SetSize(int n)
    {
        n = Mathf.Clamp(n, 3, 6);
        if (n == numTiles) return;

        numTiles = n;
        BuildGoal();
        BuildBoard();
        NewGame();
    }

    public void OnDifficultySliderChanged(float v)
    {
        difficultyLevel = Mathf.Clamp((int)v, 0, 3);

        int newSize = BoardSizeForDifficulty(difficultyLevel);

        // Rebuild only if size actually changed
        if (newSize != numTiles)
        {
            SetSize(newSize);  // See method below
        }

        if (difficultyLabel != null)
        {
            difficultyLabel.text = DifficultyName(difficultyLevel);
        }

        // Debug to confirm it’s firing:
        Debug.Log($"Slider changed → level={difficultyLevel}, target size={newSize}");
    }


    // AI solve
    public void Solve()
    {
        if (isSolving) return;

        // compute path first
        var startCopy = (int[])state.Clone();
        List<Swap> path = null;

        if (numTiles >= 3)
        {
            // BFS for 3×3
            path = BFS(startCopy, goal, numTiles);
        }
        else
        {
            // DFS for 4×4..6×6
            int depthCap = Mathf.Max(shuffleMoves + 10, 100); // safety margin
            path = IDDFS(startCopy, goal, numTiles, depthCap);
        }

        if (path == null)
        {
            Debug.LogWarning("No path found.");
            return;
        }

        if (path.Count == 0)
        {
            Debug.Log("Already solved.");
            return;
        }

        StartCoroutine(PathAnimation(path, 0.2f)); // adjust delay to taste
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

    List<Swap> IDDFS(int[] start, int[] goal, int n, int maxDepth, int maxNodes = 500000)
    {
        for (int limit = 0; limit <= maxDepth; limit++)
        {
            var result = DFS(start, goal, n, limit, maxNodes);
            if (result != null) return result;
        }
        return null;
    }

    List<Swap> DFS(int[] start, int[] goal, int n, int depthCap, int maxNodes = 500000)
    {
        string Key(int[] s) => string.Join(",", s);

        var stack = new Stack<(int[] state, int depth)>();
        //var visited = new HashSet<string>();
        var bestDepth = new Dictionary<string, int>();
        var parent = new Dictionary<string, (string pk, Swap move)>();
        
        string sKey = Key(start), gKey = Key(goal);
        if (sKey == gKey) return new List<Swap>();
        
        stack.Push((start, 0));
        //visited.Add(sKey);
        bestDepth[sKey] = 0;
        parent[sKey] = (null, new Swap(-1, -1));
        int nodes = 0;
        
        while (stack.Count > 0 && nodes < maxNodes)
        {
            nodes++;
            var (s, depth) = stack.Pop();
            var sK = Key(s);

            if (depth >= depthCap) continue;
            
            var moves = MovableTiles(s, n);
            foreach (var m in moves)
            {
                var t = Apply(s, m);
                var tK = Key(t);

                int nextDepth = depth + 1;

                if (bestDepth.TryGetValue(tK, out int seenDepth) && seenDepth <= nextDepth)
                {
                    continue;
                }

                //if (visited.Contains(tK)) continue;
                //visited.Add(tK);

                bestDepth[tK] = nextDepth;
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
                stack.Push((t, depth + 1));
            }
        }
        return null; // not found
    }
    IEnumerator PathAnimation(List<Swap> path, float delay = 0.2f)
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