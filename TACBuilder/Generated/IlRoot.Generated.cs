//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a RdGen v1.11.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Serialization;
using JetBrains.Rd;
using JetBrains.Rd.Base;
using JetBrains.Rd.Impl;
using JetBrains.Rd.Tasks;
using JetBrains.Rd.Util;
using JetBrains.Rd.Text;


// ReSharper disable RedundantEmptyObjectCreationArgumentList
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantOverflowCheckingContext


namespace org.jacodb.api.net.generated.models
{
  
  
  /// <summary>
  /// <p>Generated from: IlRoot.kt:21</p>
  /// </summary>
  public class IlRoot : RdExtBase
  {
    //fields
    //public fields
    
    //private fields
    //primary constructor
    private IlRoot(
    )
    {
    }
    //secondary constructor
    //deconstruct trait
    //statics
    
    
    
    protected override long SerializationHash => 3111946874287143L;
    
    protected override Action<ISerializers> Register => RegisterDeclaredTypesSerializers;
    public static void RegisterDeclaredTypesSerializers(ISerializers serializers)
    {
      
      serializers.RegisterToplevelOnce(typeof(IlRoot), IlRoot.RegisterDeclaredTypesSerializers);
      serializers.RegisterToplevelOnce(typeof(IlMethodBodyModel), IlMethodBodyModel.RegisterDeclaredTypesSerializers);
      serializers.RegisterToplevelOnce(typeof(IlModel), IlModel.RegisterDeclaredTypesSerializers);
      serializers.RegisterToplevelOnce(typeof(IlSigModel), IlSigModel.RegisterDeclaredTypesSerializers);
    }
    
    public IlRoot(Lifetime lifetime, IProtocol protocol) : this()
    {
      Identify(protocol.Identities, RdId.Root.Mix("IlRoot"));
      Bind(lifetime, protocol, "IlRoot");
    }
    
    //constants
    
    //custom body
    //methods
    //equals trait
    //hash code trait
    //pretty print
    public override void Print(PrettyPrinter printer)
    {
      printer.Println("IlRoot (");
      printer.Print(")");
    }
    //toString
    public override string ToString()
    {
      var printer = new SingleLinePrettyPrinter();
      Print(printer);
      return printer.ToString();
    }
  }
}
