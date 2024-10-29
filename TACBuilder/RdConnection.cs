using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Impl;
using org.jacodb.api.net.generated.models;

namespace TACBuilder;

public class RdConnection(Func<Request, List<IlDto>> asmReqCallback)
{
    private LifetimeDefinition _lifetimeDef = Lifetime.Eternal.CreateNested();
    private IScheduler _scheduler;
    private Func<Request, List<IlDto>> _asmReqCallback = asmReqCallback;

    public void Connect(int port)
    {
        var _lifetime = _lifetimeDef.Lifetime;
        _scheduler = SingleThreadScheduler.RunOnSeparateThread(_lifetime, "Client", scheduler =>
        {
            var wire = new SocketWire.Client(_lifetime, scheduler, port, "Client");
            var idKind = IdKind.Client;
            var serializers = new Serializers();
            var protocol = new Protocol("client side", serializers, new Identities(idKind), scheduler, wire,
                _lifetime);
            scheduler.Queue(() =>
                {
                    var ilModel = new IlModel(_lifetime, protocol);
                    var asmReq = ilModel.GetIlSigModel().AsmRequest;
                    var asmResp = ilModel.GetIlSigModel().AsmResponse;
                    asmReq.Advise(_lifetime, req =>
                    {
                        var response = _asmReqCallback(req);
                        asmResp.Fire(response);
                    });
                }
            );
        });
    }
}
