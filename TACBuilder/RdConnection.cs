using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Impl;
using JetBrains.Rd.Tasks;
using org.jacodb.api.net.generated.models;
using TACBuilder.ILReflection;
using TACBuilder.Serialization;

namespace TACBuilder;

public class RdConnection(AppTacBuilder builder)
{
    private readonly LifetimeDefinition _lifetimeDef = Lifetime.Eternal.CreateNested();

    public void Connect(int port)
    {
        var lifetime = _lifetimeDef.Lifetime;
        lifetime.OnTermination(() => Console.WriteLine("lifetime terminated"));
        SingleThreadScheduler.RunOnSeparateThread(lifetime, "Client", scheduler =>
            {
                scheduler.Queue(() =>
                {
                    var wire = new SocketWire.Client(lifetime, scheduler, port, "Client");
                    var idKind = IdKind.Client;
                    var serializers = new Serializers();
                    var protocol = new Protocol("client side", serializers, new Identities(idKind), scheduler, wire,
                        lifetime);
                    var ilModel = new IlModel(lifetime, protocol);

                    ilModel.GetIlSigModel().Publication.SetSync((lt, request) =>
                    {
                        foreach (var target in request.RootAsms)
                        {
                            AppTacBuilder.IncludeRootAsm(target);
                            builder.Build(target);
                        }

                        var instances = AppTacBuilder.GetFreshInstances();
                        Console.WriteLine(
                            $".net built {instances.Count} instances with total of {instances.Select(it => (it as IlType).Methods.Count).Sum()}");
                        var asmDepGraph = AppTacBuilder.GetBuiltAssemblies();
                        var serialized = RdSerializer.Serialize(instances);

                        return new PublicationResponse(
                            asmDepGraph.Select(asm => new IlAsmDto(asm.Name, asm.Location)).ToList(),
                            asmDepGraph.Select(asm =>
                                asm.ReferencedAssemblies.Select(referenced =>
                                        new IlAsmDto(referenced.Name, referenced.Location))
                                    .ToList()).ToList(), serialized);
                    });

                    ilModel.GetIlSigModel().GenericSubstitutions.SetSync(
                        (lt, request) =>
                        {
                            Console.Error.WriteLine(".net built generic substitutions");
                            return request.Select(builder.GetType).Select(v => v switch
                            {
                                null => null,
                                _ => RdSerializer.Serialize([v]).Single()
                            }).ToList();
                        }
                    );
                    ilModel.GetIlSigModel().Close.Advise(lifetime, () =>
                    {
                        Console.WriteLine(".net start terminating");
                        _lifetimeDef.Terminate();
                        Environment.Exit(0);
                    });
                });
            }
        );
    }

    public async void SpinAndTerminate()
    {
        await Task.Delay(100);
        _lifetimeDef.Terminate();
    }
}