using System.Diagnostics;
using TACBuilder.Exprs;

namespace TACBuilder.BodyBuilder;

public class LinkedListTacBody(IEnumerable<IlStmt> stmts)
{
    public Node Head = Transform(stmts);

    public record struct Node(IlStmt stmt, int index)
    {
        public IlStmt Stmt { get; } = stmt;
        public int Index => index;
        public HashSet<Node> Successors { get; set; } = [];
    }

    private static Node Transform(IEnumerable<IlStmt> stmtsRaw)
    {
        var stmts = stmtsRaw.ToList();
        var visited = new bool[stmts.Count];

        Debug.Assert(stmts.Count > 0);
        var q = new Queue<Node>();
        Node head = new Node(stmts.First(), 0);
        q.Enqueue(head);
        while (q.Count > 0)
        {
            var stmt = q.Dequeue();
            if (visited[stmt.Index]) continue;
            visited[stmt.Index] = true;
            List<Node> fresh = stmt.Stmt switch
            {
                IlReturnStmt or IlThrowStmt or IlRethrowStmt or IlEndFinallyStmt => [],
                IlBranchStmt branch => branch switch
                {
                    IlGotoStmt or IlLeaveStmt => [new Node(stmts[branch.Target], branch.Target)],
                    IlIfStmt =>
                    [
                        new Node(stmts[stmt.Index + 1], stmt.Index + 1), new Node(stmts[branch.Target], branch.Target)
                    ]
                },
                _ => stmt.Index + 1 == stmts.Count ? [] : [new Node(stmts[stmt.Index + 1], stmt.Index + 1)]
            };
            foreach (var f in fresh.Except(stmt.Successors))
            {
                q.Enqueue(f);
                stmt.Successors.Add(f);
            }
        }

        return head;
    }
}