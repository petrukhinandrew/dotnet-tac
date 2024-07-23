namespace Usvm.IL.TypeSystem;
interface ILAssignStmt : ILStmt { }
interface ILCallStmt : ILStmt { }

// return, leave, endfinally
interface ILLeaveScopeStmt : ILStmt { }

interface ILBranchStmt : ILStmt { }

class ILGotoStmt : ILBranchStmt
{
    public ILStmtLocation Location => throw new NotImplementedException();
}

class ILIfStmt : ILBranchStmt
{
    public ILStmtLocation Location => throw new NotImplementedException();
}
