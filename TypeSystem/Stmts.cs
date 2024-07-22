namespace Usvm.IL.TypeSystem;
interface ILStmt { }

interface ILAssignStmt : ILStmt { }
interface ILCallStmt : ILStmt { }

// return, leave, endfinally
interface ILLeaveScopeStmt : ILStmt { }

interface ILBranchStmt : ILStmt { }

class ILGotoStmt : ILBranchStmt { }
class ILTargetLocation;
// class ILCondGotoStmt ?
class ILIfStmt : ILBranchStmt { }
