﻿//#define CLR_DEBUG
using LibDotNetParser;
using LibDotNetParser.CILApi;
using LibDotNetParser.CILApi.IL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace libDotNetClr
{
    /// <summary>
    /// DotNetCLR Class
    /// </summary>
    public partial class DotNetClr
    {
        private DotNetFile file;
        private string EXEPath;
        private bool Running = false;

        private Dictionary<string, DotNetFile> dlls = new Dictionary<string, DotNetFile>();
        private CustomList<MethodArgStack> stack = new CustomList<MethodArgStack>();
        private MethodArgStack[] Localstack = new MethodArgStack[256];
        private List<CallStackItem> CallStack = new List<CallStackItem>();

        private Dictionary<string, ClrCustomInternalMethod> CustomInternalMethods = new Dictionary<string, ClrCustomInternalMethod>();
        public DotNetClr(DotNetFile exe, string DllPath)
        {
            if (!Directory.Exists(DllPath))
            {
                throw new DirectoryNotFoundException(DllPath);
            }
            if (exe == null)
            {
                throw new ArgumentException(nameof(exe));
            }

            EXEPath = DllPath;
            Init(exe);
        }
        private void Init(DotNetFile p)
        {
            file = p;
            dlls.Clear();
            dlls.Add("main_exe", p);

            RegisterAllInternalMethods();
        }


        /// <summary>
        /// Starts the .NET Executable
        /// </summary>
        public void Start()
        {
            try
            {
                if (file.EntryPoint == null)
                {
                    clrError("The entry point was not found.", "System.EntryPointNotFoundException");
                    file = null;
                    return;
                }
            }
            catch (Exception x)
            {
                clrError("The entry point was not found. Internal error: " + x.Message, "System.EntryPointNotFoundException");
                file = null;
                return;
            }
            InitAssembly(file);



            //Run the entry point
            RunMethod(file.EntryPoint, file, stack);
        }
        private void InitAssembly(DotNetFile file)
        {
            //Resolve all of the DLLS
            foreach (var item in file.Backend.Tabels.AssemblyRefTabel)
            {
                var fileName = file.Backend.ClrStringsStream.GetByOffset(item.Name);
                string fullPath = "";
                if (dlls.ContainsKey(fileName))
                {
                    PrintColor("[WARN] Assembly already loaded: " + fileName, ConsoleColor.Yellow);
                    continue;
                }

                if (File.Exists(Path.Combine(EXEPath, fileName + ".exe")))
                {
                    fullPath = Path.Combine(EXEPath, fileName + ".exe");
                }
                else if (File.Exists(Path.Combine(EXEPath, fileName + ".dll")))
                {
                    fullPath = Path.Combine(EXEPath, fileName + ".dll");
                }
                else if (File.Exists(fileName + ".exe"))
                {
                    fullPath = fileName + ".exe";
                }
                else if (File.Exists(fileName + ".dll"))
                {
                    fullPath = fileName + ".dll";
                }
                else
                {
                    //Console.WriteLine("File: " + fileName + ".dll does not exist in " + EXEPath + "!", "System.FileNotFoundException");
                    //Console.WriteLine("DotNetParser will not be stopped.");
                    //  return;
                }
#if CLR_DEBUG
                Console.WriteLine("[CLR] Loading: " + Path.GetFileName(fullPath));
#endif
                //try
                //{
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var file2 = new DotNetFile(fullPath);
                    InitAssembly(file2);
                    dlls.Add(fileName, file2);
                    PrintColor("[OK] Loaded assembly: " + fileName, ConsoleColor.Green);
                }

                else
                {
                    PrintColor("[ERROR] Load failed: " + fileName, ConsoleColor.Red);
                }

                //}
                // catch (Exception x)
                //{
                //    clrError("File: " + fileName + " has an unknown error in it. The error is: " + x.Message, "System.UnknownClrError");
                //   throw;
                //    return;
                //}
            }
            Running = true;
            //Call all static contructors
            foreach (var dll in dlls)
            {
                foreach (var t in dll.Value.Types)
                {
                    foreach (var m in t.Methods)
                    {
                        if (m.Name == ".cctor" && m.IsStatic)
                        {
                            RunMethod(m, file, stack, false);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Runs a method
        /// </summary>
        /// <param name="m">The method</param>
        /// <param name="file">The file of the method</param>
        /// <param name="oldStack">Old stack</param>
        /// <returns>Returns the return value</returns>
        private MethodArgStack RunMethod(DotNetMethod m, DotNetFile file, CustomList<MethodArgStack> oldStack, bool addToCallStack = true)
        {
            if (m.Name == ".ctor" && m.Parrent.FullName == "System.Object")
                return null;
            if (!Running)
                return null;
#if CLR_DEBUG
            Console.WriteLine("===========================");
            Console.WriteLine($"[CLR] Running Method: {m.Parrent.NameSpace}.{m.Parrent.Name}.{m.Name}()");
            if (stack.Count != 0)
                Console.WriteLine("Following items are on the stack:");
            foreach (var item in stack)
            {
                if (item.type == StackItemType.String)
                {
                    Console.WriteLine("String: \"" + (string)item.value + "\"");
                }
                else if (item.type == StackItemType.Int32)
                {
                    Console.WriteLine("Int32: " + (int)item.value);
                }
            }
            Console.WriteLine("===========================");
            Console.WriteLine("FUNCTION Output");
#endif

            #region Internal methods
            //Make sure that RVA is not zero. If its zero, than its extern
            if (m.IsInternalCall)
            {
                foreach (var item in CustomInternalMethods)
                {
                    if (item.Key == m.Name)
                    {
                        MethodArgStack a = null;
                        item.Value.Invoke(stack.ToArray(), ref a, m);

                        //Don't forget to remove item parms
                        if (m.AmountOfParms == 0)
                        {
                            //no need to do anything
                        }
                        else
                        {
                            int StartParmIndex = -1;
                            int EndParmIndex = -1;
                            for (int i3 = 0; i3 < stack.Count; i3++)
                            {
                                var stackitm = stack[i3];
                                if (stackitm.type == m.StartParm && EndParmIndex == -1)
                                {
                                    StartParmIndex = i3;
                                }
                                if (stackitm.type == m.EndParm && StartParmIndex != -1)
                                {
                                    EndParmIndex = i3;
                                }
                            }
                            if (StartParmIndex == -1)
                                continue;

                            if (m.AmountOfParms == 1)
                            {
                                stack.RemoveAt(StartParmIndex);
                            }
                            else
                            {
                                stack.RemoveRange(StartParmIndex, EndParmIndex - StartParmIndex);
                            }
                            ;
                        }
                        return a;
                    }
                }

                clrError("Cannot find internal method: " + m.Name, "");
                return null;
            }
            else if (m.RVA == 0)
            {
                clrError($"Cannot find the method body for {m.Parrent.FullName}.{m.Name}", "System.Exception");
                return null;
            }
            #endregion
            if (addToCallStack)
            {
                //Add this method to the callstack.
                CallStack.Add(new CallStackItem() { method = m });
            }

            //Now decompile the code and run it
            var decompiler = new IlDecompiler(m);
            foreach (var item in dlls)
            {
                decompiler.AddRefernce(item.Value);
            }
            var code = decompiler.Decompile();
            int i;
            for (i = 0; i < code.Length; i++)
            {
                if (!Running)
                    return null;
                var item = code[i];

                #region Ldloc / stloc
                if (item.OpCodeName == "stloc.s")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on stack and putting it onto var stack at index "+(byte)item.Operand);
#endif

                    if (stack.Count == 0)
                        throw new Exception("Error in stloc.s: stack count is 0, which is ilegal!");
                    var oldItem = stack[stack.Count - 1];
                    Localstack[(byte)item.Operand] = oldItem;
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (item.OpCodeName == "stloc.0")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on stack and putting it onto var stack at index 0");
#endif
                    if (ThrowIfStackIsZero(stack, "stloc.0")) return null;
                    var oldItem = stack[stack.Count - 1];
                    Localstack[0] = oldItem;
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (item.OpCodeName == "stloc.1")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on stack and putting it onto var stack at index 1");
#endif
                    var oldItem = stack[stack.Count - 1];

                    Localstack[1] = oldItem;
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (item.OpCodeName == "stloc.2")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on stack and putting it onto var stack at index 2");
#endif
                    var oldItem = stack[stack.Count - 1];

                    Localstack[2] = oldItem;
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (item.OpCodeName == "stloc.3")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on stack and putting it onto var stack at index 3");
#endif
                    var oldItem = stack[stack.Count - 1];

                    Localstack[3] = oldItem;
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (item.OpCodeName == "ldloc.s")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on var and putting it onto object stack at index " + (byte)item.Operand);
#endif
                    var oldItem = Localstack[(byte)item.Operand];
                    stack.Add(oldItem);
                }
                else if (item.OpCodeName == "ldloca.s")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on var and putting it onto object stack at index " + (ushort)item.Operand);
#endif
                    var oldItem = Localstack[(byte)item.Operand];
                    stack.Add(oldItem);
                }
                else if (item.OpCodeName == "ldloc.0")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on var and putting it onto object stack at index 0");
#endif
                    var oldItem = Localstack[0];
                    stack.Add(oldItem);
                    // Localstack[0] = null;
                }
                else if (item.OpCodeName == "ldloc.1")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on var and putting it onto object stack at index 1");
#endif
                    var oldItem = Localstack[1];
                    stack.Add(oldItem);

                    //Localstack[1] = null;
                }
                else if (item.OpCodeName == "ldloc.2")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on var and putting it onto object stack at index 2");
#endif
                    var oldItem = Localstack[2];
                    stack.Add(oldItem);
                    //Localstack[2] = null;
                }
                else if (item.OpCodeName == "ldloc.3")
                {
#if CLR_DEBUG
                    Console.WriteLine("[Debug] Removing object on var and putting it onto object stack at index 3");
#endif
                    var oldItem = Localstack[3];
                    stack.Add(oldItem);
                    //Localstack[3] = null;
                }
                #endregion
                #region ldc* opcodes
                //Push int32
                else if (item.OpCodeName == "ldc.i4")
                {
                    //Puts an int32 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)item.Operand });
                }
                else if (item.OpCodeName == "ldc.i4.0")
                {
                    //Puts an 0 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)0 });
                }
                else if (item.OpCodeName == "ldc.i4.1")
                {
                    //Puts an int32 with value 1 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)1 });
                }
                else if (item.OpCodeName == "ldc.i4.2")
                {
                    //Puts an int32 with value 2 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)2 });
                }
                else if (item.OpCodeName == "ldc.i4.3")
                {
                    //Puts an int32 with value 3 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)3 });
                }
                else if (item.OpCodeName == "ldc.i4.4")
                {
                    //Puts an int32 with value 4 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)4 });
                }
                else if (item.OpCodeName == "ldc.i4.5")
                {
                    //Puts an int32 with value 5 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)5 });
                }
                else if (item.OpCodeName == "ldc.i4.6")
                {
                    //Puts an int32 with value 6 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)6 });
                }
                else if (item.OpCodeName == "ldc.i4.7")
                {
                    //Puts an int32 with value 7 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)7 });
                }
                else if (item.OpCodeName == "ldc.i4.8")
                {
                    //Puts an int32 with value 3 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)8 });
                }
                else if (item.OpCodeName == "ldc.i4.m1")
                {
                    //Puts an int32 with value -1 onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)-1 });
                }
                else if (item.OpCodeName == "ldc.i4.s")
                {
                    //Push an int32 onto the stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)(sbyte)(byte)item.Operand });
                }
                //Push int64
                else if (item.OpCodeName == "ldc.i8")
                {
                    stack.Add(new MethodArgStack() { type = StackItemType.Int64, value = (long)item.Operand });
                }
                //push float64
                else if (item.OpCodeName == "ldc.r4")
                {
                    //Puts an float32 with value onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Float32, value = (float)item.Operand });
                }
                //Push float64
                else if (item.OpCodeName == "ldc.r8")
                {
                    //Puts an float32 with value onto the arg stack
                    stack.Add(new MethodArgStack() { type = StackItemType.Float64, value = (float)item.Operand });
                }
                #endregion
                #region conv* opcodes
                else if (item.OpCodeName == "conv.i4")
                {
                    var numb = stack[stack.Count - 1];
                    if (numb.type == StackItemType.Int32)
                    {
                        //stack.Add(numb);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                #endregion
                #region Math
                else if (item.OpCodeName == "add")
                {
                    var numb1 = (int)stack[stack.Count - 2].value;
                    var numb2 = (int)stack[stack.Count - 1].value;
                    var result = numb1 + numb2;
                    stack.RemoveRange(stack.Count - 2, 2);
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = result });
                }
                else if (item.OpCodeName == "sub")
                {
                    var numb1 = (int)stack[stack.Count - 2].value;
                    var numb2 = (int)stack[stack.Count - 1].value;
                    var result = numb1 - numb2;
                    stack.RemoveRange(stack.Count - 2, 2);
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = result });
                }
                else if (item.OpCodeName == "div")
                {
                    var numb1 = (int)stack[stack.Count - 2].value;
                    var numb2 = (int)stack[stack.Count - 1].value;

                    //TODO: Check if dividing by zero
                    var result = numb1 / numb2;
                    stack.RemoveRange(stack.Count - 2, 2);
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = result });
                }
                else if (item.OpCodeName == "mul")
                {
                    var numb1 = (int)stack[stack.Count - 2].value;
                    var numb2 = (int)stack[stack.Count - 1].value;
                    var result = numb1 * numb2;
                    stack.RemoveRange(stack.Count - 2, 2);
                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = result });
                }
                else if (item.OpCodeName == "ceq")
                {
                    if (stack.Count < 2)
                        throw new Exception("There has to be 2 or more items on the stack for ceq instruction to work!");
                    var numb1 = stack[stack.Count - 2].value;
                    var numb2 = stack[stack.Count - 1].value;
                    int Numb1 = 0;
                    int Numb2 = 0;

                    if (numb1 is int)
                    {
                        Numb1 = (int)numb1;
                    }
                    else if (numb1 is char)
                    {
                        Numb1 = (int)(char)numb1;
                    }
                    else
                    {
                        throw new Exception();
                    }

                    if (numb2 is int)
                    {
                        Numb2 = (int)numb2;
                    }
                    else if (numb2 is char)
                    {
                        Numb2 = (int)(char)numb2;
                    }
                    else
                    {
                        throw new Exception();
                    }

                    stack.RemoveRange(stack.Count - 2, 2);
                    ;
                    if (Numb1 == Numb2)
                    {
                        //push 1
                        stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)1 });
                    }
                    else
                    {
                        //push 0
                        stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)0 });
                    }
                }
                else if (item.OpCodeName == "cgt")
                {
                    var numb1 = (int)stack[stack.Count - 2].value;
                    var numb2 = (int)stack[stack.Count - 1].value;
                    stack.RemoveRange(stack.Count - 2, 2);
                    if (numb1 > numb2)
                    {
                        //push 1
                        stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)1 });
                    }
                    else
                    {
                        //push 0
                        stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)0 });
                    }
                }
                else if (item.OpCodeName == "clt")
                {
                    var numb1 = (int)stack[stack.Count - 2].value;
                    var numb2 = (int)stack[stack.Count - 1].value;
                    stack.RemoveRange(stack.Count - 2, 2);
                    if (numb1 < numb2)
                    {
                        //push 1
                        stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)1 });
                    }
                    else
                    {
                        //push 0
                        stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = (int)0 });
                    }
                }
                #endregion
                #region Branch instructions
                else if (item.OpCodeName == "br.s")
                {
                    //find the ILInstruction that is in this position
                    int i2 = item.Position + (int)item.Operand + 1;
                    ILInstruction inst = decompiler.GetInstructionAtOffset(i2, -1);

                    if (inst == null)
                        throw new Exception("Attempt to branch to null");

#if CLR_DEBUG
                    Console.WriteLine("branching to: IL_" + inst.Position + ": " + inst.OpCodeName);
#endif
                    i = inst.RelPosition - 1;
                }
                else if (item.OpCodeName == "brfalse.s")
                {
                    if (stack.Count == 0)
                    {
                        string stackTrace = "";
                        CallStack.Reverse();
                        foreach (var itm in CallStack)
                        {
                            stackTrace += itm.method.Parrent.NameSpace + "." + itm.method.Parrent.Name + "." + itm.method.Name + "()\n";
                        }
                        clrError("Do not know if I should branch, because there is nothing on the stack. Instruction: brfalse.s", "Internal", stackTrace);
                        return null;
                    }
                    var s = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    bool exec = false;
                    if (s.value == null)
                        exec = true;
                    else
                    {
                        try
                        {
                            if ((int)s.value == 0)
                                exec = true;
                        }
                        catch { }
                    }


                    if (exec)
                    {
                        // find the ILInstruction that is in this position
                        int i2 = item.Position + (int)item.Operand + 1;
                        ILInstruction inst = decompiler.GetInstructionAtOffset(i2, -1);

                        if (inst == null)
                            throw new Exception("Attempt to branch to null");
                        i = inst.RelPosition - 1;
#if CLR_DEBUG
                    Console.WriteLine("branching to: IL_" + inst.Position + ": " + inst.OpCodeName+" because item on stack is false.");
#endif
                    }
                }
                else if (item.OpCodeName == "brtrue.s")
                {
                    if (stack[stack.Count - 1].value == null)
                        continue;
                    if ((int)stack[stack.Count - 1].value == 1)
                    {
                        // find the ILInstruction that is in this position
                        int i2 = item.Position + (int)item.Operand + 1;
                        ILInstruction inst = decompiler.GetInstructionAtOffset(i2, -1);

                        if (inst == null)
                            throw new Exception("Attempt to branch to null");
                        stack.RemoveAt(stack.Count - 1);
                        i = inst.RelPosition - 1;
#if CLR_DEBUG
                    Console.WriteLine("branching to: IL_" + inst.Position + ": " + inst.OpCodeName+" because item on stack is true.");
#endif
                    }
                    else
                    {
                        stack.RemoveAt(stack.Count - 1);
                    }
                }
                #endregion
                #region Misc
                else if (item.OpCodeName == "ldstr")
                {
                    stack.Add(new MethodArgStack() { type = StackItemType.String, value = (string)item.Operand });
#if CLR_DEBUG
                    Console.WriteLine("[CLRDEBUG] Pushing: " + (string)item.Operand);
#endif
                }
                else if (item.OpCodeName == "nop")
                {
                    //Don't do anything
                }
                else if (item.OpCodeName == "conv.i8")
                {
                    var itm = stack[stack.Count - 1];
                    if (itm.type == StackItemType.Int32)
                    {
                        itm.value = (long)(int)itm.value;
                        itm.type = StackItemType.Int64;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    stack.RemoveAt(stack.Count - 1);
                    stack.Add(itm);
                }
                else if (item.OpCodeName == "ldsfld")
                {
                    //get value from feild
                    DotNetField f2 = null;
                    foreach (var f in m.Parrent.Fields)
                    {
                        if (f.IndexInTabel == (int)(byte)item.Operand)
                        {
                            f2 = f;
                            break;
                        }
                    }

                    if (f2 == null)
                    {
                        foreach (var d in dlls)
                        {
                            foreach (var t in d.Value.Types)
                            {
                                foreach (var f in t.Fields)
                                {
                                    if (f.IndexInTabel == (int)(byte)item.Operand)
                                    {
                                        f2 = f;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (f2 == null)
                        throw new Exception("Cannot find the field.");

                    StaticField f3 = null;
                    foreach (var f in StaticFieldHolder.staticFields)
                    {
                        if (f.theField.Name == f2.Name && f.theField.ParrentType.FullName == f2.ParrentType.FullName)
                        {
                            f3 = f;
                            break;
                        }
                    }
                    if (f3 == null)
                    {
                        Running = false;
                        string stackTrace = "";
                        CallStack.Reverse();
                        foreach (var itm in CallStack)
                        {
                            stackTrace += itm.method.Parrent.NameSpace + "." + itm.method.Parrent.Name + "." + itm.method.Name + "()\n";
                        }
                        clrError("Attempt to push null onto the stack. Source instruction: ldsfld", "System.NullReferenceException", stackTrace);
                        return null;
                    }
                    stack.Add(f3.value);
                }
                else if (item.OpCodeName == "stsfld")
                {
                    //write value to field.
                    DotNetField f2 = null;
                    foreach (var f in m.Parrent.Fields)
                    {
                        if (f.IndexInTabel == (int)(byte)item.Operand)
                        {
                            f2 = f;
                            break;
                        }
                    }
                    StaticField f3 = null;
                    foreach (var f in StaticFieldHolder.staticFields)
                    {
                        if (f.theField.Name == f2.Name && f.theField.ParrentType.FullName == f2.ParrentType.FullName)
                        {
                            f3 = f;

                            f.value = stack[stack.Count - 1];
                            break;
                        }
                    }

                    if (f3 == null)
                    {
                        //create field
                        StaticFieldHolder.staticFields.Add(new StaticField() { theField = f2, value = stack[stack.Count - 1] });
                    }
                    if (f2 == null)
                        throw new Exception("Cannot find the field.");
                    f2.Value = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (item.OpCodeName == "call")
                {
                    var call = (InlineMethodOperandData)item.Operand;
                    MethodArgStack returnValue;

                    DotNetMethod m2 = null;
                    if (call.RVA != 0)
                    {
                        //Local/Defined method
                        foreach (var item2 in dlls)
                        {
                            foreach (var item3 in item2.Value.Types)
                            {
                                foreach (var meth in item3.Methods)
                                {
                                    if (meth.RVA == call.RVA && meth.Name == call.FunctionName && meth.Signature == call.Signature && meth.Parrent.FullName == call.NameSpace + "." + call.ClassName)
                                    {
                                        m2 = meth;
                                        break;
                                    }
                                }
                            }
                        }

                        if (m2 == null)
                        {
                            Console.WriteLine($"Cannot resolve called method: {call.NameSpace}.{call.ClassName}.{call.FunctionName}(). Function signature is {call.Signature}");
                            return null;
                        }
                    }
                    else
                    {
                        //Attempt to resolve it
                        foreach (var item2 in dlls)
                        {
                            foreach (var item3 in item2.Value.Types)
                            {
                                foreach (var meth in item3.Methods)
                                {
                                    if (meth.Name == call.FunctionName && meth.Parrent.Name == call.ClassName && meth.Parrent.NameSpace == call.NameSpace && meth.Signature == call.Signature)
                                    {
                                        m2 = meth;
                                        break;
                                    }
                                }
                            }
                        }
                        if (m2 == null)
                        {
                            clrError($"Cannot resolve method: {call.NameSpace}.{call.ClassName}.{call.FunctionName}. Method signature is {call.Signature}", "System.MethodNotFound");
                            return null;
                        }
                    }
                    //Extract the parms
                    int StartParmIndex = -1;
                    int EndParmIndex = -1;
                    for (int i3 = 0; i3 < stack.Count; i3++)
                    {
                        var stackitm = stack[i3];
                        if (stackitm.type == m2.StartParm && EndParmIndex == -1)
                        {
                            StartParmIndex = i3;
                        }
                        if (stackitm.type == m2.EndParm && StartParmIndex != -1)
                        {
                            EndParmIndex = i3;
                        }
                    }
                    CustomList<MethodArgStack> newParms = new CustomList<MethodArgStack>();
                    if (StartParmIndex != -1)
                    {
                        for (int i5 = StartParmIndex; i5 < EndParmIndex; i5++)
                        {
                            var itm5 = stack[i5];
                            newParms.Add(itm5);
                        }
                    }
                    if (StartParmIndex == 0 && EndParmIndex == 0 && m2.AmountOfParms == 1)
                    {
                        newParms.Add(stack[0]);
                    }
                    //Call it
                    returnValue = RunMethod(m2, m.Parrent.File, newParms, addToCallStack);
                    if (m2.AmountOfParms == 0)
                    {
                        //no need to do anything
                    }
                    else
                    {
                        //Re extract the parms after running the function
                        for (int i3 = 0; i3 < stack.Count; i3++)
                        {
                            var stackitm = stack[i3];
                            if (stackitm.type == m2.StartParm && EndParmIndex == -1)
                            {
                                StartParmIndex = i3;
                            }
                            if (stackitm.type == m2.EndParm && StartParmIndex != -1)
                            {
                                EndParmIndex = i3;
                            }
                        }
                        if (StartParmIndex == -1)
                            continue;

                        if (m2.AmountOfParms == 1 && stack.Count - 1 >= m2.AmountOfParms)
                        {
                            try
                            {
                                stack.RemoveAt(StartParmIndex);
                            }
                            catch (Exception) { }
                        }
                        else
                        {
                            var numb = EndParmIndex - StartParmIndex;
                            if (numb == -1)
                                continue;
                            if (stack.Count < numb)
                                continue;

                            stack.RemoveRange(StartParmIndex, numb);
                        }
                    }
                    if (returnValue != null)
                    {
                        stack.Add(returnValue);
                    }
                }
                else if (item.OpCodeName == "ldnull")
                {
                    stack.Add(new MethodArgStack() { type = StackItemType.ldnull, value = null });
                }
                else if (item.OpCodeName == "throw")
                {
                    //Throw Exception
                    var exp = stack[stack.Count - 1];

                    if (exp.type == StackItemType.ldnull)
                    {
                        string stackTrace = "";
                        CallStack.Reverse();
                        foreach (var itm in CallStack)
                        {
                            stackTrace += "at "+itm.method.Parrent.NameSpace + "." + itm.method.Parrent.Name + "." + itm.method.Name + "();\n";
                        }

                        stackTrace = stackTrace.Substring(0, stackTrace.Length - 1); //Remove last \n

                        clrError("Object reference not set to an instance of an object.", "System.NullReferenceException", stackTrace);
                        return null;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else if (item.OpCodeName == "ret")
                {
                    //Return from function
#if CLR_DEBUG
                    Console.WriteLine("[CLR] Returning from function");
#endif
                    //Successful return
                    MethodArgStack a = null;
                    if (stack.Count != 0)
                    {
                        a = stack[stack.Count - 1];
                        if (addToCallStack)
                            CallStack.RemoveAt(CallStack.Count - 1);


                        stack.RemoveAt(stack.Count - 1);
                    }

                    return a;
                }
                else if (item.OpCodeName == "newobj")
                {
                    var call = (InlineMethodOperandData)item.Operand;
                    //Local/Defined method
                    DotNetMethod m2 = null;
                    foreach (var item2 in dlls)
                    {
                        foreach (var item3 in item2.Value.Types)
                        {
                            foreach (var meth in item3.Methods)
                            {
                                if (meth.RVA == call.RVA && meth.Name == call.FunctionName && meth.Signature == call.Signature && meth.Parrent.FullName == call.NameSpace + "." + call.ClassName)
                                {
                                    m2 = meth;
                                    break;
                                }
                            }
                        }
                    }

                    if (m2 == null)
                    {
                        clrError($"Cannot resolve called constructor: {call.NameSpace}.{call.ClassName}.{call.FunctionName}(). Function signature is {call.Signature}", "");
                        return null;
                    }

                    MethodArgStack a = new MethodArgStack() { ObjectContructor = m2, ObjectType = m2.Parrent, type = StackItemType.Object, value = new ObjectValueHolder() };
                    stack.Add(a);
                    //Call the contructor
                    RunMethod(m2, m.Parrent.File, stack, addToCallStack);

                    if (stack.Count == 0)
                    {
                        //Make sure its still here
                        stack.Add(a);
                    }
                }
                else if (item.OpCodeName == "stfld")
                {
                    //write value to field.
                    DotNetField f2 = null;
                    foreach (var f in m.Parrent.Fields)
                    {
                        if (f.IndexInTabel == (byte)item.Operand)
                        {
                            f2 = f;
                            break;
                        }
                    }
                    if (f2 == null)
                    {
                        //Resolve recursively
                        foreach (var type in file.Types)
                        {
                            foreach (var field in type.Fields)
                            {
                                if (field.IndexInTabel == (byte)item.Operand)
                                {
                                    f2 = field;
                                    break;
                                }
                            }
                        }
                    }
                    if (f2 == null)
                    {
                        clrError("Failed to resolve field for writing.", "");
                        return null;
                    }
                    var obj = stack[stack.Count - 2];
                    if (obj.type != StackItemType.Object) throw new InvalidOperationException();

                    var data = (ObjectValueHolder)obj.value;
                    if (data.Fields.ContainsKey(f2.Name))
                    {
                        data.Fields[f2.Name] = stack[stack.Count - 1];
                    }
                    else
                    {
                        data.Fields.Add(f2.Name, stack[stack.Count - 1]);
                    }
                    obj.value = data;
                    stack[0] = obj;
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (item.OpCodeName == "ldfld")
                {
                    //write value to field.
                    DotNetField f2 = null;
                    foreach (var f in m.Parrent.Fields)
                    {
                        if (f.IndexInTabel == (byte)item.Operand)
                        {
                            f2 = f;
                            break;
                        }
                    }
                    if (f2 == null)
                    {
                        //Resolve recursively
                        foreach (var type in file.Types)
                        {
                            foreach (var field in type.Fields)
                            {
                                if (field.IndexInTabel == (byte)item.Operand)
                                {
                                    f2 = field;
                                    break;
                                }
                            }
                        }
                    }
                    var obj = stack[stack.Count - 1];
                    if (obj.type != StackItemType.Object) throw new InvalidOperationException();

                    var data = (ObjectValueHolder)obj.value;
                    if (data.Fields.ContainsKey(f2.Name))
                    {
                        stack.Add(data.Fields[f2.Name]);
                    }
                    else
                    {
                        throw new Exception("Attempt to read from a nonexistent or null field.");
                    }
                }
                else if (item.OpCodeName == "ldarg.0")
                {
                    if (oldStack.Count == 0)
                        continue;

                    if (stack.Count != 0)
                        stack[0] = oldStack[0];
                    else
                        stack.Add(oldStack[0]);
                }
                else if (item.OpCodeName == "ldarg.1")
                {
                    if (oldStack.Count == 0)
                        continue;

                    if (stack.Count != 0)
                        stack[1] = oldStack[1];
                    else
                        stack.Add(oldStack[1]);
                }
                else if (item.OpCodeName == "callvirt")
                {
                    var call = (InlineMethodOperandData)item.Operand;
                    //Local/Defined method
                    DotNetMethod m2 = null;
                    foreach (var item2 in dlls)
                    {
                        foreach (var item3 in item2.Value.Types)
                        {
                            foreach (var meth in item3.Methods)
                            {
                                if (meth.RVA == call.RVA && meth.Name == call.FunctionName && meth.Signature == call.Signature && meth.Parrent.FullName == call.NameSpace + "." + call.ClassName)
                                {
                                    m2 = meth;
                                    break;
                                }
                            }
                        }
                    }

                    if (m2 == null)
                    {
                        clrError($"Cannot resolve virtual called method: {call.NameSpace}.{call.ClassName}.{call.FunctionName}(). Function signature is {call.Signature}", "");
                        return null;
                    }
                    stack.Add(RunMethod(m2, m2.File, stack, addToCallStack));
                }
                #endregion
                #region Arrays
                else if (item.OpCodeName == "newarr")
                {
                    var arrayLen = stack[stack.Count - 1];

                    stack.Add(new MethodArgStack() { type = StackItemType.Array, ArrayItems = new object[(int)arrayLen.value], ArrayLen = (int)arrayLen.value });
                }
                else if (item.OpCodeName == "ldlen")
                {
                    var arr = stack[stack.Count - 1];
                    if (arr.type != StackItemType.Array)
                    {
                        throw new Exception("Expected array, but got something else.");
                    }

                    stack.Add(new MethodArgStack() { type = StackItemType.Int32, value = arr.ArrayLen });
                }
                else if (item.OpCodeName == "stelem.ref")
                {
                    var itemToWrite = stack[stack.Count - 1];
                    var index = stack[stack.Count - 2];
                    var arr = stack[stack.Count - 3];
                    throw new NotImplementedException();
                }
                #endregion
                else
                {
                    Running = false;
                    PrintColor("Unsupported OpCode: " + item.OpCodeName, ConsoleColor.Red);
                    PrintColor("Application Terminated.", ConsoleColor.Red);
                    CallStack.Reverse();
                    string stackTrace = "";
                    foreach (var itm in CallStack)
                    {
                        stackTrace += "At " + itm.method.Parrent.NameSpace + "." + itm.method.Parrent.Name + "." + itm.method.Name + "()\n";
                    }
                    PrintColor(stackTrace, ConsoleColor.Red);
                    return null;
                }
            }
            return null;
        }
        /// <summary>
        /// Returns true if there is a problem
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="instruction"></param>
        /// <returns></returns>
        private bool ThrowIfStackIsZero(CustomList<MethodArgStack> stack, string instruction)
        {
           if (stack.Count == 0)
            {
                string stackTrace = "";
                CallStack.Reverse();
                foreach (var itm in CallStack)
                {
                    stackTrace += itm.method.Parrent.NameSpace + "." + itm.method.Parrent.Name + "." + itm.method.Name + "()\n";
                }
                clrError("Fatal error: The "+instruction+" requires more than 1 items on the stack.", "Internal", stackTrace);
                return true;
            }
            return false;
        }
        #region Utils
        private void PrintColor(string s, ConsoleColor c)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.WriteLine(s);
            Console.ForegroundColor = old;
        }

        private void clrError(string message, string errorType, string stackStace = "")
        {
            //Running = false;
            PrintColor($"A {errorType} has occured in {file.Backend.ClrStringsStream.GetByOffset(file.Backend.Tabels.ModuleTabel[0].Name)}. The error is: {message}", ConsoleColor.Red);
            PrintColor(stackStace, ConsoleColor.Red);
        }
        #endregion
    }
}
