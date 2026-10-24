using System;using System.Linq;using System.Collections.Generic;using Mono.Cecil;using Mono.Cecil.Cil;using System.IO;
class Transplant {
 static ModuleDefinition target;
 static TypeReference Type(TypeReference t){if(t is ByReferenceType b)return new ByReferenceType(Type(b.ElementType));if(t is ArrayType a)return new ArrayType(Type(a.ElementType),a.Rank);if(t is GenericInstanceType g){var x=new GenericInstanceType(Type(g.ElementType));foreach(var arg in g.GenericArguments)x.GenericArguments.Add(Type(arg));return x;}var own=target.GetType(t.FullName);return own??target.ImportReference(t);}
 static MethodReference Method(MethodReference m){if(m is GenericInstanceMethod g){var x=new GenericInstanceMethod(Method(g.ElementMethod));foreach(var arg in g.GenericArguments)x.GenericArguments.Add(Type(arg));return x;}var type=target.GetType(m.DeclaringType.FullName);if(type!=null){var possibilities=type.Methods.Where(x=>x.Name==m.Name&&x.Parameters.Count==m.Parameters.Count&&x.GenericParameters.Count==m.GenericParameters.Count).ToArray();var found=possibilities.SingleOrDefault(x=>x.Parameters.Select(p=>p.ParameterType.FullName).SequenceEqual(m.Parameters.Select(p=>p.ParameterType.FullName)));if(found==null)throw new Exception("Missing fixture method "+m.FullName);return found;}return target.ImportReference(m);}
 static FieldReference Field(FieldReference f){var type=target.GetType(f.DeclaringType.FullName);if(type!=null){var found=type.Fields.SingleOrDefault(x=>x.Name==f.Name);if(found==null||found.FieldType.FullName!=f.FieldType.FullName)throw new Exception("Missing fixture field "+f.FullName+" actual="+found?.FieldType.FullName);return found;}return target.ImportReference(f);}
 static void Copy(MethodDefinition from,MethodDefinition to){var b=new MethodBody(to){InitLocals=from.Body.InitLocals,MaxStackSize=from.Body.MaxStackSize};to.Body=b;foreach(var v in from.Body.Variables)b.Variables.Add(new VariableDefinition(Type(v.VariableType)));var map=new Dictionary<Instruction,Instruction>();foreach(var i in from.Body.Instructions){var n=Instruction.Create(OpCodes.Nop);n.OpCode=i.OpCode;b.Instructions.Add(n);map[i]=n;}foreach(var i in from.Body.Instructions){object o=i.Operand;if(o is Instruction j)o=map[j];else if(o is Instruction[] js)o=js.Select(x=>map[x]).ToArray();else if(o is TypeReference t)o=Type(t);else if(o is MethodReference m)o=Method(m);else if(o is FieldReference f)o=Field(f);else if(o is VariableDefinition v)o=b.Variables[v.Index];else if(o is ParameterDefinition p)o=to.Parameters[p.Index];map[i].Operand=o;}foreach(var h in from.Body.ExceptionHandlers)b.ExceptionHandlers.Add(new ExceptionHandler(h.HandlerType){TryStart=map[h.TryStart],TryEnd=h.TryEnd==null?null:map[h.TryEnd],HandlerStart=map[h.HandlerStart],HandlerEnd=h.HandlerEnd==null?null:map[h.HandlerEnd],FilterStart=h.FilterStart==null?null:map[h.FilterStart],CatchType=h.CatchType==null?null:Type(h.CatchType)});if(!from.Body.Instructions.Select(x=>x.OpCode.Value).SequenceEqual(b.Instructions.Select(x=>x.OpCode.Value)))throw new Exception("Opcode mismatch");Console.WriteLine("TRANSPLANTED exact opcode/branch sequence "+from.FullName);}
 static void Main(string[] args){using(var native=AssemblyDefinition.ReadAssembly(args[0])){
 if(args[1]=="--enums"){
  foreach(string name in new[]{"InControl.InputControlType","GlobalEnums.GamepadType","GlobalEnums.GamepadState","GlobalEnums.HeroActionButton","InControl.Key"}){
   var type=native.MainModule.GetType(name);Console.WriteLine("namespace "+type.Namespace+" {public enum "+type.Name+" {");
   foreach(var field in type.Fields)if(field.HasConstant)Console.WriteLine(field.Name+"="+field.Constant+",");Console.WriteLine("}}");
  }return;
 }
 using(var fixture=AssemblyDefinition.ReadAssembly(args[1])){target=fixture.MainModule;
 foreach(string name in new[]{"MapControllerButtons","ResetDefaultControllerButtonBindings","ResetDefaultKeyBindings","RemapUIButtons","MapDefaultKeyboardLayout"}){
 var from=native.MainModule.GetType("InputHandler").Methods.Single(m=>m.Name==name);
 var to=target.GetType("InputHandler").Methods.Single(m=>m.Name==(name=="MapDefaultKeyboardLayout"?name:"Native_"+name));Copy(from,to);}
 Copy(native.MainModule.GetType("InControl.PlayerActionSet").Methods.Single(m=>m.Name=="Load"),target.GetType("InControl.PlayerActionSet").Methods.Single(m=>m.Name=="Load"));
 fixture.Write(args[2]);}
 }}
}
