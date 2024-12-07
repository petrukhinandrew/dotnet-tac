using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Impl;
using JetBrains.Rd.Tasks;
using org.jacodb.api.net.generated.models;

namespace TACBuilder;

public class RdConnection
{
    private LifetimeDefinition _lifetimeDef = Lifetime.Eternal.CreateNested();
    private readonly Func<PublicationRequest, PublicationResponse> _asmReqCallback;

    public RdConnection(Func<PublicationRequest, PublicationResponse> asmReqCallback)
    {
        _asmReqCallback = asmReqCallback;
        SpinAndTerminate();
    }

    public void Connect(int port)
    {
        var lifetime = _lifetimeDef.Lifetime;
        SingleThreadScheduler.RunOnSeparateThread(lifetime, "Client", scheduler =>
            {
                var wire = new SocketWire.Client(lifetime, scheduler, port, "Client");
                var idKind = IdKind.Client;
                var serializers = new Serializers();
                var protocol = new Protocol("client side", serializers, new Identities(idKind), scheduler, wire,
                    lifetime);
                scheduler.Queue(() =>
                {
                    var ilModel = new IlModel(lifetime, protocol);
                    protocol.Scheduler.Queue(() =>
                    {
                        ilModel.GetIlSigModel().Publication.SetSync((lt, request) =>
                            _asmReqCallback(request)
                        );
                    });
                });
            }
        );
    }

    public async void SpinAndTerminate()
    {
        await Task.Delay(10_000);
        _lifetimeDef.Terminate();
    }
}