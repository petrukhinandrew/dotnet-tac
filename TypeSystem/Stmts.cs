namespace Usvm.IL.TypeSystem;
interface ILStmt { }
class ILStmtLocation;
interface ILAssignStmt : ILStmt { }
interface ILCallStmt : ILStmt { }

// return, leave, endfinally
interface ILLeaveScopeStmt : ILStmt { }

interface ILBranchStmt : ILStmt { }

class ILGotoStmt : ILBranchStmt { }

class ILIfStmt : ILBranchStmt { }
